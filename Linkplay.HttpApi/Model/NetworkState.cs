namespace Linkplay.HttpApi.Model;

public enum NetworkState
{
    NotConnected = -1, // not connected
    StaticIp = 0, // Static IP
    DHCP = 1, // DHCP
}
