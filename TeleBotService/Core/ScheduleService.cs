using Humanizer;
using Microsoft.Extensions.Options;
using Omada.OpenApi.Client.Responses;
using Quartz;
using Quartz.Impl.Matchers;
using Quartz.Logging;
using Quartz.Spi;
using TeleBotService.Config;

namespace TeleBotService.Core;

public class SchedulerService : IHostedService, IJobInfoProvider
{
    private const string TriggerNameKey = "TriggerName";

    private static readonly JobDataMap NetClientDisconnectedTriggerDataMap = new(1);
    private static readonly JobDataMap NetClientConnectedTriggerDataMap = new(1);
    private static readonly JobDataMap CronTriggerDataMap = new(1);

    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<SchedulerService> logger;
    private readonly SchedulesConfig config;
    private IScheduler? scheduler;

    public SchedulerService(
        IServiceProvider serviceProvider,
        IOptions<SchedulesConfig> config,
        ILogger<SchedulerService> logger,
        ILogger<QuartzLogProvider> quartzLogger)
    {
        NetClientDisconnectedTriggerDataMap[TriggerNameKey] = "NetClientDisconnected";
        NetClientConnectedTriggerDataMap[TriggerNameKey] = "NetClientConnected";
        CronTriggerDataMap[TriggerNameKey] = "Cron";

        this.serviceProvider = serviceProvider;
        this.logger = logger;
        this.config = config.Value;
        LogProvider.SetCurrentLogProvider(new QuartzLogProvider(quartzLogger));
        EventTriggerDataExtensions.Logger = logger;

        if (serviceProvider.GetService<INetClientMonitor>() is { } netClientMonitor)
        {
            netClientMonitor.ClientConcected += this.OnClientConnected;
            netClientMonitor.ClientDisconcected += this.OnClientDisconnected;
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var schedule in this.config.Where(s => s.Value.Enabled))
        {
            if (string.IsNullOrEmpty(schedule.Value.CronSchedule) && string.IsNullOrEmpty(schedule.Value.EventTrigger))
            {
                continue;
            }

            var job = JobBuilder.Create<CommandJob>()
                                .WithIdentity(schedule.Key)
                                .StoreDurably(true)
                                .Build();


            job.JobDataMap[nameof(ScheduleConfig)] = schedule.Value;
            var trigger = string.IsNullOrEmpty(schedule.Value.CronSchedule) ?
                          null :
                          TriggerBuilder.Create()
                                        .WithIdentity($"{schedule.Key}_CronTrigger")
                                        .UsingJobData(CronTriggerDataMap)
                                        .StartNow()
                                        .WithCronSchedule(schedule.Value.CronSchedule)
                                        .Build();

            await this.ScheduleJob(job, trigger, cancellationToken);
        }

        _ = this.scheduler?.Start(cancellationToken);
    }

