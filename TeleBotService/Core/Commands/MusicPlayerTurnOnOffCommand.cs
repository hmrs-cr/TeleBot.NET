using Microsoft.Extensions.Options;
using TeleBotService.Config;
using TeleBotService.Core.Model;
using TeleBotService.Extensions;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Commands;

public class MusicPlayerTurnOnOffCommand : MusicPlayerCommandBase
{
    public override string Description => "Turn on/off music system";
    public override string Usage => "{turn off} {music}\n{turn on} {music}";

    public MusicPlayerTurnOnOffCommand(IOptions<MusicPlayersConfig> config, IOptions<TapoConfig> tapoConfig, ILogger<MusicPlayerTurnOnOffCommand> logger) : base(config, tapoConfig, logger) { }

    public override bool CanExecuteCommand(Message message) =>
        ContainsText(message, "music") && (ContainsText(message, "turn on", true) || ContainsText(message, "turn off", true));

    protected override async Task<int> ExecuteMusicPlayerCommand(MessageContext messageContext, PlayersConfig playerConfig, MusicPlayersPresetConfig? preset, CancellationToken cancellationToken = default)
    {
        var tapoDeviceClient = this.tapoConfig.GetDeviceByConfigId(playerConfig.TapoDevice);
        var isShutDown = ContainsText(messageContext.Message, "turn off", true);
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
               this.LogWarning(e, "Error tuning {state} '{playerConfigName}'", isShutDown ? "off" : "on", playerConfig.Name);
            }
        }

        var connected = await playerConfig.Client.IsConnected();
        if (isShutDown)
        {
            await this.Reply(messageContext.Message, connected ? this.Localize(messageContext.Message, "Still on: [error]").Format(new { error }) : "Turned Off!");
        }
        else
        {
            if (error != null)
            {
                await this.Reply(messageContext.Message, error);
            }
        }

        return DoNotReplyPlayerStatus;
    }
}
