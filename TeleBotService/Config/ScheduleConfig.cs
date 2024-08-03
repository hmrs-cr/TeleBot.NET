namespace TeleBotService.Config;

public class SchedulesConfig : Dictionary<string, ScheduleConfig>
{
    public const string ScheduleConfigName = "ScheduleConfig";
}

public class ScheduleConfig
{
    private EventTriggerData? eventTriggerInfo;

    public required string CommandText { get; init; }

    public required string User { get; init; }

    public string? CronSchedule { get; init; }

    public string? EventTrigger { get; init; }

    public bool Reply { get; init; }

    public bool Enabled { get; init; } = true;

    public EventTriggerData EventTriggerInfo => this.eventTriggerInfo ??= string.IsNullOrEmpty(this.EventTrigger) ? EventTriggerData.Empty : new(this.EventTrigger);
}
