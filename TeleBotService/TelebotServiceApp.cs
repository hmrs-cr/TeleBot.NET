﻿using System.Reflection;
using System.Text.Json.Serialization;
using Linkplay.HttpApi;
using Linkplay.HttpApi.Json;
using Microsoft.AspNetCore.Mvc;
using TeleBotService.Config;
using TeleBotService.Core;
using TeleBotService.Localization;

namespace TeleBotService;

public static class TelebotServiceApp
{
    private static Version? version = null;

    public static Version? Version => version ??= Assembly.GetExecutingAssembly().GetName().Version;
    public static string? VersionLabel { get; private set; }
    public static string? VersionHash { get; private set; }

    public static WebApplication? App { get; private set; }
    public static bool IsDev { get; private set; }

    public static void Run(string[] args)
    {
        Console.WriteLine($"Starting service V{Version}");

        var builder = WebApplication.CreateBuilder(args);
        builder.Services
               .AddSwagger()
               .AddTelegramServiceInfrastructure(builder.Configuration);

        App = builder.Build()
                     .AddUseSwaggerForDevelopment()
                     .AddEndpoints()
                     .AddDevOnlyEndpoints();

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
            Console.WriteLine($"Error: {e.Message}");
        }

        Console.WriteLine($"Exiting service V{Version}. Bye bye.");
    }

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

    public static IServiceCollection AddSwagger(this IServiceCollection services) => services.AddEndpointsApiExplorer().AddSwaggerGen();

    public static IServiceCollection AddTelegramServiceInfrastructure(this IServiceCollection services, IConfiguration configuration) =>
        services.Configure<TelegramConfig>(configuration.GetSection(TelegramConfig.TelegramConfigName))
                .Configure<MusicPlayersConfig>(configuration.GetSection(MusicPlayersConfig.MusicPlayersConfigName))
                .Configure<TapoConfig>(configuration.GetSection(TapoConfig.TapoConfigName))
                .AddSingleton<ITelegramService, TelegramService>()
                .AddHostedService(s => s.GetService<ITelegramService>()!)
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
                var client = new LinkplayHttpApiClient("192.168.100.104");
                return client.GetDeviceStatus();

            }).WithName("GetNetworkStatus").WithOpenApi();
        }

        return app;
    }
}
