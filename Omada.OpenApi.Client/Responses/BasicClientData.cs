using System.Text.Json.Serialization;

namespace Omada.OpenApi.Client.Responses;

public record BasicClientData
{
    public required string Mac { get; init; }
    public required string Name { get; init; }
    public bool Wireless { get; init; }
    public string? Ssid { get; init; }
    public int SignalRank { get; init; }
    public string? ApName { get; init; }
    public long LastSeen { get; init; }
    public bool Active { get; init; }
    public string? GatewayName { get; init; }
    public string? SwitchName { get; init; }
    public string? NetworkName { get; init; }

    public DateTimeOffset LastSeenDateTime => DateTimeOffset.FromUnixTimeMilliseconds(this.LastSeen);

    [JsonIgnore]
    public int? Index { get; private set; }

    public BasicClientData SetIndex(int i)
    {
        this.Index = i;
        return this;
    }
}
