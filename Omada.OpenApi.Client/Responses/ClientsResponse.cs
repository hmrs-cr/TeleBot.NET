namespace Omada.OpenApi.Client.Responses;

public record ClientsResponse : OmadaResponse<ClientsPagedResult>
{
    public static readonly ClientsResponse FailedResponse = new()
    {
        ErrorCode = -1,
        Msg = "Failed",
    };
}

public record ClientsPagedResult : PagedResult<BasicClientData>
{
    //public required ClientStats ClientStat { get; init; }
}
