namespace Omada.OpenApi.Client;

public record OmadaResponse<TResult>
{
    public required int ErrorCode { get; init; }
    public required string Msg { get; init; }
    public TResult? Result { get; init; }
}
