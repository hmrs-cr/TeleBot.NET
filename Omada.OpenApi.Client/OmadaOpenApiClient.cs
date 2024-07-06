using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Omada.OpenApi.Client.Config;
using Omada.OpenApi.Client.Exceptions;
using Omada.OpenApi.Client.Responses;

namespace Omada.OpenApi.Client;

public class OmadaOpenApiClient : IOmadaOpenApiClient
{
    private readonly HttpClient httpClient;
    private readonly OmadaConfig config;

    private readonly Uri clientCreadentialsAutorizeTokenUrl;

    private readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

    };
    private string? defaultSiteId;

    public OmadaOpenApiClient(IOptions<OmadaConfig> config)
    {
        this.config = config.Value;

        if (this.config.ClientConfig.AllowInvalidCertificates)
        {
            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => true
            };

            this.httpClient = new HttpClient(handler);
        }
        else
        {
            this.httpClient = new HttpClient();
        }


        this.clientCreadentialsAutorizeTokenUrl = new UriBuilder(this.config.ClientConfig.BaseUrl)
        {
            Path = "openapi/authorize/token",
            Query = "grant_type=client_credentials"
        }.Uri;
    }

    public Task<AuthorizeTokenResult.AuthorizeToken> GetClientCredentialsAuthorizeToken() => this.GetClientCredentialsAuthorizeToken(CancellationToken.None);

    public async Task<AuthorizeTokenResult.AuthorizeToken> GetClientCredentialsAuthorizeToken(CancellationToken cancellationToken)
    {
        var request = this.config.ClientConfig.AsAuthorizeTokenRequest();

        var response = await this.httpClient.PostAsJsonAsync(this.clientCreadentialsAutorizeTokenUrl, request, cancellationToken).ConfigureAwait(false);
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var authorizeTokenResponse = await JsonSerializer.DeserializeAsync<AuthorizeTokenResponse>(stream, this.jsonOptions, cancellationToken).ConfigureAwait(false);
        authorizeTokenResponse!.ThrowIfFailed();

        return authorizeTokenResponse!.Result!.ToAuthorizeToken();
    }

    public async Task<SitesResponse> GetSites(int page = 1, int pageSize = 100)
    {
        var url = this.GetPagedRequestUrl("sites", page, pageSize);
        var response = await this.Get<SitesResponse>(url).ConfigureAwait(false);
        return response!;
    }

    public async Task<ClientsResponse> GetClients(string siteId, int page = 1, int pageSize = 100)
    {
        var url = this.GetPagedRequestUrl(siteId, "clients", page, pageSize);
        //var url = new Uri("http://localhost:5247/getclients");
        var response = await this.Get<ClientsResponse>(url).ConfigureAwait(false);
        return response!;
    }

    public async Task<ClientsResponse> GetClients(int page = 1, int pageSize = 100)
    {
        var siteId = this.defaultSiteId ??= (await this.GetSites().ConfigureAwait(false)).Result?.Data?.FirstOrDefault()?.SiteId;
        if (siteId is not null)
        {
            var response = await this.GetClients(siteId, page, pageSize).ConfigureAwait(false);
            return response!;
        }

        return ClientsResponse.FailedResponse;
    }

    public bool IsTempClient(BasicClientData client) => this.config.PermanentNetClients is { } permanentNetClients && !permanentNetClients.Contains(client.Mac);

    private Uri GetPagedRequestUrl(string endPoint, int page, int pageSize) => new UriBuilder(this.config.ClientConfig.BaseUrl)
    {
        Path = $"/openapi/v1/{this.config.ClientConfig.OmadacId}/{endPoint}",
        Query = $"page={page}&pageSize={pageSize}"
    }.Uri;

    private Uri GetPagedRequestUrl(string siteId, string endPoint, int page, int pageSize) => new UriBuilder(this.config.ClientConfig.BaseUrl)
    {
        Path = $"/openapi/v1/{this.config.ClientConfig.OmadacId}/sites/{siteId}/{endPoint}",
        Query = $"page={page}&pageSize={pageSize}"
    }.Uri;

    private async Task<T?> Get<T>(Uri url) where T : IResponse
    {
        var stream = await this.GetStream(url).ConfigureAwait(false);
        var response = await JsonSerializer.DeserializeAsync<T>(stream, this.jsonOptions).ConfigureAwait(false);
        response?.ThrowIfFailed();
        return response;
    }

    private async Task<Stream> GetStream(Uri url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var response = await this.Send(request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> Send(HttpRequestMessage request)
    {
        var token = await this.GetClientCredentialsAuthorizeToken().ConfigureAwait(false);
        request.Headers.TryAddWithoutValidation("Authorization", $"AccessToken={token.Token}");
        return await this.httpClient.SendAsync(request).ConfigureAwait(false);
    }
}
