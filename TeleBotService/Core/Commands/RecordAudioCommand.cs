using System.Diagnostics;
using Microsoft.Extensions.Options;
using TeleBotService.Config;
using TeleBotService.Core.Model;
using TeleBotService.Extensions;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Commands;

public class RecordAudioCommand : TelegramCommand
{
    private readonly string arecordExecPath;
    private readonly string opusencExecPath;
    public readonly string mkfifoExecPath;


    public RecordAudioCommand(IOptions<ExternalToolsConfig> config)
    {
        this.arecordExecPath = config.Value.ARecord ?? "/usr/bin/arecord";
        this.opusencExecPath = config.Value.OpusEnc ?? "/usr/bin/opusenc";
        this.mkfifoExecPath = config.Value.MKFifo ?? "/usr/bin/mkfifo";
    }

    public override string CommandString => "arecord";

    public override string Description => "Record audio from the server mic";

    public override string Usage => $"{base.Usage}\n{this.CommandString}_voice";

    protected override async Task<bool> StartExecuting(MessageContext messageContext, CancellationToken token)
    {
        var canExecute = await base.StartExecuting(messageContext, token);
        if (!canExecute)
        {
            return false;
        }

        var everthingIsSetup = System.IO.File.Exists(this.arecordExecPath) &&
                               System.IO.File.Exists(this.opusencExecPath) &&
                               System.IO.File.Exists(this.mkfifoExecPath);

        if (!everthingIsSetup)
        {
            await this.Reply(messageContext.Message, "Can not record audio. Missing audio tools.");
        }

        return everthingIsSetup;
    }

    protected override async Task Execute(MessageContext messageContext, CancellationToken cancellationToken = default)
    {
        var parameters = messageContext.Message.ParseIntArray(' ');

        var count = 1;
        var duration = 30;
        var isVoice = this.ContainsText(messageContext.Message, "voice");

        int maxCount = 100;
        int maxDuration = isVoice ? 900 : 150;

        if (parameters.Count >= 1)
        {
            duration = parameters[0];
            if (parameters.Count > 1)
            {
                count = parameters[1];
            }
        }

        if (count > maxCount)
        {
            count = maxCount;
        }

        if (duration > maxDuration)
        {
            duration = maxDuration;
        }

        var replyTasks = new List<Task>(count);
        for (var c = 1; c <= count; c++)
        {
            var fileName = Path.GetTempFileName();
            var intermediateFileName = $"{fileName}.fifo";


            Process? oggencProcess = null;
            var arecordProcess = new Process
            {
                StartInfo = new()
                {
                    FileName = this.arecordExecPath,
                    UseShellExecute = false,
                    Arguments = $"-f dat -t wav -d {duration} {(isVoice ? intermediateFileName : fileName)}",
                }
            };

            if (isVoice)
            {
                await ProcessExtensions.ExecuteProcessCommand(this.mkfifoExecPath, intermediateFileName);
                oggencProcess = new Process
                {
                    StartInfo = new()
                    {
                        FileName = this.opusencExecPath,
                        UseShellExecute = false,
                        Arguments = $"{intermediateFileName} {fileName}",
                    }
                };
            }

            var recordDateTime = DateTime.Now;
            var recordProcessTask = arecordProcess.StartAndAwaitUntilExit(cancellationToken);
            var oggencProcessTask = oggencProcess != null ? oggencProcess.StartAndAwaitUntilExit(cancellationToken: cancellationToken) : Task.CompletedTask;

            await Task.WhenAll(recordProcessTask, oggencProcessTask);
            System.IO.File.Delete(intermediateFileName);

            replyTasks.Add(this.AudioReply(messageContext.Message, fileName, title: isVoice ? "voice-memo" : $"{recordDateTime:s}", duration: duration, deleteFile: true));
        }

        await Task.WhenAll(replyTasks);
    }
}
