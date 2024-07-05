namespace Omada.OpenApi.Client.Responses;

public record PagedResult<TData>
{
    public required int TotalRows { get; init; }
    public required int CurrentPage { get; init; }
    public required int CurrentSize { get; init; }
    public required IReadOnlyCollection<TData> Data { get; init; }
}
