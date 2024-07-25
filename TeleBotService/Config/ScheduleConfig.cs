using System.Runtime.CompilerServices;

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

    public EventTriggerData EventTriggerInfo => this.eventTriggerInfo ??= new EventTriggerData(this.EventTrigger);

    public class EventTriggerData
    {
        public EventTriggerData(string? eventString)
        {
            if (string.IsNullOrEmpty(eventString))
            {
                return;
            }

            var parts = eventString.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            this.EventName = parts[0];
            for (var c = 1; c < parts.Length; c++)
            {
                var keyValue = parts[c].Split('=', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                this.EventParams[keyValue[0]] = keyValue.Length > 1 ? keyValue[1] : string.Empty;
            }
        }

        public string EventName { get; init; } = string.Empty;

        public Dictionary<string, string> EventParams { get; } = [];

        public bool HasParamValueOrNotSet(string paramName, string? value)
        {
            var result = !this.EventParams.ContainsKey(paramName) || this.EventParams[paramName] == value;

            if (result && this.EventParams.GetValueOrDefault($"Except{paramName}") is { } exceptions)
            {
                foreach (var exception in exceptions.Split('|'))
                {
                    if (value == exception)
                    {
                        return false;
                    }
                }
            }

            return result;
        }

    }
}
