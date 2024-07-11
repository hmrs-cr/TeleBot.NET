namespace TeleBotService.Config;

public class NetClientMonitorConfig
{
    public const string NetClientMonitorConfigKeyName = "Commands:NetClientMonitorCommand";

    public int? MonitorFrequencyInSeconds { get; init; }
}
