namespace Omada.OpenApi.Client;

public record SitesData
{
    public required string SiteId { get; init; }
    public required string Name { get; init; }
    public required string Region { get; init; }
    public required string TimeZone { get; init; }
    public required string Scenario { get; init; }
    public required int Type { get; init; }
}
