using Microsoft.Extensions.Options;
using TeleBotService.Config;
using TeleBotService.Core.Model;
using TeleBotService.Extensions;

namespace TeleBotService.Core.Commands.Admin.UserManagement;

public class RemoveUserCommand : TelegramCommand
{
   private readonly UsersConfig users;

    public RemoveUserCommand(IOptions<UsersConfig> users)
    {
        this.users = users.Value;
    }

    public override bool IsAdmin => true;

    public override string CommandString => "/RemoveUser";

    protected override Task Execute(MessageContext messageContext, CancellationToken cancellationToken = default)
    {
        var userName = messageContext.Message.GetLastString("_");
        if (string.IsNullOrEmpty(userName))
        {
            return this.Reply(messageContext, "No user specified");
        }

        if (messageContext.User.UserName == userName)
        {
            return this.Reply(messageContext, "You can not remove yourself.");
        }

        if (this.users.ContainsKey(userName))
        {
            this.users.RemoveUser(userName);
            return this.Reply(messageContext, $"'{userName}' removed.");
        }

        return this.Reply(messageContext, $"'{userName}' does not exists.");
    }
}
