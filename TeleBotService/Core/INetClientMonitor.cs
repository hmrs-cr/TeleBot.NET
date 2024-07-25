using Omada.OpenApi.Client.Responses;

namespace TeleBotService.Core;

public interface INetClientMonitor
{
    event EventHandler<IEnumerable<BasicClientData>> ClientConcected;
    event EventHandler<IEnumerable<BasicClientData>> ClientDisconcected;

    bool StartNetClientMonitor(Telegram.Bot.ITelegramBotClient botClient, long chatId);
    bool StopNetClientMonitor();
}
