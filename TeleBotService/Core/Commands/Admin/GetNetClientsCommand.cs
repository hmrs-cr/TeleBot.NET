using System.Text;
using System.Text.Json;
using Humanizer;
using Omada.OpenApi.Client;
using Omada.OpenApi.Client.Responses;
using TeleBotService.Core.Model;
using TeleBotService.Extensions;

namespace TeleBotService.Core.Commands.Admin;

public class GetNetClientsCommand : TelegramCommand
{
    private static readonly string SignalLevels = "⁰¹²³⁴⁵";

    private static object disconnectedClientsResponseLock = new();

    private static readonly Dictionary<string, BasicClientData> disconectedClients = [];

    private static ClientsResponse? disconnectedClientsResponse = null;

    private static ClientsResponse DisconnectedClientsResponse
    {
        get
        {
            ClientsResponse response;
            lock (disconnectedClientsResponseLock)
            {
                response = disconnectedClientsResponse ??= new()
                {
                    Msg = string.Empty,
                    Result = new()
                    {
                        TotalRows = disconectedClients.Count,
                        CurrentPage = 1,
                        CurrentSize = disconectedClients.Count,
                        Data = disconectedClients.Select(c => c.Value).ToList().AsReadOnly()
                    }
                };
            }

            return response;
        }
    }

    protected readonly IOmadaOpenApiClient omadaClient;

    protected readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public GetNetClientsCommand(IOmadaOpenApiClient omadaClient)
    {
        this.omadaClient = omadaClient;
    }

    public override bool IsAdmin => true;
    public override string CommandString => "/GetNetClients";

    public override string Usage => "/GetNetClients\n/GetNetClients_All\n/GetNetClients_Raw\n/GetNetClients_Disconnected";

    protected override async Task Execute(MessageContext messageContext, CancellationToken cancellationToken = default)
    {
        var disconnected = this.ContainsText(messageContext.Message, "Disconnected");
        var response = disconnected ? DisconnectedClientsResponse : await this.omadaClient.GetClients();
        if (response.IsOk)
        {
            if (this.FilterClientList(messageContext, response) is { } clients && clients.Count > 0)
            {
                var maxClientNameLen = clients.Max(c => c.Name.Length);
                var sb = new StringBuilder();
                sb.Append("<pre>");
                var isRaw = this.ContainsText(messageContext.Message, "Raw");
                if (isRaw)
                {
                    sb.Append(JsonSerializer.Serialize(clients, jsonSerializerOptions));
                }
                else
                {
                    foreach (var client in clients)
                    {
                        AppendClientData(sb, client, maxClientNameLen, disconnected);
                    }
                }
                sb.Append("</pre>");


                await this.ReplyFormated(messageContext.Message, sb.ToString(), cancellationToken);
            }
            else
            {
                _ = this.Reply(messageContext.Message, "No connected clients", cancellationToken);
            }
        }
        else
        {
            _ = this.Reply(messageContext.Message, response.Msg, cancellationToken);
        }
    }

    private List<BasicClientData>? FilterClientList(MessageContext messageContext, ClientsResponse response)
    {
        if (response?.Result is { } result && result.TotalRows > 0)
        {
            var disconnected = this.ContainsText(messageContext.Message, "Disconnected");
            var allClients = messageContext.Message.Text!.Contains("all", StringComparison.InvariantCultureIgnoreCase) || disconnected;
            var clients = result.Data.Where(c => allClients || this.omadaClient.IsTempClient(c));
            clients = disconnected ? clients.OrderByDescending(c => c.LastSeen) : clients.OrderBy(c => c.ApName).ThenBy(c => c.Ssid);
            return clients.ToList();
        }

        return null;
    }

    protected static void DisconectedClientsRemove(BasicClientData clientAdded)
    {
        lock (disconnectedClientsResponseLock)
        {
            disconectedClients.Remove(clientAdded.Mac);
            disconnectedClientsResponse = null;
        }
    }

    protected static void DisconectedClientsAdd(BasicClientData clientRemoved)
    {
        lock  (disconnectedClientsResponseLock)
        {
            disconectedClients[clientRemoved.Mac] = clientRemoved;
            disconnectedClientsResponse = null;
        }
    }

    protected static StringBuilder AppendClientData(StringBuilder sb, BasicClientData client, int maxClientNameLen, bool isDisconnected = false)
    {
        sb.Append("<b>").Append(client.Name).Append("</b>")
          .AppendSpaces(maxClientNameLen - client.Name.Length + 1);

        if (isDisconnected)
        {
            var dateTimeDisconection = DateTimeOffset.FromUnixTimeMilliseconds(client.LastSeen).ToLocalTime();
            var ago = DateTime.Now - dateTimeDisconection;
            sb.Append(": ").Append(ago.Humanize()).Append(" ago ");
        }
        else
        {
            sb.Append("<i>[")
              .Append(client.Ssid ?? client.NetworkName)
              .Append('@').Append(client.ApName ?? client.SwitchName ?? client.GatewayName).Append("]</i>")
              .Append(client.Wireless ? SignalLevels[client.SignalRank] : string.Empty);
        }

        return sb.AppendLine();
    }
}
