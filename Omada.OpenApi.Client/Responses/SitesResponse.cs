using Omada.OpenApi.Client.Responses;

namespace Omada.OpenApi.Client.Responses;

public record SitesResponse : OmadaResponse<PagedResult<SitesData>> { }
