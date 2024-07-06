using System.Text;
using System.Text.Json;
using Omada.OpenApi.Client;
using Omada.OpenApi.Client.Responses;
using TeleBotService.Core.Model;
using TeleBotService.Extensions;

namespace TeleBotService.Core.Commands.Admin;

public class GetNetClientsCommand : TelegramCommand
{
    private static string SignalLevels = "⁰¹²³⁴⁵";

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

    public override string Usage => "/GetNetClients\n/GetNetClients_All\nGetNetClients_Raw";

    protected override async Task Execute(MessageContext messageContext, CancellationToken cancellationToken = default)
    {
        var response = await this.omadaClient.GetClients();
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
                        AppendClientData(sb, client, maxClientNameLen);
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

    private IReadOnlyCollection<BasicClientData>? FilterClientList(MessageContext messageContext, ClientsResponse response)
    {
        if (response?.Result is { } result && result.TotalRows > 0)
        {
            var allClients = messageContext.Message.Text!.Contains("all", StringComparison.InvariantCultureIgnoreCase);
            var clients = result.Data.Where(c => allClients || this.omadaClient.IsTempClient(c)).OrderBy(c => c.Ssid).ThenBy(c => c.ApName).ToList();
            return clients;
        }

        return null;
    }

    protected static StringBuilder AppendClientData(StringBuilder sb, BasicClientData client, int maxClientNameLen) =>
        sb.Append("<b>").Append(client.Name).Append("</b>")
          .AppendSpaces(maxClientNameLen - client.Name.Length + 1)
          .Append(" <i>[")
          .Append(client.Ssid ?? client.NetworkName)
          .Append('@').Append(client.ApName ?? client.SwitchName ?? client.GatewayName).Append("]</i>")
          .Append(client.Wireless ? SignalLevels[client.SignalRank] : string.Empty)
          .AppendLine();
}
