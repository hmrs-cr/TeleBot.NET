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

    public bool IsInValidTime
    {
        get
        {
            var triggerInfo = this.EventTriggerInfo;
            var startDateTime = DateTime.MinValue;
            var endDateTime = DateTime.MaxValue;
            var now = DateTime.UtcNow;

            if (triggerInfo.StartValidTime.HasValue)
            {
                startDateTime = now.Date.Add(triggerInfo.StartValidTime.Value.ToTimeSpan());
            }

            if (triggerInfo.EndValidTime.HasValue)
            {
                endDateTime = now.Date.Add(triggerInfo.EndValidTime.Value.ToTimeSpan());
                if (startDateTime > endDateTime)
                {
                    endDateTime = endDateTime.AddDays(1);
                }
            }

            return now >= startDateTime && now <= endDateTime;
        }
    }

    public EventTriggerData EventTriggerInfo => this.eventTriggerInfo ??= string.IsNullOrEmpty(this.EventTrigger) ? EventTriggerData.Empty : new(this.EventTrigger);
}
