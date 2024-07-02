using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Omada.OpenApi.Client.Config;
using Omada.OpenApi.Client.Requests;
using Omada.OpenApi.Client.Responses;

namespace Omada.OpenApi.Client;

public class OmadaOpenApiClient
{
    private readonly HttpClient httpClient;
    private readonly OmadaClientConfig config;

    private readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

    };

    public OmadaOpenApiClient(IOptions<OmadaClientConfig> config)
    {
        HttpClientHandler handler = new HttpClientHandler
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => true
        };

        this.httpClient = new HttpClient(handler);
        this.config = config.Value;
    }

    public async Task<AuthorizeTokenResponse> GetAuthorizeToken(string grantType)
    {
        var url = new UriBuilder(config.BaseUrl)
        {
            Path = "openapi/authorize/token",
            Query = $"grant_type={grantType}"
        };

        var request = new AuthorizeTokenRequest
        {
            ClientId = this.config.ClientId,
            ClientSecret = this.config.ClientSecret,
            OmadacId = this.config.OmadacId,

        };

        var response = await this.httpClient.PostAsJsonAsync(url.Uri, request, CancellationToken.None).ConfigureAwait(false);
        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        var authorizeTokenResponse = await JsonSerializer.DeserializeAsync<AuthorizeTokenResponse>(stream, this.jsonOptions).ConfigureAwait(false);

        return authorizeTokenResponse!;
    }
}
