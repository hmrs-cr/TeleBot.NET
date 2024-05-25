using Smdn.TPSmartHomeDevices.Tapo;

namespace TeleBotService.Config;

public class TapoConfig
{
    private static readonly Dictionary<string, Func<TapoConfig, TapoDeviceConfig, TapoDevice>> tapoDeviceFactory = new ()
    {
        { "P105", (tc, td) => new P105(td.Host, tc.UserName, tc.Password) },
    };

    public const string TapoConfigName = "Tapo";
    public string UserName { get; set; }
    public string Password { get; set; }
    public ICollection<TapoDeviceConfig> Devices { get; set; }

    public TapoDevice? GetDeviceByConfigId(int configId)
    {
        if (!string.IsNullOrEmpty(this.UserName) && !string.IsNullOrEmpty(this.Password) && this.UserName != "secret" && this.Password != "secret" )
        {
            var deviceConfig = this.Devices?.FirstOrDefault(t => t.Id == configId);
            if (deviceConfig != null)
            {
                if (deviceConfig.DeviceClient == null && !string.IsNullOrEmpty(deviceConfig.Host))
                {
                    var factory = tapoDeviceFactory.GetValueOrDefault(deviceConfig.Type, (tc, td) => TapoDevice.Create(td.Host, tc.UserName, tc.Password));
                    deviceConfig.DeviceClient = factory(this, deviceConfig);
                }

                return deviceConfig.DeviceClient;
            }
        }

        Console.WriteLine("No tapo configuration set");
        return null;
    }

    public class TapoDeviceConfig
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string Host { get; set; }

        public TapoDevice DeviceClient { get; internal set; }
    }
}



    