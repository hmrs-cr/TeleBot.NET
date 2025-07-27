namespace TeleBotService.Config;

public record InternetRadioStationConfig : IUrlData
{
    private static int internalId = 0;

    public int InternalId { get; } = ++internalId;

    public required string Id { get; init; }
    public string? Name { get; init; }
    public Uri? Url { get; init; }

    public bool? IsContainer { get; init; }
}


