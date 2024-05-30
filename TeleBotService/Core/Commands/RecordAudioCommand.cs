using System.Diagnostics;
using Microsoft.Extensions.Options;
using TeleBotService.Config;
using TeleBotService.Extensions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Commands;

public class RecordAudioCommand : TelegramCommand
{
    private readonly string arecordExecPath;

    public RecordAudioCommand(IOptions<ExternalToolsConfig> config)
    {
        this.arecordExecPath = config.Value.ARecord ?? "/usr/bin/arecord";
    }

    public override bool CanExecuteCommand(Message message) => this.ContainsText(message, "/arecord");

    protected override async Task Execute(Message message, CancellationToken cancellationToken = default)
    {
        var parameters = message.ParseIntArray(' ');

        var count = 1;
        var duration = 30;

        if (parameters.Count >= 1)
        {
            duration = parameters[0];
            if (parameters.Count > 1)
            {
                count = parameters[1];
            }
        }

        if (count > 100)
        {
            count = 100;
        }

        if (duration > 150)
        {
            duration = 150;
        }

        var replyTasks = new List<Task>(count);
        for (var c = 1; c <= count; c++)
        {
            var fileName = Path.GetTempFileName();
            var process = new Process
            {
                StartInfo = new()
                {
                    FileName = this.arecordExecPath,
                    UseShellExecute = false,
                    Arguments = $"-f dat -t wav -d {duration} {fileName}",
                }
            };

            var recordDateTime = DateTime.Now;
            await process.StartAndAwaitUntilExit(cancellationToken);

            replyTasks.Add(this.AudioReply(message, fileName, $"{recordDateTime:s}", duration, true));
        }

        await Task.WhenAll(replyTasks);
    }
}
