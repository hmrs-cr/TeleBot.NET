using Linkplay.HttpApi.Model;
using Microsoft.Extensions.Logging;

namespace Linkplay.HttpApi;

public interface ILinkplayHttpApiClient
{
    ILogger? Logger { get ; set; }

    TimeSpan RequestTimeout { get; set; }

    Task<string> Reboot();
    Task<PlayerStatus?> GetPlayerStatus();
    Task<bool> IsConnected();
    Task<bool> ControlPlayer(PlayerControl command);
    Task<bool> PlayerSeek(uint newPos);
    Task<bool> PlayLocalList(uint v);
    Task<bool> PlayPreset(uint index);
    Task<bool> PlayUrl(Uri url);
    Task<bool> PlayPlaylist(Uri url);
    Task<bool> PlayerVolumeUp();
    Task<bool> PlayerVolumeDown();
    Task<bool> PlayerSetVolume(uint volume);
}
