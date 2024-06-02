namespace TeleBotService.Localization;

public static class ResgistrationExtensions
{
    public static IServiceCollection AddCommandTextMappings(this IServiceCollection services, IConfigurationManager config)
    {
        config.AddJsonFile("appsettings.LocalizedStrings.json", optional: true, reloadOnChange: false);
        services.AddSingleton<ILocalizationResolver, SimpleLocalizationResolver>()
                .Configure<LocalizedStringsConfig>(config.GetSection(LocalizedStringsConfig.LocalizedStringsConfigName));

        return services;
    }
}
