using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization;
using Linkplay.HttpApi.Json;
using Microsoft.AspNetCore.Mvc;
using Omada.OpenApi.Client;
using TeleBotService.Config;
using TeleBotService.Core;
using TeleBotService.Core.Commands;
using TeleBotService.Core.Commands.Admin;
using TeleBotService.Data.Redis;
using TeleBotService.Extensions;
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

    public static string? HostName { get; private set; }

    public static string LocalConfigPath { get; } = Path.Combine(Directory.GetCurrentDirectory(), "local-config");

    public static void Run(string[] args)
    {
        DistributedContextPropagator.Current = DistributedContextPropagator.CreateNoOutputPropagator();
        var builder = WebApplication.CreateBuilder(args);
        builder.AddCustomLogger()
               .AddServices();

        App = builder.Build()
                     .AddUseSwaggerForDevelopment()
                     .AddEndpoints()
                     .AddDevOnlyEndpoints();

        Logger = App.Services.GetRequiredService<ILogger<TelebotServiceApp>>();
        var configVersion = builder.Configuration.GetValue<int>("ConfigVersion");
        VersionLabel = builder.Configuration.GetValue<string>("ServiceVersionLabel");
        HostName = builder.Configuration.GetValue<string>("HOSTNAME") ?? System.Net.Dns.GetHostName();

        LogInformation("Starting service V{Version}-{VersionLabel}. Config version: '{configVersion}'", Version, VersionLabel, configVersion);
        VersionHash = GetVersionHash();
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

    public static async Task Stop(int exitCode)
    {
        await Task.Delay(2500);
        Environment.ExitCode = exitCode;
        await App!.StopAsync(TimeSpan.FromSeconds(7));
    }

    public static void LogDebug(string message, params object?[] args) => Logger?.LogDebug(message, args);
    public static void LogDebug(string message) => Logger?.LogDebug(message);
    public static void LogInformation(string message, params object?[] args) => Logger?.LogInformation(message, args);
    public static void LogInformation(string message) => Logger?.LogInformation(message);
    public static void LogWarning(string message, params object?[] args) => Logger?.LogWarning(message, args);
    public static void LogWarning(Exception e, string message, params object?[] args) => Logger?.LogWarning(e, message, args);
    public static void LogSimpleException(Exception e, string message) => Logger?.LogSimpleException(message, e);
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

    public static IServiceCollection AddLocalFileConfig(this IServiceCollection services, IConfigurationManager config)
    {
        config.AddKeyPerFile(TelebotServiceApp.LocalConfigPath, true);
        return services;
    }

    public static IServiceCollection AddRemoteConfig(this IServiceCollection services, IConfigurationManager config)
    {
        config.AddJsonFile("appsettings.Remote.json", optional: true, reloadOnChange: false);
        return services;
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
                .AddOmadaOpenApiClient(configuration)
                .AddInternertRadioConfig(configuration)
                .AddRemoteConfig(configuration)
                .AddLocalFileConfig(configuration)
                .AddCommandTextMappings(configuration)
                .RegisterTelegramCommands()
                .AddNetClientMonitor(configuration)
                .AddVoiceMessageService()
                .AddJobScheduler(configuration)
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
        app.AddVoiceServiceEndpoints();
        return app;
    }

    public static IServiceCollection AddVoiceMessageService(this IServiceCollection services) => services.AddSingleton<IVoiceMessageService>(sp => sp.GetRequiredService<AudioMessagePlayerCommand>());
    
    public static WebApplication AddVoiceServiceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/audio-message").WithName("Audio Messages").WithTags("Audio Messages");
        if (app.Environment.IsDevelopment())
        {
            group.MapGet("/", ([FromServices] IVoiceMessageService vms) => vms.GetPendingMessages())
                .WithName("GetPendingMessages").WithOpenApi();
            group.MapGet("/{fileUniqueId}",
                    ([FromServices] IVoiceMessageService vms, string fileUniqueId) => vms.GetMessageById(fileUniqueId))
                .WithName("GetMessageById").WithOpenApi();
            
            group.MapGet("/{fileUniqueId}/download", ([FromServices] IVoiceMessageService vms, string fileUniqueId) => 
                ServeVoiceMessage(vms, fileUniqueId, download: true)).WithName("DownloadMessage").WithOpenApi();
        }

        group.MapGet("/{fileUniqueId}/stream", ([FromServices] IVoiceMessageService vms, string fileUniqueId) => ServeVoiceMessage(vms, fileUniqueId, download: false)).WithName("StreamMessage").WithOpenApi();
        
        return app;

        IResult ServeVoiceMessage(IVoiceMessageService voiceMessageService, string fileUniqueId, bool download)
        {
            var voice = voiceMessageService.GetMessageById(fileUniqueId);
            if (voice is null)
            {
                return Results.NotFound();
            }

            var stream = File.OpenRead(voice.LocalFullFilePath);
            return Results.File(stream, contentType: voice.MimeType, fileDownloadName: download ? voice.FileName ?? Path.GetFileName(voice.FilePath) : null, enableRangeProcessing: !download);
        }
    }

    public static WebApplication AddDevOnlyEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapPost("/execute-command", ([FromServices] ITelegramService ts, [FromBody] string commandText, [FromHeader] string userName, bool sentReply = false ) => ts.ExecuteCommand(commandText, userName, sentReply)).WithName("ExecuteUpdate").WithOpenApi();
            app.MapGet("test", async ([FromServices]IOmadaOpenApiClient omadaClient) =>
            {

                ///var account = new Account("camera_ip", "camera_username", "camera_password");
                /*var camera = Camera.Create(account, ex =>
                {
                    // exception
                });*/


                //var client = new LinkplayHttpApiClient("192.168.100.104");
                //return client.GetDeviceStatus();

                return await omadaClient.GetClients();

            }).WithName("GetAuthorizeToken").WithOpenApi();


            app.MapGet("getclients", () => File.ReadAllText("/Users/hectormauriciorodriguez/Projects/TeleBot/TeleBotService/GetClientsResponse.json"));

        }

        return app;
    }
}
