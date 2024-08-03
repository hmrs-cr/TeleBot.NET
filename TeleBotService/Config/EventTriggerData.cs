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

        var parts = eventString.SplitEnumerated(':', 2);
        this.EventName = parts.MoveNext() ? parts.Current.ToString() : string.Empty;

        if (parts.MoveNext())
        {
            var parameters = parts.Current.SplitEnumerated(';', removeEmpty: true);
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

    public bool IsInValidTime
    {
        get
        {
            var startDateTime = DateTime.MinValue;
            var endDateTime = DateTime.MaxValue;
            var now = DateTime.UtcNow;

            if (this.StartValidTime.HasValue)
            {
                startDateTime = now.Date.Add(this.StartValidTime.Value.ToTimeSpan());
            }

            if (this.EndValidTime.HasValue)
            {
                endDateTime = now.Date.Add(this.EndValidTime.Value.ToTimeSpan());
                if (startDateTime > endDateTime)
                {
                    endDateTime = endDateTime.AddDays(1);
                }
            }

            return now >= startDateTime && now <= endDateTime;
        }
    }

    public bool HasParamValueOrNotSet(string paramName, string? value) =>
        (!this.eventParams.ContainsKey(paramName) || this.eventParams[paramName] == value) && !this.IsExcluded(paramName, value);

    public string? GetParamValue(string paramName) => this.eventParams.GetValueOrDefault(paramName);

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

    private void ParseValidTimeRange(ReadOnlySpan<char> validTimeRange)
    {
        var parts = validTimeRange.SplitEnumerated('-', removeEmpty: true);

        if (parts.MoveNext() && TimeOnly.TryParseExact(parts.Current, "HH:mm", out var startValidTime))
        {
            this.StartValidTime = startValidTime;
        }

        if (parts.MoveNext() && TimeOnly.TryParseExact(parts.Current, "HH:mm", out var endValidTime))
        {
            this.EndValidTime = endValidTime;
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
