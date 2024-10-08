﻿using Linkplay.HttpApi.Model;
using Microsoft.Extensions.Options;
using TeleBotService.Config;
using TeleBotService.Core.Model;
using TeleBotService.Extensions;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Commands;

public abstract class MusicPlayerCommandBase : TelegramCommand
{
    protected const int ReplyPlayerStatusDelayLong = 5000;
    protected const int ReplyPlayerStatusDelay = 2000;
    protected const int ReplyPlayerStatusDelayShort = 500;
    protected const int DoNotReplyPlayerStatus = -1;

    protected static readonly Task<int> DoNotReplyPlayerStatusTask = Task.FromResult(DoNotReplyPlayerStatus);
    protected static readonly Task<int> ReplyPlayerStatusDelayShortTask = Task.FromResult(ReplyPlayerStatusDelayShort);

    protected readonly IReadOnlyDictionary<string, PlayersConfig>? playersConfig;
    protected readonly TapoConfig tapoConfig;

    protected virtual bool CanAutoTurnOn => false;

    protected MusicPlayerCommandBase(
        IOptions<MusicPlayersConfig> config,
        IOptions<TapoConfig> tapoConfig,
        ILogger logger)
    {
        this.playersConfig = config.Value?.PlayersDict;
        this.tapoConfig = tapoConfig.Value;
        this.Logger = logger;
    }

    protected override async Task Execute(MessageContext messageContext, CancellationToken cancellationToken = default)
    {
        var message = messageContext.Message;
        message.Text = message.Text?.Trim();
        var text = message.Text;
        if (this.playersConfig?.Count > 0 && this.GetPlayerConfig(message) is { } playerConfig)
        {
            var preset = playerConfig.GetPreset(text);
            try
            {
                if (this.CanAutoTurnOn && !await playerConfig.Client.IsConnected())
                {
                    var localizedTemplate = this.Localize(message, "'[playerName]' is offline. Wait a minute, I'm trying to turn it on");
                    _ = this.Reply(messageContext, localizedTemplate.Format(new { playerName = playerConfig.Name }));
                    await this.UntilOnline(playerConfig, true, cancellationToken);
                }

                var context = messageContext.Context;
                context.LastPlayerConfig = null;
                var result = await this.ExecuteMusicPlayerCommand(messageContext, playerConfig, preset, cancellationToken);
                if (context.LastPlayerConfig == null)
                {
                    context.LastPlayerConfig = playerConfig;
                }
                if (result > 0)
                {
                    _ = this.ReplyPlayerStatus(messageContext, playerConfig, result);
                }
            }
            catch (Exception e)
            {
                this.LogWarning("Error executing message '{message}': [{ExceptionType}] {ExceptionMessage}", message.Text, e.GetType().FullName, e.Message);

            }
        }
        else
        {
            _ = this.Reply(messageContext, "No music players configured");
        }
    }

    protected async Task UntilOnline(PlayersConfig playerConfig, bool autoTurnon = false, CancellationToken cancellationToken = default)
    {
        const int maxRetries = 30;
        var retries = maxRetries;

        var tapoDeviceClient = this.tapoConfig.GetDeviceByConfigId(playerConfig.TapoDevice);
        if (tapoDeviceClient != null)
        {
            var isConnected = await playerConfig.Client.IsConnected();
            if (!isConnected)
            {
                try
                {
                    if (autoTurnon)
                    {
                        this.LogInformation("{playerConfigName} is offline triying to turn it on", playerConfig.Name);
                        await ProcessExtensions.Retry(tapoDeviceClient.TurnOnAsync, waitTime: 3500, cancellationToken: cancellationToken);
                        await Task.Delay(29000, cancellationToken);
                    }

                    do
                    {
                        await Task.Delay(1000, cancellationToken);
                        isConnected = await playerConfig.Client.IsConnected();
                    } while (!isConnected && --retries > 0);

                    if (isConnected)
                    {
                        this.LogInformation("{playerConfigName} connected! ({count})", playerConfig.Name, maxRetries - retries);
                        await Task.Delay(15000, cancellationToken);
                    }
                }
                catch (Exception e)
                {
                    this.LogWarning("Error connecting {playerConfigName}: [{ExceptionType}] {ExceptionMessage}", playerConfig.Name, e.GetType().FullName, e.Message);
                }
            }
        }
    }

