using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TeleBotService.Config;
using TeleBotService.Core.Model;
using TeleBotService.Data;
using TeleBotService.Extensions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Commands;

public class RadioPlayerListCommand : TelegramCommand
{
    private static readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions()
    {
        WriteIndented = true,
    };
    
    private readonly InternetRadioConfig radioConfig;
    private readonly IMemoryCache memoryCache;
    private readonly IInternetRadioRepository internetRadioRepository;

    public RadioPlayerListCommand(
        IOptions<MusicPlayersConfig> config,
        IOptions<InternetRadioConfig> radioConfig,
        ILogger<RadioPlayerListCommand> logger,
        IMemoryCache memoryCache,
        IInternetRadioRepository internetRadioRepository)
    {
        this.radioConfig = radioConfig.Value;
        this.memoryCache = memoryCache;
        this.internetRadioRepository = internetRadioRepository;
        this.Logger = logger;
    }

    public override string Description => "Internet Radio";

    public override string Usage => "{list} {radio}";

    public override bool CanExecuteCommand(Message message) =>
        this.ContainsText(message, "radio") && this.ContainsText(message, "list");

    protected override async Task Execute(
        MessageContext messageContext,
        CancellationToken cancellationToken = default)
    {
        var message = messageContext.Message;
        if (this.radioConfig.Stations?.Count > 0)
        {
            var sb = new StringBuilder();
            var isDownload = this.ContainsText(message, "download");
            var cachedRadioInfo = await this.internetRadioRepository.ListStreamData();
            foreach (var radio in this.radioConfig.Stations) 
            {
                var isCached = cachedRadioInfo?.ContainsKey(radio.Id) == true;
                if (isDownload && isCached)
                {
                    continue;
                }
                
                sb.Append("<b>").Append(radio.Name).Append("</b> (<i>").Append(radio.Id).Append("</i>) ==> ")
                  .Append('/').Append(this.Localize(message, "play")).Append(this.Localize(message, "radio")).Append('_').AppendFormat("{0:D2}", radio.InternalId)
                  .Append(!isCached ? " *" : string.Empty)
                  .AppendLine();

                if (sb.Length > 4000)
                {
                    await this.ReplyFormated(messageContext, sb.ToString(), cancellationToken);
                    sb.Clear();
                }
            }

            if (sb.Length > 0)
            {
                await this.ReplyFormated(messageContext, sb.ToString(), cancellationToken);
            }

            if (isDownload)
            {
                using var stream = new MemoryStream();
                await JsonSerializer.SerializeAsync(stream, cachedRadioInfo, jsonSerializerOptions, cancellationToken);
                stream.Position = 0;
                await (messageContext.BotClient?.SendDocumentAsync(
                    chatId: messageContext.Message.Chat.Id,
                    InputFile.FromStream(stream, "radio-list.json"),
                    replyToMessageId: messageContext.Message.MessageId,
                    cancellationToken: cancellationToken) ?? Task.CompletedTask);
            }
            
            return;
        }

        await this.Reply(messageContext, "No radio stations configured", cancellationToken);
    }
}
