using Microsoft.Extensions.Options;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;
using Quartz.Spi;
using TeleBotService.Config;

namespace TeleBotService.Core;

public class SchedulerService : IHostedService
{
    private readonly IServiceProvider serviceProvider;
    private readonly SchedulesConfig config;
    private IScheduler? scheduler;

    public SchedulerService(IServiceProvider serviceProvider, IOptions<SchedulesConfig> config, ILogger<SchedulerService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.config = config.Value;
        LogProvider.SetCurrentLogProvider(new MyLogProvider(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var schedule in this.config.Where(s => s.Value.Enabled))
        {
            var job = JobBuilder.Create<CommandJob>()
                                .WithIdentity(schedule.Key, "group1")
                                .Build();


            job.JobDataMap[nameof(ScheduleConfig)] = schedule.Value;
            var trigger = TriggerBuilder.Create()
                                        .WithIdentity($"{schedule.Key}_Trigger", "group1")
                                        .StartNow()
                                        .WithCronSchedule(schedule.Value.CronSchedule)
                                        .Build();

            await this.ScheduleJob(job, trigger, cancellationToken);
        }

        _ = this.scheduler?.Start(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => this.scheduler?.Shutdown(cancellationToken) ?? Task.CompletedTask;

    private async Task ScheduleJob(IJobDetail job, ITrigger trigger, CancellationToken cancellationToken)
    {
        if (this.scheduler == null)
        {
            this.scheduler = await SchedulerBuilder.Create()
                                                   .UseDefaultThreadPool(o => o.MaxConcurrency = 2)
                                                   .BuildScheduler();

            scheduler.JobFactory = new JobFactory(this.serviceProvider);
        }

        await this.scheduler.ScheduleJob(job, trigger, cancellationToken);
    }

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
            var result = await this.telegramService.ExecuteCommand(config.CommandText, config.User, config.Reply);
            this.logger.LogDebug("Automatically executed command '{commandText}' by user '{user}' with result '{commandResult}'", config.CommandText, config.User, result);
        }
    }

    private class MyLogProvider : ILogProvider
    {
        private static Dictionary<Quartz.Logging.LogLevel, Microsoft.Extensions.Logging.LogLevel> LogLevelMap = new ()
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


public static class SchedulerRegistrationExtensions
{
    public static IServiceCollection AddJobScheduler(this IServiceCollection serviceCollection, IConfigurationManager config) =>
        serviceCollection.AddHostedService<SchedulerService>()
                         .AddSingleton<SchedulerService.CommandJob>()
                         .Configure<SchedulesConfig>(config.GetSection(SchedulesConfig.ScheduleConfigName));
}
