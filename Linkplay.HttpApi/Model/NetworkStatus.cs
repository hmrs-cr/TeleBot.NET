namespace Linkplay.HttpApi.Model;

public record NetworkStatus
{
    public NetworkState Wifi { get; init; }
    public NetworkState Eth { get; init; }
    public string? EthStaticIP { get; init; }
    public string? EthStaticMask { get; init; }
    public string? EthStaticGateway { get; init; }
    public string? EthStaticDNS1 { get; init; }
    public string? EthStaticDNS2 { get; init; }
}
