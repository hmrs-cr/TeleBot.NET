namespace TeleBotService.Config;


public class SchedulesConfig : Dictionary<string, ScheduleConfig>
{
    public const string ScheduleConfigName = "ScheduleConfig";
}

public class ScheduleConfig
{
    public required string CommandText { get; init; }

    public required string User { get; init; }

    public required string CronSchedule { get; init; }

    public bool Reply { get; init; }

    public bool Enabled { get; init; } = true;
}
