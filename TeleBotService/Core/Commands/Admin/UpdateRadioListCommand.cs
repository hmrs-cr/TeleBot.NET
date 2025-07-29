using System.Text.Json;
using TeleBotService.Core.Model;
using TeleBotService.Data;
using TeleBotService.Extensions;
using Telegram.Bot;

namespace TeleBotService.Core.Commands.Admin;

public class UpdateRadioListCommand : TelegramCommand
{
    private readonly IInternetRadioRepository internetRadioRepository;

    public UpdateRadioListCommand(ILogger<KillServiceCommand> logger, IInternetRadioRepository internetRadioRepository)
    {
        this.internetRadioRepository = internetRadioRepository;
        this.Logger = logger;
    }

    public override bool IsAdmin => true;
    public override string CommandString => "radio-list.json";

    protected override async Task Execute(MessageContext messageContext, CancellationToken cancellationToken = default)
    {
        var fileId = messageContext.Message.GetLastString();
        if (fileId != null)
        {
            var fileInfo = await messageContext.BotClient.GetFileAsync(fileId, cancellationToken: cancellationToken);
            using var stream = new MemoryStream();
            if (fileInfo.FilePath != null)
            {
                await messageContext.BotClient.DownloadFileAsync(fileInfo.FilePath, stream, cancellationToken);
                stream.Position = 0;
                var radioInfoList = await JsonSerializer.DeserializeAsync<Dictionary<string, RadioDiscoverResponse.ResultData.Stream>>(stream, cancellationToken: cancellationToken);
                if (radioInfoList != null)
                {
                    // TODO: Implement
                }
            }
        }
    }
}
