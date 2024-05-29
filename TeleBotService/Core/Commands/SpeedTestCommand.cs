﻿using TeleBotService.Core.Model;
using TeleBotService.Extensions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Commands;

public class SpeedTestCommand : TelegramCommand
{
    private const string AcceptanceMessage = "/I_Accept_the_speedtestNET_licence";

    private readonly string speedTestExecPath;

    public SpeedTestCommand(IConfiguration configuration)
    {
        this.speedTestExecPath = configuration.GetValue<string>("SpeedTestExecPath") ?? "/usr/bin/speedtest";
    }

    public override bool CanExecuteCommand(Message message) => message.Text?.Contains("speedtest", StringComparison.InvariantCultureIgnoreCase) == true;

    protected override async Task Execute(Message message, CancellationToken cancellationToken = default)
    {
        var isAcceptanceMessage = this.ContainsText(message, AcceptanceMessage);
       if (!isAcceptanceMessage && !message.GetCommandContextData<ContextData>(this).SpeedTestLicenceAccepted)
       {
            await this.Reply(message, $"You need to accept the terms of use first:\n\n\thttps://www.speedtest.net/about/eula\n\thttps://www.speedtest.net/about/terms\n\thttps://www.speedtest.net/about/privacy\n\nExecute the command {AcceptanceMessage} to continue", cancellationToken);
            return;
       }

        try
        {
            var arguments = "--format json";
            if (isAcceptanceMessage)
            {
                arguments = "--accept-license --accept-gdpr --format json";
                message.GetCommandContextData<ContextData>(this).SpeedTestLicenceAccepted = true;
            }

            var result = await ProcessExtensions.ExecuteJsonProcessCommand<SpeedtestResult>(this.speedTestExecPath, arguments, cancellationToken);
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

    private record ContextData
    {
        public bool SpeedTestLicenceAccepted { get; set; }
    }
}
