using Smdn.TPSmartHomeDevices.Tapo;

namespace TeleBotService.Config;

public class TapoConfig
{
    public const string TapoConfigName = "Tapo";
    public string? UserName { get; init; }
    public string? Password { get; init; }
    public ICollection<TapoDeviceConfig>? Devices { get; init; }

    public class TapoDeviceConfig
    {
        public int Id { get; init; }
        public string? Type { get; init; }
        public string? Name { get; init; }
        public string? Host { get; init; }

        public TapoDevice? DeviceClient { get; internal set; }
    }
}



