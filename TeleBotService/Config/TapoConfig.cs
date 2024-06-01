namespace TeleBotService.Config;

public partial class TapoConfig
{
    public const string TapoConfigName = "Tapo";
    public string? UserName { get; init; }
    public string? Password { get; init; }
    public ICollection<TapoDeviceConfig>? Devices { get; init; }
}