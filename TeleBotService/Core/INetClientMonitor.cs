using Omada.OpenApi.Client.Responses;

namespace TeleBotService.Core;

public interface INetClientMonitor
{
    event EventHandler<ClientConnectionParams> ClientConcected;
    event EventHandler<ClientConnectionParams> ClientDisconcected;

    bool StartNetClientMonitor(Telegram.Bot.ITelegramBotClient botClient, long chatId);
    bool StopNetClientMonitor();
}

public class ClientConnectionParams
{
    public ClientConnectionParams(IReadOnlyCollection<BasicClientData> currentClients, IReadOnlyCollection<BasicClientData> updatedClients)
    {
        this.CurrentClients = currentClients;
        this.UpdatedClients = updatedClients;
    }

    public IReadOnlyCollection<BasicClientData> CurrentClients { get; }
    public IReadOnlyCollection<BasicClientData> UpdatedClients { get; }
}
