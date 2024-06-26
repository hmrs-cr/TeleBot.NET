﻿using Microsoft.Extensions.Options;
using TeleBotService.Config;
using TeleBotService.Core.Model;
using TeleBotService.Extensions;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Commands;

public class MusicPlayersPlayMusicCommand : MusicPlayerCommandBase
{
    public MusicPlayersPlayMusicCommand(IOptions<MusicPlayersConfig> config, IOptions<TapoConfig> tapoConfig, ILogger<MusicPlayersPlayMusicCommand> logger) : base(config, tapoConfig, logger) { }

    public override bool CanExecuteCommand(Message message) => ContainsText(message, "play") && ContainsText(message, "music");

    public override string Description => "Play music";

    public override string Usage => "{play} {music} [presetId/url]\n{play} {music} local [idx]";

    protected override bool CanAutoTurnOn => true;

    protected override async Task<int> ExecuteMusicPlayerCommand(MessageContext messageContext, PlayersConfig playerConfig, MusicPlayersPresetConfig? preset, CancellationToken cancellationToken = default)
    {
        var message = messageContext.Message;
        var url = preset?.Url ?? message.ParseLastUrl();
        if (url?.IsAbsoluteUri == true)
        {
            if (url.ToString().Contains(".m3u", StringComparison.OrdinalIgnoreCase))
            {
                await this.ExecutePlayerClientCommand(messageContext, playerConfig, (context, message, pc) => pc.Client.PlayPlaylist(url));
            }
            else
            {
                await this.ExecutePlayerClientCommand(messageContext, playerConfig, (context, message, pc) => pc.Client.PlayUrl(url));
            }
        }
        else if (preset != null)
        {
            await this.ExecutePlayerClientCommand(messageContext, playerConfig, (context, message, pc) => pc.Client.PlayPreset(preset.Index));
        }
        else if (ContainsText(message, "local"))
        {
            await this.ExecutePlayerClientCommand(messageContext, playerConfig, (context, message, pc) => pc.Client.PlayLocalList((uint)message.ParseLastInt().GetValueOrDefault()));
        }

        return ReplyPlayerStatusDelayLong;
    }
}
