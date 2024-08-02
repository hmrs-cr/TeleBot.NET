using TeleBotService.Extensions;

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
                var keyValueEnumerator = param.SplitEnumerated('=', 2, true);
                var key = keyValueEnumerator.MoveNext() ? keyValueEnumerator.Current : [];
                var value = keyValueEnumerator.MoveNext() ? keyValueEnumerator.Current : [];

                if ("ValidTimeRange".AsSpan().Equals(key, StringComparison.Ordinal))
                {
                    this.ParseValidTimeRange(value);
                }
                else if ("Delay".AsSpan().Equals(key, StringComparison.Ordinal))
                {
                    this.ParseDelay(value);
                }
                else if ("MeetCount".AsSpan().Equals(key, StringComparison.Ordinal))
                {
                    this.MeetCount = ParseInt(value);
                }
                 else if ("PrevMeetCount".AsSpan().Equals(key, StringComparison.Ordinal))
                {
                    this.PrevMeetCount = ParseInt(value);
                }
                else
                {
                    this.eventParams[key.ToString()] = value.ToString();
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

    public bool HasParamValueOrNotSet(string paramName, string? value) =>
        (!this.eventParams.ContainsKey(paramName) || this.eventParams[paramName] == value) && !this.IsExcluded(paramName, value);

    public bool IsExcluded(string paramName, ReadOnlySpan<char> value)
    {
        if (this.eventParams.GetValueOrDefault($"Except{paramName}") is { } exceptions)
        {
            foreach (var exception in exceptions.SplitEnumerated('|'))
            {
                if (value.Equals(exception, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void ParseValidTimeRange(ReadOnlySpan<char> validTimeRangeSpan)
    {
        // TODO: Remove this when optimized
        var validTimeRange = validTimeRangeSpan.ToString();

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

    private void ParseDelay(ReadOnlySpan<char> delayStr)
    {
        if (int.TryParse(delayStr, out var delay))
        {
            this.Delay = TimeSpan.FromSeconds(delay);
        }
    }

    private static int? ParseInt(ReadOnlySpan<char> delayStr) =>
        int.TryParse(delayStr, out var value) ? value : null;
}
