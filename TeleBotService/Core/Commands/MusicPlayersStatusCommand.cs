﻿using Microsoft.Extensions.Options;
using TeleBotService.Config;
using TeleBotService.Core.Model;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Commands;

public class MusicPlayersStatusCommand : MusicPlayerCommandBase
{
    public MusicPlayersStatusCommand(IOptions<MusicPlayersConfig> config, IOptions<TapoConfig> tapoConfig, ILogger<MusicPlayersStatusCommand> logger) : base(config, tapoConfig, logger) { }

    public override string Description => "Tell what's playing";

    public override string Usage => "Playing now";

    public override bool CanExecuteCommand(Message message) =>
        ContainsText(message, "Playing now", true);

    protected override Task<int> ExecuteMusicPlayerCommand(MessageContext message, PlayersConfig playerConfig, MusicPlayersPresetConfig? preset, CancellationToken cancellationToken = default) => ReplyPlayerStatusDelayShortTask;
}
