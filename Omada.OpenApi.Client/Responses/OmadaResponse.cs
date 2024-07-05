namespace Omada.OpenApi.Client;

public interface IResponse
{
    int ErrorCode { get; }
    string Msg { get; }
    bool IsOk { get; }
}

public record OmadaResponse<TResult> : IResponse
{
    public bool IsOk => this.ErrorCode == 0;
    public int ErrorCode { get; init; }
    public required string Msg { get; init; }
    public TResult? Result { get; init; }
}