namespace Omada.OpenApi.Client.Responses;

public record AuthorizeTokenResponse : OmadaResponse<AuthorizeTokenResult> { }

public record AuthorizeTokenResult
{
    public required string AccessToken { get; init; }
    public required string TokenType { get; init; }
    public required int ExpiresIn { get; init; }
    public required string RefreshToken { get; init; }
}
