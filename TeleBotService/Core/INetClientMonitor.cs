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
    public ClientConnectionParams(
        IReadOnlyCollection<BasicClientData> previousClients,
        IReadOnlyCollection<BasicClientData> currentClients,
        IReadOnlyCollection<BasicClientData> updatedClients)
    {
        this.PreviousClients = previousClients;
        this.CurrentClients = currentClients;
        this.UpdatedClients = updatedClients;
    }

    public IReadOnlyCollection<BasicClientData> PreviousClients { get; }
    public IReadOnlyCollection<BasicClientData> CurrentClients { get; }

    /// <summary>
    /// Get the list of client that where added or removed.
    /// </summary>
    public IReadOnlyCollection<BasicClientData> UpdatedClients { get; }
}
