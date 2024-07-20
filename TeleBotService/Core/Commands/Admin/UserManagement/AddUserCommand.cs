using Microsoft.Extensions.Options;
using TeleBotService.Config;
using TeleBotService.Core.Model;
using TeleBotService.Extensions;

namespace TeleBotService.Core.Commands.Admin.UserManagement;

public class AddUserCommand : TelegramCommand
{
    private readonly UsersConfig users;

    public AddUserCommand(IOptions<UsersConfig> users)
    {
        this.users = users.Value;
    }

    public override bool IsAdmin => true;

    public override string CommandString => "/AddUser";

    protected override Task Execute(MessageContext messageContext, CancellationToken cancellationToken = default)
    {
        var userName = messageContext.Message.GetLastString("_");
        if (string.IsNullOrEmpty(userName))
        {
            return this.Reply(messageContext, "No user specified");
        }

        if (this.users.GetValueOrDefault(userName)?.Enabled == true)
        {
            return this.Reply(messageContext, $"'{userName}' is already configured.");
        }

        users.AddNewUser(userName);
        return this.Reply(messageContext, $"User '{userName}' added successfully.");
    }
}