    protected abstract Task<int> ExecuteMusicPlayerCommand(
        MessageContext messageContext,
        PlayersConfig playersConfig,
        MusicPlayersPresetConfig? musicPlayersPresetConfig,
        CancellationToken cancellationToken = default);

    protected PlayersConfig? GetPlayerConfig(Message message) =>
        this.playersConfig?.FirstOrDefault(pc => message.Text?.Contains(pc.Key, StringComparison.OrdinalIgnoreCase) == true).Value ?? message.GetContext().LastPlayerConfig ?? this.playersConfig?.FirstOrDefault().Value;

    protected async Task ReplyPlayerStatus(
        MessageContext messageContext,
        PlayersConfig playerConfig,
        int? delay = null,
        CancellationToken cancellationToken = default)
    {
        if (delay.HasValue)
        {
            await Task.Delay(delay.Value, cancellationToken);
        }

        var loadRetries = 50;
        var statusMessage = string.Empty;
        PlayerStatus? playerStatus = null;
        while (loadRetries-- > 0 && (playerStatus = await playerConfig.Client.GetPlayerStatus())?.Status == Status.Load)
        {
            await Task.Delay(250, cancellationToken);
        }

        this.LogDebug("Player Status: {playerStatus}", playerStatus);
        if (playerStatus == null)
        {
            statusMessage = $"'[playerName]' does not respond";
        }
        else if (playerStatus.Status == Status.Play)
        {
            statusMessage = playerStatus.Mode == PlaybackMode.Network ?
                            "Playing '[netMediaName]' ([url]) in '[playerName]'. Volume at [volume]" :
                            "Playing '[title]' by '[artist]' in '[playerName]' ([playerMode]). Volume at [volume]";
        }
        else
        {
            statusMessage = "Nothing playing in '[playerName]'";
        }

        statusMessage = ResolveTokens(Localize(messageContext.Message, statusMessage), playerStatus, playerConfig);

        await this.Reply(messageContext, statusMessage);
    }

    protected async Task<T?> ExecutePlayerClientCommand<T>(MessageContext messageContext, PlayersConfig playerConfig, Func<MessageContext, Message, PlayersConfig, Task<T>> command)
    {
        var success = false;
        var result = default(T);
        try
        {
            result = await command.Invoke(messageContext, messageContext.Message, playerConfig);
            success = true;
            if (result is bool boolResult)
            {
                success = boolResult;
            }
        }
        catch (Exception e)
        {
            this.LogSimpleException(e, "Error executing player command");
        }

        if (!success)
        {
            await this.Reply(messageContext, "Something failed");
        }

        return result;
    }

    private static string ResolveTokens(string template, PlayerStatus? playerStatus, PlayersConfig playerConfig)
    {
        var tokenValues = new Dictionary<string, string>
        {
            { "playerName", playerConfig.Name ?? string.Empty }
        };

        if (playerStatus != null)
        {
            tokenValues["netMediaName"] = playerStatus.NetMediaName ?? playerStatus.Title.ToString();
            tokenValues["url"] = playerStatus.Url?.ToString() ?? "Unknown";
            tokenValues["title"] = playerStatus.Title.ToString();
            tokenValues["artist"] = playerStatus.Artist.ToString();
            tokenValues["playerMode"] = $"{playerStatus.Mode}";
            tokenValues["volume"] = $"{(playerStatus.Mute ? 0 : playerStatus.Vol)}";
        }

        return template.Format(tokenValues);
    }
}
