using Omada.OpenApi.Client.Responses;

namespace TeleBotService.Data;

public interface INetClientRepository
{
    Task SaveDisconnectedNetClientInfo(BasicClientData clientData);
    Task RemoveDisconnectedNetClientInfo(BasicClientData clientData);
    IAsyncEnumerable<BasicClientData> GetDisconnectedNetClientsInfo();
}
