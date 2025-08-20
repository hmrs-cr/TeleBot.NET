using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TeleBotService.Core.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using File = Telegram.Bot.Types.File;

namespace TeleBotService.Core.Commands;

public class AudioFileMetadata : Audio
{
    internal const string MetadataFileName = "metadata.json";
    
    private string? hashValue;
    
    public AudioFileMetadata()
    {
    }

    public AudioFileMetadata(FileBase fileInfo)
    {
        this.FileId = fileInfo.FileId;
        this.FileUniqueId = fileInfo.FileUniqueId;
        this.FileSize = fileInfo.FileSize;
        this.Duration = (fileInfo as Voice)?.Duration ?? (fileInfo as Audio)?.Duration ?? 0;
        this.MimeType = (fileInfo as Voice)?.MimeType ?? (fileInfo as Audio)?.MimeType;
        this.Performer = (fileInfo as Audio)?.Performer;
        this.Title = (fileInfo as Audio)?.Title;
        this.FileName = (fileInfo as Audio)?.FileName;
    }

    public string? FilePath { get; private set; }

    public string LocalFullFilePath { get; set; }

    public string HashValue
    {
        get
        {
            if (hashValue != null)
            {
                return hashValue;
            }
            
            var crc32 = new System.IO.Hashing.Crc32();
            crc32.Append(BitConverter.GetBytes(FileSize.GetValueOrDefault()));
            crc32.Append(BitConverter.GetBytes(Duration));
            crc32.Append(Encoding.UTF8.GetBytes(MimeType ?? string.Empty));
            crc32.Append(Encoding.UTF8.GetBytes(Performer ?? string.Empty));
            crc32.Append(Encoding.UTF8.GetBytes(Title ?? string.Empty));

            return hashValue = Convert.ToHexString(BitConverter.GetBytes(crc32.GetCurrentHashAsUInt32()));
        }
    }

    internal void SetFileInfo(File file)
    {
        this.FilePath = file.FilePath;
    }

    internal async ValueTask<AudioFileMetadata?> AlreadyExistsInFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            return null;
        }
        
        var hashFileName = Directory.EnumerateFiles(folderPath, this.HashValue, 
            SearchOption.AllDirectories).FirstOrDefault();

        if (hashFileName is null)
        {
            return null;
        }

        var metadataFolderName = Path.GetDirectoryName(hashFileName);
        if (metadataFolderName is null)
        {
            return null;
        }
        
        return await Load(Path.Join(metadataFolderName, MetadataFileName));
    }
    
    internal static async Task<AudioFileMetadata?> Load(string filename)
    {
        await using var metadataReadStream = new FileStream(filename, FileMode.Open);
        var metadata = await JsonSerializer.DeserializeAsync<AudioFileMetadata>(metadataReadStream);
        return metadata;
    }
}

public interface IVoiceMessageService
{  
    Task<AudioFileMetadata?> GetMessageById(string? fileUniqueId);
    IAsyncEnumerable<AudioFileMetadata?> EnumerateAudioMessages();
}

public class AudioMessagePlayerCommand : TelegramCommand, IVoiceMessageService
{
    private readonly JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        IgnoreReadOnlyProperties = true
    };
    
    private readonly string voiceMessageFolder;

    private AudioFileMetadata? mostRecentAudioFileMetadata;

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
            var audioFileMetadata = new AudioFileMetadata(fileBaseInfo);
            if (await audioFileMetadata.AlreadyExistsInFolder(this.voiceMessageFolder) is { } existing)
            {
                PlayLatestAudioMessage(existing);
                return;
            }
            
            var fileInfo =
                await messageContext.BotClient.GetFileAsync(fileBaseInfo.FileId, cancellationToken: cancellationToken);
            
            if (fileInfo.FilePath is not null)
            {
                audioFileMetadata.SetFileInfo(fileInfo);
                
                var directory = Path.Join(this.voiceMessageFolder, Path.GetDirectoryName(fileInfo.FilePath.AsSpan()), fileInfo.FileUniqueId);
                var localFullFilePath = Path.Join(directory, Path.GetFileName(fileInfo.FilePath.AsSpan()));
                audioFileMetadata.LocalFullFilePath = localFullFilePath;

                Directory.CreateDirectory(directory);
                
                await using var audioWriteStream = new FileStream(audioFileMetadata.LocalFullFilePath, FileMode.Create);
                await using var metadataWriteStream = new FileStream(Path.Join(directory, AudioFileMetadata.MetadataFileName), FileMode.Create);
                
                var writeMetadataTask = JsonSerializer.SerializeAsync(metadataWriteStream, audioFileMetadata, jsonOptions, cancellationToken: cancellationToken);
                var writeAudioTask =  messageContext.BotClient.DownloadFileAsync(fileInfo.FilePath, audioWriteStream, cancellationToken);
                
                var writeHashTask = System.IO.File.WriteAllTextAsync(Path.Join(directory, audioFileMetadata.HashValue), audioFileMetadata.HashValue, cancellationToken);
                
                await Task.WhenAll(writeAudioTask, writeMetadataTask, writeHashTask);

                PlayLatestAudioMessage(audioFileMetadata);

                return;
            }
        }

        await Reply(messageContext, "No valid voice message found", cancellationToken: cancellationToken);
        
        return;

        void PlayLatestAudioMessage(AudioFileMetadata audioFileMetadata)
        {
            mostRecentAudioFileMetadata = audioFileMetadata;
            _ = messageContext.ExecuteCommand("play music with-resume MostRecentAudioMessage", ct: cancellationToken);
        }
    }

    async Task<AudioFileMetadata?> IVoiceMessageService.GetMessageById(string? fileUniqueId)
    {
        if (fileUniqueId is null)
        {
            return  mostRecentAudioFileMetadata;
        }
        
        var folder = Directory.EnumerateDirectories(this.voiceMessageFolder, fileUniqueId, SearchOption.AllDirectories).FirstOrDefault();
        if (folder is null)
        {
            return null;
        }
        
        return await AudioFileMetadata.Load(Path.Join(folder, AudioFileMetadata.MetadataFileName));
    }

    async IAsyncEnumerable<AudioFileMetadata?>  IVoiceMessageService.EnumerateAudioMessages()
    {
        foreach (var filename in Directory.EnumerateFiles(this.voiceMessageFolder, AudioFileMetadata.MetadataFileName,
                     SearchOption.AllDirectories))
        {
            yield return await AudioFileMetadata.Load(filename);
        }
    }
}