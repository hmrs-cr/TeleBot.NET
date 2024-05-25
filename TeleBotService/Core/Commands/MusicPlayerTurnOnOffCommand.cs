using Linkplay.HttpApi.Model;
using Microsoft.Extensions.Options;
using TeleBotService.Config;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Commands;

public class MusicPlayerTurnOnOffCommand : MusicPlayerCommandBase
{
    public override string Description => "Turn on/off music system";
    public override string Usage => "{turn off} {music}\n{turn on} {music}";

    public MusicPlayerTurnOnOffCommand(IOptions<MusicPlayersConfig> config, IOptions<TapoConfig> tapoConfig) : base(config, tapoConfig) { }

    public override bool CanExecuteCommand(Message message) =>
        ContainsText(message, "music") && (ContainsText(message, "turn on") || ContainsText(message, "turn off"));

    protected override async Task<int> ExecuteMusicPlayerCommand(Message message, PlayersConfig playerConfig, MusicPlayersPresetConfig? preset, CancellationToken cancellationToken = default)
    {
        var tapoDeviceClient = this.tapoConfig.GetDeviceByConfigId(playerConfig.TapoDevice);
        var isShutDown = ContainsText(message, "turn off");
        if (tapoDeviceClient != null)
        {
            if (isShutDown) 
            {
                await tapoDeviceClient.TurnOffAsync();
                await Task.Delay(2000, cancellationToken);
            }
            else
            {
                 await tapoDeviceClient.TurnOnAsync();
                 await this.UntilOnline(playerConfig, cancellationToken: cancellationToken);
                 return ReplyPlayerStatusDelay;
            }
        }

        if (isShutDown)  
        {
            var connected = await playerConfig.Client.IsConnected();
            await this.Reply(message, connected ? "Still on" : "Turned Off!");
        }
        else
        {

        }

        return DoNotReplyPlayerStatus;
    }
}
