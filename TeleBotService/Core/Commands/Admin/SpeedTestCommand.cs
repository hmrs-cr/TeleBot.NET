using Microsoft.Extensions.Options;
using TeleBotService.Config;
using TeleBotService.Core.Model;
using TeleBotService.Extensions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Commands.Admin;

public class SpeedTestCommand : TelegramCommand
{
    private const string SpeedTestLicenseAccepted = "SpeedTestLicenseAccepted";

    private const string AcceptanceMessage = "/I_Accept_the_speedtestNET_license";

    private readonly string speedTestExecPath;

    public SpeedTestCommand(IOptions<ExternalToolsConfig> config, ILogger<SpeedTestCommand> logger)
    {
        this.speedTestExecPath = config.Value.SpeedTest ?? "/usr/bin/speedtest";
        this.Logger = logger;
    }

    public override string CommandString => "speedtest";

    public override bool IsAdmin => true;

    protected override async Task<bool> StartExecuting(MessageContext messageContext, CancellationToken token)
    {
        var canExecute = await base.StartExecuting(messageContext, token);
        if (!canExecute)
        {
            return false;
        }

        var everthingIsSetup = System.IO.File.Exists(this.speedTestExecPath);
        if (!everthingIsSetup)
        {
            await this.Reply(messageContext, "Can not run speedtest. Missing speedtest tools.");
        }

        return everthingIsSetup;
    }

    protected override async Task Execute(MessageContext messageContext, CancellationToken cancellationToken = default)
    {
        var message = messageContext.Message;
        var isAcceptanceMessage = this.ContainsText(message, AcceptanceMessage);
        var isLicenseAlreadyAccepted = messageContext.User.GetBoolSetting(SpeedTestLicenseAccepted);
        if (!isAcceptanceMessage && !isLicenseAlreadyAccepted)
        {
            await this.Reply(messageContext, $"You need to accept the terms of use first:\n\n\thttps://www.speedtest.net/about/eula\n\thttps://www.speedtest.net/about/terms\n\thttps://www.speedtest.net/about/privacy\n\nExecute the command {AcceptanceMessage} to continue", cancellationToken);
            return;
        }

        try
        {
executeSpeedTest:
            var arguments = "--format json";
            if (isAcceptanceMessage)
            {
                arguments = "--accept-license --accept-gdpr --format json";
                messageContext.User.SetSetting(SpeedTestLicenseAccepted, true);
            }

            var result = await ProcessExtensions.ExecuteJsonProcessCommand<SimpleSpeedTestResult>(this.speedTestExecPath, arguments, cancellationToken);
            if (result?.Result?.Url is { })
            {
                await SendResultMessage(messageContext, result, cancellationToken);
            }
            else
            {
                if (!isAcceptanceMessage && isLicenseAlreadyAccepted)
                {
                    isAcceptanceMessage = true;
                    goto executeSpeedTest;
                }

                await this.Reply(messageContext, "An error happened.");
            }
        }
        catch (Exception e)
        {
            var error = "Error executing speedtest";
            await this.Reply(messageContext, error, cancellationToken);
            this.LogSimpleException(e, error);
        }
    }

    private async Task SendResultMessage(MessageContext messageContext, SimpleSpeedTestResult result, CancellationToken cancellationToken = default)
    {
        var message = messageContext.Message;
        try
        {
            var image = InputFile.FromString($"{result.Result.Url}.png");
            await messageContext.BotClient.SendPhotoAsync(message.Chat.Id, image, replyToMessageId: message.MessageId,
                caption: result.Result.Url, cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            var error = "Error sending speedtest message";
            this.LogSimpleException(e, error);
            
            await messageContext.BotClient.SendTextMessageAsync(message.Chat.Id, result.Result.Url, replyToMessageId: message.MessageId, cancellationToken: cancellationToken);
        }
    }

    private class SimpleSpeedTestResult
    {
        public ResultData? Result { get; init; }

        public class ResultData
        {
            public string? Url { get; init; }
        }
    }
}
