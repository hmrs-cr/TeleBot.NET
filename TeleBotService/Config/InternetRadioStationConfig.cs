namespace TeleBotService.Config;

public class InternetRadioStationConfig : IUrlData
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public Uri? Url { get; init; }

    public bool? IsContainer { get; init; }
}