    public async IAsyncEnumerable<JobInfo> GetJobs()
    {
        if (this.scheduler != null)
        {
            var jobKeys = await this.scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
            foreach (var key in jobKeys)
            {
                var job = await this.scheduler.GetJobDetail(key);
                if (job != null)
                {
                    ScheduleConfig? scheduleConfig = null;
                    if (job.JobDataMap.TryGetValue(nameof(ScheduleConfig), out var config))
                    {
                        scheduleConfig = config as ScheduleConfig;
                    }


                    var trigger = await this.scheduler.GetTriggersOfJob(key);
                    var triggers = trigger.Select(t => new JobInfo.TriggerInfo() { TriggerKey = t.Key.Name, NextFireTimeUtc = t.GetNextFireTimeUtc(), PreviousFireTimeUtc = t.GetPreviousFireTimeUtc() });
                    yield return new JobInfo()
                    {
                        JobKey = job.Key.Name,
                        CommandText = scheduleConfig?.CommandText,
                        CronSchedule = scheduleConfig?.CronSchedule,
                        User = scheduleConfig?.User,
                        EventTrigger = scheduleConfig?.EventTrigger,
                        IsInValidTime = scheduleConfig?.EventTriggerInfo.IsInValidTime,
                        Triggers = triggers,
                    };
                }
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => this.scheduler?.Shutdown(cancellationToken) ?? Task.CompletedTask;

    private async Task ScheduleJob(IJobDetail job, ITrigger? trigger, CancellationToken cancellationToken)
    {
        if (this.scheduler == null)
        {
            this.scheduler = await SchedulerBuilder.Create()
                                                   .UseDefaultThreadPool(o => o.MaxConcurrency = 2)
                                                   .BuildScheduler();

            scheduler.JobFactory = new JobFactory(this.serviceProvider);
        }

        if (trigger != null)
        {
            await this.scheduler.ScheduleJob(job, trigger, cancellationToken);
        }
        else
        {
            await this.scheduler.AddJob(job, false, cancellationToken);
        }
    }

    private void OnClientDisconnected(object? _, ClientConnectionParams clientData) =>
        _ = this.OnClientConnectionEvent("ClientDisconnected", clientData, NetClientDisconnectedTriggerDataMap);

    private void OnClientConnected(object? _, ClientConnectionParams clientData) =>
        _ = this.OnClientConnectionEvent("ClientConnected", clientData, NetClientConnectedTriggerDataMap);


    private async Task OnClientConnectionEvent(string eventName, ClientConnectionParams clientData, JobDataMap data)
    {
        foreach (var client in clientData.UpdatedClients)
        {
            if (await this.OnClientConnectionEvent(eventName, clientData, client, data))
            {
                return;
            }
        }
    }

    private async Task<bool> OnClientConnectionEvent(string eventName, ClientConnectionParams clientData, BasicClientData client, JobDataMap data)
    {
        var jobs = this.config.Where(s => s.Value.EventTriggerInfo.EventName == eventName);
        var executedCount = 0;
        foreach (var job in jobs)
        {
            var eventInfo = job.Value.EventTriggerInfo;
            if (eventInfo.ClientMeetsParameters(job.Key, clientData, client))
            {
                var task = eventInfo.Delay.HasValue ?
                           this.scheduler?.ScheduleJob(GetSingleFireTrigger(job.Key, data, eventInfo.Delay.Value)) :
                           this.scheduler?.TriggerJob(new JobKey(job.Key), data);

                if (task != null)
                {
                    await task;

                    if (this.logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug) )
                    {
                        if (task is Task<DateTimeOffset> futureTask)
                        {
                            var time = await futureTask;
                            this.logger.LogDebug("[{eventName}]: Job {job} will run in {time}", eventName, job.Key, time.Humanize());
                        }
                        else
                        {
                            this.logger.LogDebug("[{eventName}]: Job {job} will run now", eventName, job.Key);
                        }
                    }

                    executedCount++;
                }
            }
            else
            {
                this.logger.LogDebug("[{eventName}]: Job {job} does not meet parameters to execute job.", eventName, job.Key);
            }
        }

        return executedCount > 0;
    }

    private static ITrigger GetSingleFireTrigger(string key , JobDataMap data, TimeSpan delay) =>
        TriggerBuilder.Create()
                      .WithIdentity($"{key}_DelaySingleTrigger", "Delay")
                      .UsingJobData(data)
                      .StartAt(DateTime.UtcNow.Add(delay))
                      .ForJob(key)
                      .WithSimpleSchedule(b => b.WithRepeatCount(0))
                      .Build();

    private class JobFactory : IJobFactory
    {
        private readonly IServiceProvider serviceProvider;

        public JobFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            var job = this.serviceProvider.GetService(bundle.JobDetail.JobType)!;
            return (IJob)job;
        }


        public void ReturnJob(IJob job) { }
    }

    internal class CommandJob : IJob
    {
        private readonly ITelegramService telegramService;
        private readonly ILogger<CommandJob> logger;

        public CommandJob(ITelegramService telegramService, ILogger<CommandJob> logger)
        {
            this.telegramService = telegramService;
            this.logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var config = (ScheduleConfig)context.JobDetail.JobDataMap[nameof(ScheduleConfig)];
            var triggerName = context.Trigger.JobDataMap.GetString(TriggerNameKey);
            foreach (var command in config.CommandText.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var result = await this.telegramService.ExecuteCommand(command, config.User, config.Reply);
                this.logger.LogInformation("Automatically executed command '{commandText}' ({configKey}) by user '{user}' triggered by '{trigger} ({triggerName})' with result '{commandResult}'", command, context.JobDetail.Key, config.User, triggerName, context.Trigger.Key.Name, result);
                await Task.Delay(1000);
            }
        }
    }

    public class QuartzLogProvider : ILogProvider
    {
        private static readonly Dictionary<Quartz.Logging.LogLevel, Microsoft.Extensions.Logging.LogLevel> LogLevelMap = new()
        {
            { Quartz.Logging.LogLevel.Debug, Microsoft.Extensions.Logging.LogLevel.Debug },
            { Quartz.Logging.LogLevel.Error, Microsoft.Extensions.Logging.LogLevel.Error },
            { Quartz.Logging.LogLevel.Fatal, Microsoft.Extensions.Logging.LogLevel.Critical },
            { Quartz.Logging.LogLevel.Info, Microsoft.Extensions.Logging.LogLevel.Information },
            { Quartz.Logging.LogLevel.Trace, Microsoft.Extensions.Logging.LogLevel.Trace },
            { Quartz.Logging.LogLevel.Warn, Microsoft.Extensions.Logging.LogLevel.Warning },
        };

        private readonly ILogger logger;

        public QuartzLogProvider(ILogger logger)
        {
            this.logger = logger;
        }

        public Logger GetLogger(string name) => (level, func, exception, parameters) =>
        {
            if (func != null)
            {
                var message = func();
                this.logger.Log(LogLevelMap[level], exception, message, parameters);
            }
            return true;
        };

        public IDisposable OpenNestedContext(string message)
        {
            throw new NotImplementedException();
        }

        public IDisposable OpenMappedContext(string key, object value, bool destructure = false)
        {
            throw new NotImplementedException();
        }
    }
}

public static class EventTriggerDataExtensions
{
    public static ILogger? Logger { get; set; }

