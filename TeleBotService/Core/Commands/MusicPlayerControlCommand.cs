using Linkplay.HttpApi.Model;
using Microsoft.Extensions.Options;
using TeleBotService.Config;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Commands;

public class MusicPlayerControlCommand : MusicPlayerCommandBase
{
    private readonly static Dictionary<string, PlayerControl> commandMap = new()
    {
        { $"{PlayerControl.Stop}", PlayerControl.Stop },
        { $"{PlayerControl.Next}", PlayerControl.Next },
        { $"{PlayerControl.Prev}", PlayerControl.Prev },
        { $"{PlayerControl.Pause}", PlayerControl.Pause },
        { $"{PlayerControl.Resume}", PlayerControl.Resume },
        { $"{PlayerControl.OnePause}", PlayerControl.OnePause },
    };

    public override string Description => "Controls music playback";
    public override string Usage => "{next} {song}\n{stop} {music}\n{prev} {song}\n{pause} {music}\n{resume} {music}\n{onepause} {music}";

    public MusicPlayerControlCommand(IOptions<MusicPlayersConfig> config, IOptions<TapoConfig> tapoConfig) : base(config, tapoConfig) { }

    public override bool CanExecuteCommand(Message message) =>
        (ContainsText(message, "music") || ContainsText(message, "song")) && commandMap.Any(c => ContainsText(message, c.Key));

    protected override async Task<int> ExecuteMusicPlayerCommand(Message message, PlayersConfig playerConfig, MusicPlayersPresetConfig? preset, CancellationToken cancellationToken = default)
    {
        await this.ExecutePlayerClientCommand(message, playerConfig, async (pc) =>
        {
            var command = commandMap.First(c => ContainsText(message, c.Key)).Value;
            var repeat = ParseLastInt(message).GetValueOrDefault(command.DefaultRepeat);
            if (repeat > command.MaxRepeat)
            {
                repeat = command.MaxRepeat;
            }

            for (int i = 0; i < repeat; i++)
            {
                await pc.Client.ControlPlayer(command);
                if (repeat > 1 && command.RepeatDelay > 0)
                {
                    await Task.Delay(command.RepeatDelay);
                }
            }

            return true;
        });

        return ReplyPlayerStatusDelay;
    }
}
