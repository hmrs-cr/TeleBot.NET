using Microsoft.Extensions.Options;
using Omada.OpenApi.Client.Responses;
using Quartz;
using Quartz.Logging;
using Quartz.Spi;
using TeleBotService.Config;

namespace TeleBotService.Core;

public class SchedulerService : IHostedService
{
    private const string TriggerNameKey = "TriggerName";

    private static readonly JobDataMap NetClientDisconnectedTriggerDataMap = new(1);
    private static readonly JobDataMap NetClientConnectedTriggerDataMap = new(1);
    private static readonly JobDataMap CronTriggerDataMap = new(1);

    private readonly IServiceProvider serviceProvider;
    private readonly SchedulesConfig config;
    private IScheduler? scheduler;

    public SchedulerService(IServiceProvider serviceProvider, IOptions<SchedulesConfig> config, ILogger<SchedulerService> logger)
    {
        NetClientDisconnectedTriggerDataMap[TriggerNameKey] = "NetClientDisconnected";
        NetClientConnectedTriggerDataMap[TriggerNameKey] = "NetClientConnected";
        CronTriggerDataMap[TriggerNameKey] = "Cron";

        this.serviceProvider = serviceProvider;
        this.config = config.Value;
        LogProvider.SetCurrentLogProvider(new MyLogProvider(logger));

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
        var jobs = this.config.Where(s => s.Value.EventTriggerInfo.EventName == eventName && s.Value.IsInValidTime);
        foreach (var job in jobs)
        {
            var eventInfo = job.Value.EventTriggerInfo;
            if (eventInfo.ClientMeetsParameters(clientData, client))
            {
                var task = eventInfo.Delay.HasValue ?
                           this.scheduler?.ScheduleJob(GetSingleFireTrigger(job.Key, data, eventInfo.Delay.Value)) :
                           this.scheduler?.TriggerJob(new JobKey(job.Key), data);

                if (task != null)
                {
                    await task;
                    return true;
                }
            }
        }

        return false;
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
                this.logger.LogInformation("Automatically executed command '{commandText}' by user '{user}' triggered by '{trigger} ({triggerName})' with result '{commandResult}'", command, config.User, triggerName, context.Trigger.Key.Name, result);
            }
        }
    }

    private class MyLogProvider : ILogProvider
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

        public MyLogProvider(ILogger logger)
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
    public static bool ClientMeetsParameters(this EventTriggerData eventInfo, ClientConnectionParams clientData, BasicClientData client)
    {
        return MeetsIndividualParameters(eventInfo, client) && MeetsGroupParameters(eventInfo, clientData, client);

        static bool MeetsGroupParameters(EventTriggerData eventInfo, ClientConnectionParams clientData, BasicClientData client)
        {
            int? meetCount = eventInfo.MeetCount.HasValue ? clientData.CurrentClients.Count(c => MeetsIndividualParameters(eventInfo, c)) : null;
            int? prevMeetCount = eventInfo.PrevMeetCount.HasValue ? clientData.PreviousClients.Count(c => MeetsIndividualParameters(eventInfo, c)) : null;
            return meetCount == eventInfo.MeetCount && prevMeetCount == eventInfo.PrevMeetCount;
        }

        static bool MeetsIndividualParameters(EventTriggerData eventInfo, BasicClientData client) =>
            eventInfo.HasParamValueOrNotSet(nameof(BasicClientData.Ssid), client.Ssid) &&
                eventInfo.HasParamValueOrNotSet(nameof(BasicClientData.NetworkName), client.NetworkName) &&
                    eventInfo.HasParamValueOrNotSet(nameof(BasicClientData.Name), client.Name) &&
                        eventInfo.HasParamValueOrNotSet(nameof(BasicClientData.Mac), client.Mac);
    }
}

public static class SchedulerRegistrationExtensions
{
    public static IServiceCollection AddJobScheduler(this IServiceCollection serviceCollection, IConfigurationManager config) =>
        serviceCollection.AddHostedService<SchedulerService>()
                         .AddSingleton<SchedulerService.CommandJob>()
                         .Configure<SchedulesConfig>(config.GetSection(SchedulesConfig.ScheduleConfigName));
}
