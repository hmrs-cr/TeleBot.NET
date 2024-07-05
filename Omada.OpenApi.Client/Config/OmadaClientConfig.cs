using System.Text;
using Omada.OpenApi.Client.Requests;

namespace Omada.OpenApi.Client.Config;

public class OmadaClientConfig
{
    private AuthorizeTokenRequest? authorizeTokenRequest;

    public required Uri BaseUrl { get; init; }

    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public required string OmadacId { get; init; }

    public bool AllowInvalidCertificates { get; init; }

    public AuthorizeTokenRequest AsAuthorizeTokenRequest() => this.authorizeTokenRequest ??= new AuthorizeTokenRequest
    {
        ClientId = this.ClientId,
        ClientSecret = this.ClientSecret,
        OmadacId = this.OmadacId,
    };
}