    public static bool ClientMeetsParameters(this EventTriggerData eventInfo, string jobKey, ClientConnectionParams clientData, BasicClientData client)
    {
        if (!eventInfo.IsInValidTime)
        {
            if (Logger?.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug) == true)
            {
                Logger.LogDebug("[{jobKey}]: Not in valid date time.", jobKey);
            }
            return false;
        }

        return MeetsIndividualParameters(jobKey, eventInfo, client) && MeetsGroupParameters(jobKey, eventInfo, clientData, client);

        static bool MeetsGroupParameters(string jobKey, EventTriggerData eventInfo, ClientConnectionParams clientData, BasicClientData client)
        {
            int? meetCount = eventInfo.MeetCount.HasValue ? clientData.CurrentClients.Count(c => MeetsIndividualParameters(null, eventInfo, c)) : null;
            int? prevMeetCount = eventInfo.PrevMeetCount.HasValue ? clientData.PreviousClients.Count(c => MeetsIndividualParameters(null, eventInfo, c)) : null;

            var meetsMeetCount = meetCount == eventInfo.MeetCount;
            var meetsPrevMeetCount = prevMeetCount == eventInfo.PrevMeetCount;

            if (Logger?.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug) == true)
            {
                if (!meetsMeetCount)
                {
                    Logger.LogDebug("[{jobKey}]: MeetCount does not meet criteria: Required: {requiredValue}, Actual: {value}.", jobKey, eventInfo.MeetCount, meetCount);
                }

                if (!meetsPrevMeetCount)
                {
                    Logger.LogDebug("[{jobKey}]: PrevMeetCount does not meet criteria: Required: {requiredValue}, Actual: {value}.", jobKey, eventInfo.PrevMeetCount, prevMeetCount);
                }
            }

            return meetsMeetCount && meetsPrevMeetCount;
        }

        static bool MeetsIndividualParameters(string? jobKey, EventTriggerData eventInfo, BasicClientData client) =>
            eventInfo.MeetsIndividualParameter(jobKey, nameof(BasicClientData.Ssid), client.Ssid) &&
                eventInfo.MeetsIndividualParameter(jobKey, nameof(BasicClientData.NetworkName), client.NetworkName) &&
                    eventInfo.MeetsIndividualParameter(jobKey, nameof(BasicClientData.Name), client.Name) &&
                        eventInfo.MeetsIndividualParameter(jobKey, nameof(BasicClientData.Mac), client.Mac);
    }

    public static bool MeetsIndividualParameter(this EventTriggerData eventInfo, string? jobKey, string paramName, string? value)
    {
        var result = eventInfo.HasParamValueOrNotSet(paramName, value);
        if (!result && jobKey != null && Logger?.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug) == true)
        {
            var requiredValue = eventInfo.GetParamValue(paramName);
            Logger.LogDebug("[{jobKey}]: {paramName} does not meet criteria: Required: {requiredValue}, Actual: {value}, Excluded: {excluded}", jobKey, paramName, requiredValue, value, eventInfo.IsExcluded(paramName, value));
        }

        return result;
    }
}

public static class SchedulerRegistrationExtensions
{
    public static IServiceCollection AddJobScheduler(this IServiceCollection serviceCollection, IConfigurationManager config) =>
        serviceCollection.AddSingleton<SchedulerService.CommandJob>()
                         .AddSingleton<IJobInfoProvider, SchedulerService>()
                         .AddHostedService(s => (SchedulerService)s.GetService<IJobInfoProvider>()!)
                         .Configure<SchedulesConfig>(config.GetSection(SchedulesConfig.ScheduleConfigName));
}
