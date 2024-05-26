using Linkplay.HttpApi.Model;
using Microsoft.Extensions.Options;
using TeleBotService.Config;
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


    protected readonly IDictionary<string, PlayersConfig>? playersConfig;
    protected readonly TapoConfig tapoConfig;

    protected virtual bool CanAutoTurnOn => false;

    protected MusicPlayerCommandBase(IOptions<MusicPlayersConfig> config, IOptions<TapoConfig> tapoConfig)
    {
        this.playersConfig = config.Value?.Players.ToDictionary(p => p.Name.ToLower());
        this.tapoConfig = tapoConfig.Value;
    }

    public override async Task Execute(Message message, CancellationToken cancellationToken = default)
    {
        message.Text = message.Text?.Trim();
        var text = message.Text;
        if (this.playersConfig?.Count > 0 && this.GetPlayerConfig(text) is { } playerConfig)
        {
            var preset = playerConfig.GetPreset(text);
            try
            {
                if (this.CanAutoTurnOn)
                {
                    await this.UntilOnline(playerConfig, true, cancellationToken);
                }

                var result = await this.ExecuteMusicPlayerCommand(message, playerConfig, preset, cancellationToken);
                if (result > 0)
                {
                    await this.ReplyPlayerStatus(message, playerConfig, result);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }
        else
        {
            _ = this.Reply(message, "No music players configured");
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
                        Console.WriteLine($"{playerConfig.Name} is offline triying to turn it on");
                        await tapoDeviceClient.TurnOnAsync();
                        await Task.Delay(29000, cancellationToken);
                    }

                    do
                    {
                        await Task.Delay(1000, cancellationToken);
                        isConnected = await playerConfig.Client.IsConnected();
                    } while (!isConnected && --retries > 0);

                    if (isConnected)
                    {
                        Console.WriteLine($"{playerConfig.Name} connected! ({maxRetries - retries})");
                        await Task.Delay(15000, cancellationToken);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error connecting {playerConfig.Name}: {e.Message}");
                }
            }
        }
    }

    protected abstract Task<int> ExecuteMusicPlayerCommand(
        Message message,
        PlayersConfig playersConfig,
        MusicPlayersPresetConfig? musicPlayersPresetConfig,
        CancellationToken cancellationToken = default);

    protected PlayersConfig? GetPlayerConfig(string? text) => this.playersConfig?.FirstOrDefault(pc => text?.Contains(pc.Key) == true).Value ?? this.playersConfig?.FirstOrDefault().Value;

    protected async Task ReplyPlayerStatus(
        Message message,
        PlayersConfig playerConfig,
        int? delay = null,
        CancellationToken cancellationToken = default)
    {
        if (delay.HasValue)
        {
            await Task.Delay(delay.Value, cancellationToken);
        }

        var statusMessage = string.Empty;
        var playerStatus = await playerConfig.Client.GetPlayerStatus();
        if (playerStatus == null)
        {
            statusMessage = $"'[playerName]' does not respond";
        }
        else if (playerStatus.Status == "play")
        {
            statusMessage = "Playing '[songTitle]' by '[artist]' in '[playerName]' ([playerMode]). Volume at [volume]";
        }
        else
        {
            statusMessage = "Nothing playing in '[playerName]'";
        }

        statusMessage = ResolveTokens(Localize(message, statusMessage), playerStatus, playerConfig);

        await this.Reply(message, statusMessage);
    }

    protected async Task<T?> ExecutePlayerClientCommand<T>(Message message, PlayersConfig playerConfig, Func<PlayersConfig, Task<T>> command)
    {
        var success = false;
        var result = default(T);
        try
        {
            result = await command.Invoke(playerConfig);
            success = true;
            if (result is bool boolResult)
            {
                success = boolResult;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        if (!success)
        {
            await this.Reply(message, "Something failed");
        }

        return result;
    }

    private static string ResolveTokens(string template, PlayerStatus? playerStatus, PlayersConfig playerConfig)
    {
        var tokenValues = new Dictionary<string, string>
        {
            { "playerName", playerConfig.Name }
        };

        if (playerStatus != null)
        {
            tokenValues["songTitle"] = playerStatus.Title.ToString();
            tokenValues["artist"] = playerStatus.Artist.ToString();
            tokenValues["playerMode"] = $"{playerStatus.Mode}";
            tokenValues["volume"] = $"{(playerStatus.Mute ? 0 : playerStatus.Vol)}";
        }

        return template.Format(tokenValues);
    }
}
