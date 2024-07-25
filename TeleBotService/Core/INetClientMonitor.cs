using Omada.OpenApi.Client.Responses;

namespace TeleBotService.Core;

public interface INetClientMonitor
{
    event EventHandler<BasicClientData> ClientConcected;
    event EventHandler<BasicClientData> ClientDisconcected;

    bool StartNetClientMonitor(Telegram.Bot.ITelegramBotClient botClient, long chatId);
    bool StopNetClientMonitor();
}
