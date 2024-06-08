namespace TeleBotService.Data;

public interface IInternetRadioRepository
{
    void SaveDiscoveredUrl(string radioId, Uri? url);
}
