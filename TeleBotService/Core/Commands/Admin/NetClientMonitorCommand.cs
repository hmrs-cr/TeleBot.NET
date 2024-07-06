using System.Text;
using Omada.OpenApi.Client;
using Omada.OpenApi.Client.Responses;
using TeleBotService.Core.Model;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Commands.Admin;

public class NetClientMonitorCommand : GetNetClientsCommand
{
    private static readonly object taskCreationLock = new();

    private Dictionary<string, BasicClientData>? prevClientList;
    private readonly ILogger<NetClientMonitorCommand> logger;

    public NetClientMonitorCommand(IOmadaOpenApiClient omadaClient, ILogger<NetClientMonitorCommand> logger) : base(omadaClient)
    {
        this.logger = logger;
    }

    public override string CommandString => string.Empty;

    public override string Usage => "/NetClients_Monitor_Start\n/NetClients_Monitor_End";

    public override bool CanExecuteCommand(Message message) => message.Text == "/NetClients_Monitor_Start" || message.Text == "/NetClients_Monitor_End";

    protected override Task Execute(MessageContext messageContext, CancellationToken cancellationToken = default)
    {
        if (messageContext.Message.Text == "/NetClients_Monitor_Start")
        {
            this.StartNetClientMonitor(messageContext);
        }
        else if (messageContext.Message.Text == "/NetClients_Monitor_End")
        {
            this.StopNetClientMonitor(messageContext);
        }

        return Task.CompletedTask;
    }

    private void StopNetClientMonitor(MessageContext messageContext)
    {
        lock (taskCreationLock)
        {
            var monitorData = messageContext.Context.GetCommandContextData<NetClientsMonitorData>(this);
            monitorData.NotifyUserList.Remove(messageContext.Message.Chat.Id);
            if (monitorData.NotifyUserList.Count == 0)
            {
                monitorData.CancellationTokenSource?.Cancel();
                monitorData.NotifyTask = null;
                monitorData.CancellationTokenSource = null;
            }
        }
    }

    private void StartNetClientMonitor(MessageContext messageContext)
    {
        lock (taskCreationLock)
        {
            var monitorData = messageContext.Context.GetCommandContextData<NetClientsMonitorData>(this);
            monitorData.NotifyUserList.Add(messageContext.Message.Chat.Id);
            monitorData.NotifyTask ??= this.StartNotifyTask(monitorData);
        }
    }

    private async Task StartNotifyTask(NetClientsMonitorData monitorData)
    {
        monitorData.CancellationTokenSource ??= new CancellationTokenSource();

        while (!monitorData.CancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                var clients = await this.omadaClient.GetClients();

                var currClientList = clients.Result!.Data.ToDictionary(c => c.Mac);
                this.prevClientList ??= currClientList;

                var addedClients = currClientList.Where(c => !this.prevClientList.ContainsKey(c.Key)).OrderByDescending(c => c.Value.Name.Length).Select((c, i) => c.Value.SetIndex(i));
                var removedClients = this.prevClientList.Where(c => !currClientList.ContainsKey(c.Key)).OrderByDescending(c => c.Value.Name.Length).Select((c, i) => c.Value.SetIndex(i));

                var maxClientNameLen = 0;
                StringBuilder? messageBuilder = null;
                foreach (var clientAdded in addedClients)
                {
                    if (clientAdded.Index == 0)
                    {
                        messageBuilder ??= new StringBuilder().Append("<pre>");
                        messageBuilder.AppendLine("NEWLY CONNECTED CLIENTS:");
                        maxClientNameLen = clientAdded.Name.Length;
                    }

                    AppendClientData(messageBuilder!, clientAdded, maxClientNameLen);
                }

                foreach (var clientRemoved in removedClients)
                {
                    if (clientRemoved.Index == 0)
                    {
                        messageBuilder ??= new StringBuilder().Append("<pre>");
                        if (messageBuilder.Length > 10)
                        {
                            messageBuilder.AppendLine();
                        }

                        messageBuilder.AppendLine("JUST DISCONNECTED CLIENTS:");
                        maxClientNameLen = clientRemoved.Name.Length;
                    }

                    AppendClientData(messageBuilder!, clientRemoved, maxClientNameLen);
                }

                if (messageBuilder?.Length > 0)
                {
                    messageBuilder.Append("</pre>");
                    foreach (var chatId in monitorData.NotifyUserList)
                    {
                        await this.ReplyFormated(chatId, messageBuilder.ToString());
                    }
                }

                this.prevClientList = currClientList;
                await Task.Delay(TimeSpan.FromSeconds(66));
            }
            catch (Exception e)
            {
                this.logger.LogWarning(e, "Unhandled error on StartNotifyTask");
            }
        }
    }

    private class NetClientsMonitorData
    {
        public CancellationTokenSource? CancellationTokenSource { get; set; }

        public Task? NotifyTask { get; set; }

        public HashSet<long> NotifyUserList { get; } = [];
    }
}
