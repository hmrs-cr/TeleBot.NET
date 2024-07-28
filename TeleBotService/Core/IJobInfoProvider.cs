namespace TeleBotService.Core;

public interface IJobInfoProvider
{
    IAsyncEnumerable<JobInfo> GetJobs();
}

public class JobInfo
{
    public required string JobKey { get; init; }

    public string? CommandText { get; init; }
    public string? CronSchedule { get; init; }
    public string? User { get; init; }
    public string? EventTrigger { get; init; }
    public bool? IsInValidTime { get; init; }

    public IEnumerable<TriggerInfo> Triggers { get; init; } = [];

    public class TriggerInfo
    {
        public required string TriggerKey { get; init; }

        public DateTimeOffset? PreviousFireTimeUtc { get; init; }

        public DateTimeOffset? NextFireTimeUtc { get; init; }
    }
}
