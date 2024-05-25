﻿using Microsoft.Extensions.Options;
using TeleBotService.Config;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Commands;

public class MusicPlayerSeekCommand : MusicPlayerCommandBase
{
    public MusicPlayerSeekCommand(IOptions<MusicPlayersConfig> config, IOptions<TapoConfig> tapoConfig) : base(config, tapoConfig) { }    

    public override bool CanExecuteCommand(Message message) => 
        ContainsText(message, "song") && (ContainsText(message, "forward") || ContainsText(message, "backward"));

    public override string Description => "Backward/forward current song";
    public override string Usage => "{forward} {song} [{seconds}]\n{backward} {song} [{seconds}]";

    protected override async Task<int> ExecuteMusicPlayerCommand(Message message, PlayersConfig playerConfig, MusicPlayersPresetConfig? preset, CancellationToken cancellationToken = default) 
    {
        await this.ExecutePlayerClientCommand(message, playerConfig, async (pc) => 
        {
            int? newPos = null;
            var offsetPos = ParseLastInt(message).GetValueOrDefault(5);
            var status = await playerConfig.Client.GetPlayerStatus();
            var curPos = status.Curpos / 1000;
            if (ContainsText(message, "forward"))
            {
                newPos = curPos + offsetPos;
            }
            else if (ContainsText(message, "backward"))
            {
                newPos = curPos - offsetPos;
            }
            
            return newPos.HasValue ? playerConfig.Client.PlayerSeek((uint)newPos) : Task.CompletedTask;
        });
        
        return ReplyPlayerStatusDelayShort;
    }
}