﻿namespace TeleBotService.Config;

public class TelegramConfig
{
    public const string TelegramConfigName = "Telegram";

    public int AdminChatId { get; init; }

    public required string BotToken { get; init; }

    public string? JoinBotServicesPassword { get; init; }
    public string? JoinBotServicesWatchword { get; init; }
    public Uri? FailedJoinPasswordImageUrl { get; init; }
    public Uri? WelcomeImageUrl { get; init; }
    public string? DefaultLanguageCode { get; init; }
}
