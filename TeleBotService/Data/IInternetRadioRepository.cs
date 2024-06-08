namespace TeleBotService.Data;

public interface IInternetRadioRepository
{
    ValueTask SaveDiscoveredUrl(string radioId, Uri? url);
}
