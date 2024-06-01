namespace TeleBotService.Localization;

public static class ResgistrationExtensions
{
    private const string LocalizedStringMappingFileConfigName = "LocalizedStringMappingFile";

    public static IServiceCollection AddCommandTextMappings(this IServiceCollection services, IConfiguration config)
    {
        var instance = new SimpleLocalizationResolver(TelebotServiceApp.Logger);
        var fileName = config.GetValue<string>(LocalizedStringMappingFileConfigName);
        instance.LoadStringMappings(fileName);
        services.AddSingleton<ILocalizationResolver>(instance);
        return services;
    }
}
