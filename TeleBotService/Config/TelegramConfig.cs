namespace TeleBotService.Config;

public class TelegramConfig
{
    public const string TelegramConfigName = "Telegram";
    public int AdminChatId { get; set; }

    public required string BotToken { get; set; }

    public required IReadOnlyCollection<string> AllowedUsers { get; set; }
}
