using System.Text;
using Omada.OpenApi.Client;
using Omada.OpenApi.Client.Responses;
using TeleBotService.Core.Model;
using TeleBotService.Extensions;

namespace TeleBotService.Core.Commands.Admin;

public class GetNetClientsCommand : TelegramCommand
{
    private static string SignalLevels = "⁰¹²³⁴⁵";

    protected readonly IOmadaOpenApiClient omadaClient;

    public GetNetClientsCommand(IOmadaOpenApiClient omadaClient)
    {
        this.omadaClient = omadaClient;
    }

    public override bool IsAdmin => true;
    public override string CommandString => "/GetNetClients";

    public override string Usage => "/GetNetClients\n/GetNetClients_All";

    protected override async Task Execute(MessageContext messageContext, CancellationToken cancellationToken = default)
    {
        var response = await this.omadaClient.GetClients();
        if (response.IsOk)
        {
            if (response?.Result is {} resul && resul.TotalRows > 0)
            {
                var allClients = messageContext.Message.Text!.Contains("all", StringComparison.InvariantCultureIgnoreCase);

                var clients = resul.Data.Where(c => allClients || this.omadaClient.IsTempClient(c)).OrderBy(c => c.Ssid).ThenBy(c => c.ApName).ToList();
                var maxClientNameLen = clients.Max(c => c.Name.Length);
                var sb = new StringBuilder();
                sb.Append("<pre>");
                foreach (var client in clients)
                {
                    AppendClientData(sb, client, maxClientNameLen);
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

    protected static StringBuilder AppendClientData(StringBuilder sb, ClientData client, int maxClientNameLen) =>
        sb.Append("<b>").Append(client.Name).Append("</b>")
          .AppendSpaces(maxClientNameLen - client.Name.Length + 1)
          .Append(" <i>[")
          .Append(client.Ssid ?? client.NetworkName)
          .Append('@').Append(client.ApName ?? client.SwitchName ?? client.GatewayName).Append("]</i>")
          .Append(client.Wireless ? SignalLevels[client.SignalRank] : string.Empty)
          .AppendLine();
}
