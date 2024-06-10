using TeleBotService.Core.Model;
using TeleBotService.Extensions;

namespace TeleBotService.Core.Commands.Admin;

public class KillServiceCommand : TelegramCommand
{
    public KillServiceCommand(ILogger<KillServiceCommand> logger)
    {
        this.Logger = logger;
    }

    public override bool IsAdmin => true;
    public override string CommandString => "/Kill_With_Exit_Code";

    protected override Task Execute(MessageContext messageContext, CancellationToken cancellationToken = default)
    {
        var exitCode = messageContext.Message.ParseLastInt(' ') ??  messageContext.Message.ParseLastInt('_');
        if (exitCode.HasValue)
        {
            this.LogWarning("Killing service with exit code {exitCode}", exitCode);
            _ = TelebotServiceApp.Stop(exitCode.Value);
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }
}
