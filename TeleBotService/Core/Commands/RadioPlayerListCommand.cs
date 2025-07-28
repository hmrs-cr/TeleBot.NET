using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TeleBotService.Config;
using TeleBotService.Core.Model;
using TeleBotService.Data;
using TeleBotService.Extensions;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Commands;

public class RadioPlayerListCommand : TelegramCommand
{
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
            foreach (var radio in this.radioConfig.Stations)
            {
                var cached = await this.internetRadioRepository.GetStreamData(radio.Id);
                sb.Append("<b>").Append(radio.Name).Append("</b> (<i>").Append(radio.Id).Append("</i>) ==> ")
                  .Append('/').Append(this.Localize(message, "play")).Append(this.Localize(message, "radio")).Append('_').AppendFormat("{0:D2}", radio.InternalId)
                  .Append(cached is null ? " *" : string.Empty)
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
            
            return;
        }

        await this.Reply(messageContext, "No radio stations configured", cancellationToken);
    }
}
