using System.Text;

namespace Omada.OpenApi.Client.Config;

public class OmadaClientConfig
{
    public const string OmadaClientConfigName = "Omada:ClientConfig";

    public required Uri BaseUrl { get; init; }

    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public required string OmadacId { get; init; }
}
