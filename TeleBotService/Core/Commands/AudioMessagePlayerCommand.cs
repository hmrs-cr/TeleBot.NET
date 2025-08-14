using System.Collections.Concurrent;
using TeleBotService.Core.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using File = Telegram.Bot.Types.File;

namespace TeleBotService.Core.Commands;

public class AudioFileInfo : Audio
{
    public AudioFileInfo(FileBase fileInfo, File file)
    {
        this.FileId = fileInfo.FileId;
        this.FileUniqueId = fileInfo.FileUniqueId;
        this.FileSize = fileInfo.FileSize;
        this.Duration = (fileInfo as Voice)?.Duration ?? (fileInfo as Audio)?.Duration ?? 0;
        this.MimeType = (fileInfo as Voice)?.MimeType ?? (fileInfo as Audio)?.MimeType;
        this.FilePath = file.FilePath;
        this.Performer = (fileInfo as Audio)?.Performer;
        this.Title = (fileInfo as Audio)?.Title;
        this.FileName = (fileInfo as Audio)?.FileName;
    }

    public string? FilePath { get; }
    
    public required string LocalFullFilePath { get; init; }
}

public interface IVoiceMessageService
{
    IEnumerable<AudioFileInfo> GetPendingMessages();
    AudioFileInfo? GetMessageById(string fileUniqueId);
}

public class AudioMessagePlayerCommand : TelegramCommand, IVoiceMessageService
{
    private readonly string voiceMessageFolder;
    
    private readonly ConcurrentDictionary<string, AudioFileInfo> pendingVoiceMessages = new();

    public AudioMessagePlayerCommand(ILogger<AudioMessagePlayerCommand> logger)
    {
        this.Logger = logger;
        this.voiceMessageFolder = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "audio-messages");
    }

    public override bool IsAdmin => true;
    public override string CommandString => "Voice-Message-Player-Command";

    protected override async Task Execute(MessageContext messageContext, CancellationToken cancellationToken = default)
    {
        var fileBaseInfo = messageContext.Message.Voice as FileBase ?? messageContext.Message.Audio;
        if (fileBaseInfo is not null)
        {
            var fileInfo =
                await messageContext.BotClient.GetFileAsync(fileBaseInfo.FileId, cancellationToken: cancellationToken);
            if (fileInfo.FilePath is not null)
            {
                var directory = Path.Join(this.voiceMessageFolder, Path.GetDirectoryName(fileInfo.FilePath.AsSpan()));
                var localFullFilePath = Path.Join(directory, Path.GetFileName(fileInfo.FilePath.AsSpan()));
                
                Directory.CreateDirectory(directory);
                
                var voiceFileInfo = new AudioFileInfo(fileBaseInfo, fileInfo)
                {
                    LocalFullFilePath = localFullFilePath
                };

                pendingVoiceMessages.TryAdd(fileBaseInfo.FileUniqueId, voiceFileInfo);
                
                await using var writeStream = new FileStream(voiceFileInfo.LocalFullFilePath, FileMode.Create);
                await messageContext.BotClient.DownloadFileAsync(fileInfo.FilePath, writeStream, cancellationToken);
                return;
            }
        }

        await Reply(messageContext, "No valid voice message found", cancellationToken: cancellationToken);
    }

    IEnumerable<AudioFileInfo> IVoiceMessageService.GetPendingMessages() => pendingVoiceMessages.Values;

    AudioFileInfo? IVoiceMessageService.GetMessageById(string fileUniqueId) =>
        pendingVoiceMessages.GetValueOrDefault(fileUniqueId);
}
