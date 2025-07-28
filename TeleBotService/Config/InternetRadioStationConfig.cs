namespace TeleBotService.Config;

public class InternetRadioStationConfig : IUrlData, IEquatable<InternetRadioStationConfig>
{
    private static int internalId = 0;

    public int InternalId { get; } = ++internalId;

    public required string Id { get; init; }
    public string? Name { get; init; }
    public Uri? Url { get; init; }

    public bool? IsContainer { get; init; }

    public override int GetHashCode() => Id.GetHashCode();

    public bool Equals(InternetRadioStationConfig? other) => Id == other?.Id;

    public override bool Equals(object? obj) => Equals(obj as InternetRadioStationConfig);
}


