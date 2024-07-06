using Omada.OpenApi.Client.Responses;

namespace Omada.OpenApi.Client;

public interface IOmadaOpenApiClient
{
    Task<ClientsResponse> GetClients(int page = 1, int pageSize = 100);
    bool IsTempClient(BasicClientData client);
}
