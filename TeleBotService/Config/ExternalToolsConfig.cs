namespace TeleBotService.Config;

public record ExternalToolsConfig
{
    public const string ExternalToolsConfigName = "ExternalTools";

    public string? SpeedTest { get; init; }

    public string? ARecord { get; init; }
    public string? OpusEnc { get; init; }
    public string? MKFifo { get; init; }
    
    public string? FFMpeg { get; init; }
}
