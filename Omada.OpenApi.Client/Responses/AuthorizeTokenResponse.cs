namespace Omada.OpenApi.Client.Responses;

public record AuthorizeTokenResponse : OmadaResponse<AuthorizeTokenResult> { }

public record AuthorizeTokenResult
{
    public required string AccessToken { get; init; }
    public required string TokenType { get; init; }
    public required int ExpiresIn { get; init; }
    public required string RefreshToken { get; init; }

    public AuthorizeToken ToAuthorizeToken() => new(this);

    public readonly struct AuthorizeToken
    {
        internal AuthorizeToken(AuthorizeTokenResult response)
        {
            this.Token = response.AccessToken;
            this.ExpitationTime = DateTime.Now.AddSeconds(response.ExpiresIn);
        }

        public string Token { get; }
        public DateTime ExpitationTime { get; }

        public bool IsValid => DateTime.Now < this.ExpitationTime;

        public override string ToString() => this.Token;
    }
}
