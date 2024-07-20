
using System.Text;
using Microsoft.Extensions.Options;
using TeleBotService.Config;
using TeleBotService.Core.Model;

namespace TeleBotService.Core.Commands.Admin.UserManagement;

public class ListUsersCommand : TelegramCommand
{
    private readonly UsersConfig users;

    public ListUsersCommand(IOptions<UsersConfig> users)
    {
        this.users = users.Value;
    }

    public override bool IsAdmin => true;

    public override string CommandString => "/ListUsers";

    protected override Task Execute(MessageContext messageContext, CancellationToken cancellationToken = default) =>
        this.Reply(messageContext, string.Join('\n', this.users.Where(kv => kv.Value.Enabled).Select(kv => kv.Key)));
}
