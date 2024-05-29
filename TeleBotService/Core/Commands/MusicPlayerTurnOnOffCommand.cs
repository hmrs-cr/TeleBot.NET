using Microsoft.Extensions.Options;
using TeleBotService.Config;
using TeleBotService.Extensions;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Commands;

public class MusicPlayerTurnOnOffCommand : MusicPlayerCommandBase
{
    public override string Description => "Turn on/off music system";
    public override string Usage => "{turn off} {music}\n{turn on} {music}";

    public MusicPlayerTurnOnOffCommand(IOptions<MusicPlayersConfig> config, IOptions<TapoConfig> tapoConfig) : base(config, tapoConfig) { }

    public override bool CanExecuteCommand(Message message) =>
        ContainsText(message, "music") && (ContainsText(message, "turn on", true) || ContainsText(message, "turn off", true));

    protected override async Task<int> ExecuteMusicPlayerCommand(Message message, PlayersConfig playerConfig, MusicPlayersPresetConfig? preset, CancellationToken cancellationToken = default)
    {
        var tapoDeviceClient = this.tapoConfig.GetDeviceByConfigId(playerConfig.TapoDevice);
        var isShutDown = ContainsText(message, "turn off", true);
        string? error = null;
        if (tapoDeviceClient != null)
        {
            try
            {
                if (isShutDown)
                {
                    await tapoDeviceClient.TurnOffAsync();
                    await Task.Delay(2000, cancellationToken);
                }
                else
                {
                    if (!await playerConfig.Client.IsConnected())
                    {
                        await tapoDeviceClient.TurnOnAsync();
                        await this.UntilOnline(playerConfig, cancellationToken: cancellationToken);
                    }
                    return ReplyPlayerStatusDelayShort;
                }
            }
            catch (Exception e)
            {
                error = $"Error tuning {(isShutDown ? "off" : "on")} '{playerConfig.Name}': {e.Message}";
                Console.WriteLine(error);
            }
        }

        var connected = await playerConfig.Client.IsConnected();
        if (isShutDown)
        {
            await this.Reply(message, connected ? this.Localize(message, "Still on: [error]").Format(new { error }) : "Turned Off!");
        }
        else
        {
            if (error != null)
            {
                await this.Reply(message, error);
            }
        }

        return DoNotReplyPlayerStatus;
    }
}
