using System.Text.Json.Serialization;

namespace Omada.OpenApi.Client.Responses;

public record ClientData : BasicClientData
{
    public string? HostName { get; init; }
    public required string DeviceType { get; init; }
    public required string Ip { get; init; }
    public IReadOnlyList<string>? Ipv6List { get; init; }
    public int ConnectType { get; init; }
    public required string ConnectDevType { get; init; }
    public bool ConnectedToWirelessRouter { get; init; }
    public int SignalLevel { get; init; }
    public int HealthScore { get; init; }
    public int WifiMode { get; init; }
    public string? ApMac { get; init; }
    public int RadioId { get; init; }
    public int Channel { get; init; }
    public long RxRate { get; init; }
    public long TxRate { get; init; }
    public bool PowerSave { get; init; }
    public int Rssi { get; init; }
    public int Snr { get; init; }
    public int Vid { get; init; }
    public string? Dot1xIdentity { get; init; }
    public long Activity { get; init; }
    public long TrafficDown { get; init; }
    public long TrafficUp { get; init; }
    public long Uptime { get; init; }
    public int AuthStatus { get; init; }
    public bool Guest { get; init; }
    public bool Manager { get; init; }
    public long DownPacket { get; init; }
    public long UpPacket { get; init; }
    public bool Support5g2 { get; init; }
    public string? GatewayMac { get; init; }
}
