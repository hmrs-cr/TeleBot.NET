using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Omada.OpenApi.Client;
using Omada.OpenApi.Client.Responses;
using TeleBotService.Config;
using TeleBotService.Core.Model;
using TeleBotService.Data;
using TeleBotService.Data.Redis;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Commands.Admin;

public class NetClientMonitorCommand : GetNetClientsCommand, INetClientMonitor
{
    private static readonly NetClientsMonitorData MonitorData = new();
    private static readonly object taskCreationLock = new();

    private Dictionary<string, BasicClientData>? prevClientList;
    private readonly NetClientMonitorConfig config;

    public event EventHandler<ClientConnectionParams>? ClientConcected;
    public event EventHandler<ClientConnectionParams>? ClientDisconcected;

    public NetClientMonitorCommand(
        IOptions<NetClientMonitorConfig> config,
        IOmadaOpenApiClient omadaClient,
        INetClientRepository netClientRepository,
        ILogger<NetClientMonitorCommand> logger) : base(omadaClient, netClientRepository, logger: logger)
    {
        this.config = config.Value;
    }

    public override string CommandString => string.Empty;

    public override string Usage => "/NetClients_Monitor_Start\n/NetClients_Monitor_End";

    public override bool CanExecuteCommand(Message message) => message.Text == "/NetClients_Monitor_Start" || message.Text == "/NetClients_Monitor_End";

    private bool StopNetClientMonitor(MessageContext? messageContext)
    {
        lock (taskCreationLock)
        {
            if (messageContext == null)
            {
                MonitorData.CancellationTokenSource?.Cancel();
                MonitorData.MonitorTask = null;
                return true;
            }

            var chatId = messageContext.Message.Chat.Id;
            if (MonitorData.NotifyUserList.Remove(chatId))
            {
                this.LogInformation("Ended NetClient Monitor for chat {chatId}", chatId);
                if (MonitorData.NotifyUserList.Count == 0)
                {
                    messageContext.User.RemoveSetting(UserData.NetClientMonitorChatIdKeyName);
                    MonitorData.CancellationTokenSource = null;
                }

                return true;
            }

        }

        return false;
    }

    private bool StartNetClientMonitor(MessageContext messageContext)
    {
        var started = this.StartNetClientMonitor(messageContext.BotClient, messageContext.Message.Chat.Id);
        if (started)
        {
            messageContext.User.SetSetting(UserData.NetClientMonitorChatIdKeyName, messageContext.Message.Chat.Id);
        }

        return started;
    }

    public bool StartNetClientMonitor(Telegram.Bot.ITelegramBotClient botClient, long chatId)
    {
        lock (taskCreationLock)
        {
            MonitorData.MonitorTask ??= this.StartNotifyTask(botClient, MonitorData);
            if (chatId > 0 && MonitorData.NotifyUserList.Add(chatId))
            {
                this.LogInformation("Starting monitor NetClient Monitor for chat {chatId}", chatId);
                return true;
            }
        }

        return false;
    }

    public bool StopNetClientMonitor() => this.StopNetClientMonitor(null);

    protected override Task Execute(MessageContext messageContext, CancellationToken cancellationToken = default)
    {
        if (messageContext.Message.Text == "/NetClients_Monitor_Start")
        {
            var started = this.StartNetClientMonitor(messageContext);
            _ = this.Reply(messageContext, $"Started:{started}");
        }
        else if (messageContext.Message.Text == "/NetClients_Monitor_End")
        {
            var stoped = this.StopNetClientMonitor(messageContext);
            _ = this.Reply(messageContext, $"Stoped:{stoped}");
        }

        return Task.CompletedTask;
    }

