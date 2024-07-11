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

    public void StopNetClientMonitor(MessageContext messageContext)
    {
        lock (taskCreationLock)
        {
            var chatId = messageContext.Message.Chat.Id;
            if (MonitorData.NotifyUserList.Remove(chatId))
            {
                this.LogInformation("Ended NetClient Monitor for chat {chatId}", chatId);
                if (MonitorData.NotifyUserList.Count == 0)
                {
                    messageContext.User.RemoveSetting(UserData.NetClientMonitorChatIdKeyName);
                    MonitorData.CancellationTokenSource?.Cancel();
                    MonitorData.NotifyTask = null;
                    MonitorData.CancellationTokenSource = null;
                }
            }
        }
    }

    public void StartNetClientMonitor(MessageContext messageContext)
    {
        if (this.StartNetClientMonitor(messageContext.Message.Chat.Id))
        {
            messageContext.User.SetSetting(UserData.NetClientMonitorChatIdKeyName, messageContext.Message.Chat.Id);
        }
    }

    public bool StartNetClientMonitor(long chatId)
    {
        lock (taskCreationLock)
        {
            if (MonitorData.NotifyUserList.Add(chatId))
            {
                MonitorData.NotifyTask ??= this.StartNotifyTask(MonitorData);
                this.LogInformation("Starting monitor NetClient Monitor for chat {chatId}", chatId);
                return true;
            }
        }

        return false;
    }

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

    private async Task StartNotifyTask(NetClientsMonitorData monitorData)
    {
        monitorData.CancellationTokenSource ??= new CancellationTokenSource();

        while (monitorData.CancellationTokenSource != null && !monitorData.CancellationTokenSource.IsCancellationRequested)
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
                    DisconectedClientsRemove(clientAdded);
                    _ = this.netClientRepository.RemoveDisconnectedNetClientInfo(clientAdded);
                    if (clientAdded.Index == 0)
                    {
                        messageBuilder ??= new StringBuilder().Append("<pre>");
                        messageBuilder.AppendLine("NEWLY CONNECTED CLIENTS:");
                        maxClientNameLen = clientAdded.Name.Length;
                    }

                    AppendClientData(messageBuilder!, clientAdded, maxClientNameLen);
                    this.LogDebug("Added ClientData: {clientData}", JsonSerializer.Serialize(clientAdded, this.jsonSerializerOptions));
                }

                foreach (var clientRemoved in removedClients)
                {
                    DisconectedClientsAdd(clientRemoved);
                    _ = this.netClientRepository.SaveDisconnectedNetClientInfo(clientRemoved);
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
                    this.LogDebug("Removed ClientData: {clientData}", JsonSerializer.Serialize(clientRemoved, this.jsonSerializerOptions));
                }

                if (messageBuilder?.Length > 0)
                {
                    messageBuilder.Append("</pre>");
                    var message = messageBuilder.ToString();

                    foreach (var chatId in monitorData.NotifyUserList)
                    {
                        await this.ReplyFormated(chatId, message);
                    }
                }

                this.prevClientList = currClientList;
                await Task.Delay(TimeSpan.FromSeconds(this.config?.MonitorFrequencyInSeconds ?? 60));
            }
            catch (Exception e)
            {
                this.LogWarning(e, "Unhandled error on StartNotifyTask");
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

public static class NetClientMonitorExtensions
{
    public static IServiceCollection AddNetClientMonitor(this IServiceCollection services, IConfigurationManager configuration) =>
        services.Configure<NetClientMonitorConfig>(configuration.GetSection(NetClientMonitorConfig.NetClientMonitorConfigKeyName))
                .AddSingleton<INetClientMonitor>(sp => sp.GetService<NetClientMonitorCommand>()!)
                .AddSingleton<INetClientRepository, NetClientRedisRepository>();
}
