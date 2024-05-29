using TeleBotService.Core;
using TeleBotService.Model;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TeleBotService;

public class SpeedTestCommand : TelegramCommand
{
    private readonly string speedTestExecPath;

    public SpeedTestCommand(IConfiguration configuration)
    {
        this.speedTestExecPath = configuration.GetValue<string>("SpeedTestExecPath") ?? "/usr/bin/speedtest";
    }

    public override bool CanExecuteCommand(Message message) => message.Text?.StartsWith("/speedtest", StringComparison.InvariantCultureIgnoreCase) == true;

    protected override async Task Execute(Message message, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await ProcessExtensions.ExecuteJsonProcessCommand<SpeedtestResult>(this.speedTestExecPath, "--accept-license --accept-gdpr --format json", cancellationToken);
            if (result?.Result?.Url is { })
            {
                var image = InputFile.FromString($"{result.Result.Url}.png");
                await this.BotClient.SendPhotoAsync(message.Chat.Id, image, replyToMessageId: message.MessageId, caption: result.Result.Url, cancellationToken: cancellationToken);
            }
            else
            {
                await this.Reply(message, "An error happened.");
            }
        }
        catch (Exception e)
        {
            var error = $"Error executing speedtest: {e.Message}";
            await this.Reply(message, error, cancellationToken);
            Console.WriteLine(error);
        }
    }
}