    private async Task StartNotifyTask(Telegram.Bot.ITelegramBotClient botClient, NetClientsMonitorData monitorData)
    {
        monitorData.CancellationTokenSource ??= new CancellationTokenSource();

        while (monitorData.CancellationTokenSource != null && !monitorData.CancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                var clientsResponse = await this.omadaClient.GetClients();

                IReadOnlyCollection<BasicClientData>? prevClientList = null;
                var clients = clientsResponse.Result!.Data;
                var currClientList = clients.ToDictionary(c => c.Mac);
                this.prevClientList ??= currClientList;

                var addedClients = currClientList.Where(c => !this.prevClientList.ContainsKey(c.Key)).OrderByDescending(c => c.Value.Name.Length).Select((c, i) => c.Value.SetIndex(i)).ToList();
                var removedClients = this.prevClientList.Where(c => !currClientList.ContainsKey(c.Key)).OrderByDescending(c => c.Value.Name.Length).Select((c, i) => c.Value.SetIndex(i)).ToList();

                var maxClientNameLen = 0;
                StringBuilder? messageBuilder = null;

                if (addedClients.Count > 0)
                {
                    foreach (var clientAdded in addedClients)
                    {
                        DisconectedClientsRemove(clientAdded);
                        _ = this.netClientRepository.RemoveDisconnectedNetClientInfo(clientAdded);
                        if (monitorData.NotifyUserList.Count > 0)
                        {
                            AppendClientInfo("NEWLY CONNECTED CLIENTS:", clientAdded, false, ref maxClientNameLen, ref messageBuilder);
                        }
                        this.LogDebug("Added ClientData: {clientData}", JsonSerializer.Serialize(clientAdded, this.jsonSerializerOptions));
                    }

                    prevClientList ??= this.prevClientList.Select(kv => kv.Value).ToList();
                    this.ClientConcected?.Invoke(this, new(previousClients: prevClientList, currentClients: clients, updatedClients: addedClients));
                }

                if (removedClients.Count > 0)
                {
                    foreach (var clientRemoved in removedClients)
                    {
                        DisconectedClientsAdd(clientRemoved);
                        _ = this.netClientRepository.SaveDisconnectedNetClientInfo(clientRemoved);
                        if (monitorData.NotifyUserList.Count > 0)
                        {
                            AppendClientInfo("JUST DISCONNECTED CLIENTS:", clientRemoved, true, ref maxClientNameLen, ref messageBuilder);
                        }

                        this.LogDebug("Removed ClientData: {clientData}", JsonSerializer.Serialize(clientRemoved, this.jsonSerializerOptions));
                    }

                    prevClientList ??= this.prevClientList.Select(kv => kv.Value).ToList();
                    this.ClientDisconcected?.Invoke(this, new(previousClients: prevClientList, currentClients: clients, updatedClients: removedClients));
                }

                if (messageBuilder?.Length > 0)
                {
                    messageBuilder.Append("</pre>");
                    var message = messageBuilder.ToString();

                    foreach (var chatId in monitorData.NotifyUserList)
                    {
                        await this.ReplyFormated(botClient, chatId, message);
                    }
                }

                this.prevClientList = currClientList;
            }
            catch (Exception e)
            {
                this.LogSimpleException(e, "Unhandled error on StartNotifyTask");
            }

            await Task.Delay(TimeSpan.FromSeconds(this.config?.MonitorFrequencyInSeconds ?? 60));
        }
    }

    private static void AppendClientInfo(
        string headerText,
        BasicClientData clientRemoved,
        bool appendExtraLine,
        ref int maxClientNameLen,
        ref StringBuilder? messageBuilder)
    {
        if (clientRemoved.Index == 0)
        {
            messageBuilder ??= new StringBuilder().Append("<pre>");
            if (appendExtraLine && messageBuilder.Length > 10)
            {
                messageBuilder.AppendLine();
            }

            messageBuilder.AppendLine(headerText);
            maxClientNameLen = clientRemoved.Name.Length;
        }

        AppendClientData(messageBuilder!, clientRemoved, maxClientNameLen);
    }

    private class NetClientsMonitorData
    {
        public CancellationTokenSource? CancellationTokenSource { get; set; }

        public Task? MonitorTask { get; set; }

        public HashSet<long> NotifyUserList { get; } = [];
    }
}

public static class NetClientMonitorExtensions
{
    public static IServiceCollection AddNetClientMonitor(this IServiceCollection services, IConfigurationManager configuration) =>
        services.Configure<NetClientMonitorConfig>(configuration.GetSection(NetClientMonitorConfig.NetClientMonitorConfigKeyName))
                .AddSingleton<INetClientMonitor>(sp => sp.GetService<NetClientMonitorCommand>()!)
                .AddSingleton<INetClientRepository, NetClientRedisRepository>();
}
