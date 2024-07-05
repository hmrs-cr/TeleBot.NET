using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Omada.OpenApi.Client.Config;

namespace Omada.OpenApi.Client;

public static class RegistrationExtensions
{
    public static IServiceCollection AddOmadaOpenApiClient(this IServiceCollection services, IConfigurationManager config) =>
        services.Configure<OmadaConfig>(config.GetSection(OmadaConfig.OmadaConfigName))
                .AddSingleton<IOmadaOpenApiClient, OmadaOpenApiClient>();
}
