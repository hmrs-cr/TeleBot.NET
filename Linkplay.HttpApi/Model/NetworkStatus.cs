namespace Linkplay.HttpApi.Model;

public class NetworkStatus
{
    public NetworkState Wifi { get; set; }
    public NetworkState Eth { get; set; }
    public string? EthStaticIP { get; set; }
    public string? EthStaticMask { get; set; }
    public string? EthStaticGateway { get; set; }
    public string? EthStaticDNS1 { get; set; }
    public string? EthStaticDNS2 { get; set; }
}
