using Smdn.TPSmartHomeDevices.Tapo;
using TeleBotService.Config;
using static TeleBotService.Config.TapoConfig;

namespace TeleBotService.Extensions;

public static class ConfigExtensions
{
    private static readonly Dictionary<string, Func<TapoConfig, TapoDeviceConfig, TapoDevice>> tapoDeviceFactory = new()
    {
        { "P105", (tc, td) => new P105(td.Host!, tc.UserName!, tc.Password!) },
    };

    public static TapoDevice? GetDeviceByConfigId(this TapoConfig config, int configId)
    {
        if (!string.IsNullOrEmpty(config.UserName) && !string.IsNullOrEmpty(config.Password) && config.UserName != "secret" && config.Password != "secret")
        {
            var deviceConfig = config.Devices?.FirstOrDefault(t => t.Id == configId);
            if (deviceConfig != null)
            {
                if (deviceConfig.DeviceClient == null && !string.IsNullOrEmpty(deviceConfig.Host))
                {
                    var factory = tapoDeviceFactory!.GetValueOrDefault(deviceConfig.Type, (tc, td) => TapoDevice.Create(td.Host!, tc.UserName!, tc.Password!));
                    deviceConfig.DeviceClient = factory(config, deviceConfig);
                }

                return deviceConfig.DeviceClient;
            }
        }

        TelebotServiceApp.LogWarning("No tapo configuration set");
        return null;
    }
}
