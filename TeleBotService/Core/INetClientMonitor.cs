namespace TeleBotService;

public interface INetClientMonitor
{
    bool StartNetClientMonitor(Telegram.Bot.ITelegramBotClient botClient, long chatId);
}
