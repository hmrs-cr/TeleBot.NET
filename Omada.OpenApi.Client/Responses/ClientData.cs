namespace Omada.OpenApi.Client.Responses;

public record ClientData
{
    public required string Mac { get; init; }
    public required string Name { get; init; }
    public required string HostName { get; init; }
    public required string DeviceType { get; init; }
    public required string Ip { get; init; }
    public IReadOnlyList<string>? Ipv6List { get; init; }
    public int ConnectType { get; init; }
    public required string ConnectDevType { get; init; }
    public bool ConnectedToWirelessRouter { get; init; }
    public bool Wireless { get; init; }
    public string? Ssid { get; init; }
    public int SignalLevel { get; init; }
    public int HealthScore { get; init; }
    public int SignalRank { get; init; }
    public int WifiMode { get; init; }
    public string? ApName { get; init; }
    public string? ApMac { get; init; }
    public int RadioId { get; init; }
    public int Channel { get; init; }
    public int RxRate { get; init; }
    public int TxRate { get; init; }
    public bool PowerSave { get; init; }
    public int Rssi { get; init; }
    public int Snr { get; init; }
    public int Vid { get; init; }
    public string? Dot1xIdentity { get; init; }
    public int Activity { get; init; }
    public int TrafficDown { get; init; }
    public int TrafficUp { get; init; }
    public int Uptime { get; init; }
    public long LastSeen { get; init; }
    public int AuthStatus { get; init; }
    public bool Guest { get; init; }
    public bool Active { get; init; }
    public bool Manager { get; init; }
    public int DownPacket { get; init; }
    public int UpPacket { get; init; }
    public bool Support5g2 { get; init; }
    public string? GatewayMac { get; init; }
    public string? GatewayName { get; init; }
    public string? SwitchName { get; init; }
    public string? NetworkName { get; init; }
}
