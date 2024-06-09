using System.Reflection;
using System.Text.Json.Serialization;
using Linkplay.HttpApi;
using Linkplay.HttpApi.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TeleBotService.Config;
using TeleBotService.Core;
using TeleBotService.Data;
using TeleBotService.Data.Redis;
using TeleBotService.Localization;

namespace TeleBotService;

public class TelebotServiceApp
{
    private TelebotServiceApp() { }

    private static Version? version = null;

    public static Version? Version => version ??= Assembly.GetExecutingAssembly().GetName().Version;
    public static string? VersionLabel { get; private set; }
    public static string? VersionHash { get; private set; }

    public static ILogger? Logger { get; private set; }

    public static WebApplication? App { get; private set; }
    public static bool IsDev { get; private set; }

    public static void Run(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddCustomLogger()
               .AddServices();

        App = builder.Build()
                     .AddUseSwaggerForDevelopment()
                     .AddEndpoints()
                     .AddDevOnlyEndpoints();

        Logger = App.Services.GetRequiredService<ILogger<TelebotServiceApp>>();
        LogInformation("Starting service V{Version}", Version);

        VersionHash = GetVersionHash();
        VersionLabel = builder.Configuration.GetValue<string>("ServiceVersionLabel");
        IsDev = App.Environment.IsDevelopment();
        try
        {
            App.Run();
        }
        catch (ApplicationException e)
        {
            Environment.ExitCode = 1;
            LogError(e, $"Error running the service");
        }

        LogInformation($"Exiting service V{Version}. Bye bye.");
    }

    public static void LogDebug(string message, params object?[] args) => Logger?.LogDebug(message, args);
    public static void LogDebug(string message) => Logger?.LogDebug(message);
    public static void LogInformation(string message, params object?[] args) => Logger?.LogInformation(message, args);
    public static void LogInformation(string message) => Logger?.LogInformation(message);
    public static void LogWarning(string message, params object?[] args) => Logger?.LogWarning(message, args);
    public static void LogWarning(Exception e, string message, params object?[] args) => Logger?.LogWarning(e, message, args);
    public static void LogWarning(Exception e, string message) => Logger?.LogWarning(e, message);
    public static void LogWarning(string message) => Logger?.LogWarning(message);
    public static void LogError(string message, params object?[] args) => Logger?.LogError(message, args);
    public static void LogError(Exception e, string message, params object?[] args) => Logger?.LogError(e, message, args);
    public static void LogError(Exception e, string message) => Logger?.LogError(e, message);
    public static void LogError(string message) => Logger?.LogError(message);

    private static string? GetVersionHash()
    {
        var result = "NONE";
        try
        {
            result = File.ReadAllText("buildinfo").Trim();
        }
        catch
        {
            // Ignore
        }

        return result;
    }
}

public static class RegistrationExtensions
{
    public static IServiceCollection AddSwagger(this IServiceCollection services) => services.AddEndpointsApiExplorer().AddSwaggerGen();

    public static IHostApplicationBuilder AddCustomLogger(this IHostApplicationBuilder builder)
    {
        builder.Logging
               .ClearProviders()
               .AddConsole();

        return builder;
    }

    public static IHostApplicationBuilder AddServices(this IHostApplicationBuilder builder)
    {
        builder.Services
               .AddSwagger()
               .AddTelegramServiceInfrastructure(builder.Configuration);

        return builder;
    }

    public static IServiceCollection AddInternertRadioConfig(this IServiceCollection services, IConfigurationManager config)
    {
        config.AddJsonFile("appsettings.InternetRadio.json", optional: true, reloadOnChange: false);
        return services.Configure<InternetRadioConfig>(config.GetSection(InternetRadioConfig.InternetRadioConfigName));
    }

    public static IServiceCollection AddTelegramServiceInfrastructure(this IServiceCollection services, IConfigurationManager configuration) =>
        services.Configure<TelegramConfig>(configuration.GetSection(TelegramConfig.TelegramConfigName))
                .Configure<MusicPlayersConfig>(configuration.GetSection(MusicPlayersConfig.MusicPlayersConfigName))
                .Configure<TapoConfig>(configuration.GetSection(TapoConfig.TapoConfigName))
                .Configure<ExternalToolsConfig>(configuration.GetSection(ExternalToolsConfig.ExternalToolsConfigName))
                .Configure<UsersConfig>(configuration.GetSection(UsersConfig.UsersConfigName))
                .AddSingleton<ITelegramService, TelegramService>()
                .AddHostedService(s => s.GetService<ITelegramService>()!)
                .AddMemoryCache()
                .AddRedisRepositories(configuration)
                .AddInternertRadioConfig(configuration)
                .AddCommandTextMappings(configuration)
                .RegisterTelegramCommands()
                .ConfigureHttpJsonOptions(options =>
                {
                    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.SerializerOptions.Converters.Add(new HexedStringJsonConverter());
                    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                });

    public static WebApplication AddUseSwaggerForDevelopment(this WebApplication app)
    {
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        return app;
    }

    public static WebApplication AddEndpoints(this WebApplication app)
    {
        app.MapGet("/info", async ([FromServices] ITelegramService ts) => await ts.GetInfo()).WithName("GetInfo").WithOpenApi();
        app.MapGet("/commands", ([FromServices] ITelegramService ts) => ts.GetCommands()).WithName("GetCommands").WithOpenApi();
        return app;
    }

    public static WebApplication AddDevOnlyEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapGet("test", () =>
            {

                ///var account = new Account("camera_ip", "camera_username", "camera_password");
                /*var camera = Camera.Create(account, ex =>
                {
                    // exception
                });*/


                //var client = new LinkplayHttpApiClient("192.168.100.104");
                //return client.GetDeviceStatus();

            }).WithName("GetNetworkStatus").WithOpenApi();
        }

        return app;
    }
}
