namespace TeleBotService.Config;

public class TelegramConfig
{
    public const string TelegramConfigName = "Telegram";

    public int AdminChatId { get; init; }

    public required string BotToken { get; init; }

    public required IReadOnlyCollection<string> AllowedUsers { get; init; }
}
