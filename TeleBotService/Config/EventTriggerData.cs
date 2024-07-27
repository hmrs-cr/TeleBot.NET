namespace TeleBotService.Config;

public class EventTriggerData
{
    public static readonly EventTriggerData Empty = new(string.Empty);

    private readonly Dictionary<string, string> eventParams = [];

    public EventTriggerData(string? eventString)
    {
        if (string.IsNullOrEmpty(eventString))
        {
            return;
        }

        var parts = eventString.Split(':', 2, StringSplitOptions.TrimEntries);
        this.EventName = parts[0];

        if (parts.Length == 2)
        {
            var parameters = parts[1].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var param in parameters)
            {
                var keyValue = param.Split('=', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (keyValue[0] == "ValidTimeRange" && keyValue.Length == 2)
                {
                    this.ParseValidTimeRange(keyValue[1]);
                }
                else if (keyValue[0] == "Delay" && keyValue.Length == 2)
                {
                    this.ParseDelay(keyValue[1]);
                }
                else if (keyValue[0] == "MeetCount" && keyValue.Length == 2)
                {
                    this.MeetCount = ParseInt(keyValue[1]);
                }
                 else if (keyValue[0] == "PrevMeetCount" && keyValue.Length == 2)
                {
                    this.PrevMeetCount = ParseInt(keyValue[1]);
                }
                else
                {
                    this.eventParams[keyValue[0]] = keyValue.Length == 2 ? keyValue[1] : string.Empty;
                }
            }
        }
    }

    public string EventName { get; private set; } = string.Empty;

    public TimeOnly? StartValidTime { get; private set; }

    public TimeOnly? EndValidTime { get; private set; }

    public TimeSpan? Delay { get; private set; }

    public int? MeetCount { get; private set; }

    public int? PrevMeetCount { get; private set; }

    public bool HasParamValueOrNotSet(string paramName, string? value)
    {
        var result = !this.eventParams.ContainsKey(paramName) || this.eventParams[paramName] == value;

        if (result && this.eventParams.GetValueOrDefault($"Except{paramName}") is { } exceptions)
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

    private void ParseValidTimeRange(string validTimeRange)
    {
        var parts = validTimeRange.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 2)
        {
            if (TimeOnly.TryParseExact(parts[0], "HH:mm", out var startValidTime))
            {
                this.StartValidTime = startValidTime;
            }

            if (TimeOnly.TryParseExact(parts[1], "HH:mm", out var endValidTime))
            {
                this.EndValidTime = endValidTime;
            }
        }
    }

    private void ParseDelay(string delayStr)
    {
        if (int.TryParse(delayStr, out var delay))
        {
            this.Delay = TimeSpan.FromSeconds(delay);
        }
    }

    private static int? ParseInt(string delayStr) =>
        int.TryParse(delayStr, out var value) ? value : null;
}
