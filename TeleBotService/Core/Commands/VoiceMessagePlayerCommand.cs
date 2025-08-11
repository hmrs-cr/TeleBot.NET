using System.Collections.Concurrent;
using TeleBotService.Core.Model;
using TeleBotService.Data;
using Telegram.Bot;
using Telegram.Bot.Types;
using File = Telegram.Bot.Types.File;

namespace TeleBotService.Core.Commands;

public class VoiceFileInfo : Voice
{
    public VoiceFileInfo(Voice voice, File file)
    {
        this.FileId = voice.FileId;
        this.FileUniqueId = voice.FileUniqueId;
        this.FileSize = voice.FileSize;
        this.Duration = voice.Duration;
        this.MimeType = voice.MimeType;
        this.FilePath = file.FilePath;
    }

    public string? FilePath { get; }
    
    public string LocalFullFilePath { get; init; }
    public string LocalFileName { get; init; }
}

public interface IVoiceMessageService
{
    IEnumerable<VoiceFileInfo> GetPendingMessages();
    VoiceFileInfo? GetMessageById(string fileUniqueId);
    Task<Stream> DownloadMessage(VoiceFileInfo voice);
}

public class VoiceMessagePlayerCommand : TelegramCommand, IVoiceMessageService
{
    private readonly IInternetRadioRepository internetRadioRepository;
    private readonly string voiceMessageFolder;
    
    private readonly ConcurrentDictionary<string, VoiceFileInfo> pendingVoiceMessages = new();

    public VoiceMessagePlayerCommand(ILogger<VoiceMessagePlayerCommand> logger, IInternetRadioRepository internetRadioRepository)
    {
        this.internetRadioRepository = internetRadioRepository;
        this.Logger = logger;
        this.voiceMessageFolder = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "voice-messages");
    }

    public override bool IsAdmin => true;
    public override string CommandString => "Voice-Message-Player-Command";

    protected override async Task Execute(MessageContext messageContext, CancellationToken cancellationToken = default)
    {
        if (messageContext.Message.Voice is { } voice)
        {
            var fileInfo =
                await messageContext.BotClient.GetFileAsync(voice.FileId, cancellationToken: cancellationToken);
            if (fileInfo.FilePath is not null)
            {
                var localFileName = $"{DateTime.Now:yyyyMMddHHmmss}-{Path.GetFileName(fileInfo.FilePath)}";
                var voiceFileInfo = new VoiceFileInfo(voice, fileInfo)
                {
                    LocalFileName = localFileName,
                    LocalFullFilePath = Path.Join(this.voiceMessageFolder, localFileName)
                };

                pendingVoiceMessages.TryAdd(voice.FileUniqueId, voiceFileInfo);
                await using var writeStream = new FileStream(voiceFileInfo.LocalFullFilePath, FileMode.Create);
                await messageContext.BotClient.DownloadFileAsync(fileInfo.FilePath, writeStream, cancellationToken);
                return;
            }
        }

        await Reply(messageContext, "No valid voice message found", cancellationToken: cancellationToken);
    }

    IEnumerable<VoiceFileInfo> IVoiceMessageService.GetPendingMessages() => pendingVoiceMessages.Values;

    VoiceFileInfo? IVoiceMessageService.GetMessageById(string fileUniqueId) => pendingVoiceMessages.GetValueOrDefault(fileUniqueId);

    async Task<Stream> IVoiceMessageService.DownloadMessage(VoiceFileInfo voice)
    {
        Directory.CreateDirectory(this.voiceMessageFolder);
        throw new NotImplementedException();
    }
}
