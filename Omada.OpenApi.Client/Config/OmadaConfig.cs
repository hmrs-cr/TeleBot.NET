namespace Omada.OpenApi.Client.Config;

public class OmadaConfig
{
    public const string OmadaConfigName = "Omada";

    public required OmadaClientConfig ClientConfig { get; set; }

    public HashSet<string>? PermanentNetClients { get; init; }
}
