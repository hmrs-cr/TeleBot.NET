namespace TeleBotService.Data.Redis;

public static class RegistrationExtensions
{
    public static IServiceCollection AddRedisRepositories(this IServiceCollection services, IConfigurationManager configuration) =>
        services.Configure<RedisConfig>(configuration.GetSection(RedisConfig.RedisConfigName))
                .AddSingleton<IUsersRepository, UsersRedisRepository>()
                .AddSingleton<IInternetRadioRepository, InternetRadioRedisRepository>()
                .AddSingleton<LazyRedis>();

}
