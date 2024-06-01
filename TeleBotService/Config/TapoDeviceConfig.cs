using Smdn.TPSmartHomeDevices.Tapo;

namespace TeleBotService.Config;

public class TapoDeviceConfig
{
    public int Id { get; init; }
    public string? Type { get; init; }
    public string? Name { get; init; }
    public string? Host { get; init; }

    public TapoDevice? DeviceClient { get; internal set; }
}
