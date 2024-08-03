using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Linkplay.HttpApi.Json;
using Linkplay.HttpApi.Model;
using Microsoft.Extensions.Logging;

namespace Linkplay.HttpApi;

public class LinkplayHttpApiClient : ILinkplayHttpApiClient
{
    private const string CommandPath = "/httpapi.asp";
    private readonly string host;
    private readonly HttpClient httpClient;

    private Uri? currentNetMediaUrl;
    private string? currentNetMediaName;

    public TimeSpan RequestTimeout { get => this.httpClient.Timeout; set => this.httpClient.Timeout = value; }

    public ILogger? Logger { get ; set; }

    private readonly JsonSerializerOptions jsonOptions = new()
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

    };

    public LinkplayHttpApiClient(string host, HttpClient? httpClient = null)
    {
        this.host = host ?? throw new ArgumentNullException(nameof(host));
        this.httpClient = httpClient ?? new HttpClient();
        this.jsonOptions.Converters.Add(new JsonStringEnumConverter());
        this.jsonOptions.Converters.Add(new HexedStringJsonConverter());
        this.jsonOptions.Converters.Add(new BoolStringJsonConverter());
        this.jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    }

    public async Task<string> Reboot() => await this.ExecuteCommand("reboot");

    public async Task<PlayerStatus?> GetPlayerStatus() =>  this.SetNetMediaValues(await this.ExecuteCommand<PlayerStatus>("getPlayerStatus"));

    public async Task<NetworkStatus?> GetNetworkStatus() => await this.ExecuteCommand<NetworkStatus>("getStaticIP");

    public async Task<bool> IsConnected()
    {
        var netStatus = await this.GetNetworkStatus();
        var isConnected = netStatus != null && (netStatus.Eth != NetworkState.NotConnected || netStatus.Wifi != NetworkState.NotConnected);
        return isConnected;
    }

    public async Task<DeviceStatus?> GetDeviceStatus() => await this.ExecuteCommand<DeviceStatus>("getStatusEx");

    public async Task<int> GetTrackCount() => await this.ExecuteCommand<int>("GetTrackNumber");

    public async Task<LocalPlayList?> GetLocalPlayList() => await this.ExecuteCommand<LocalPlayList>("getLocalPlayList");

    public async Task<LocalInfolist?> GetLocalFileInfo(uint indexStart, uint range) => await this.ExecuteCommand<LocalInfolist>($"getFileInfo:{indexStart}:{range}");

    public async Task<bool> SetPlayerMode(PlayerMode playerMode) => IsOkResponse(await this.ExecuteCommand($"setPlayerCmd:switchmode:{playerMode}"));

    public async Task<bool> PlayUrl(Uri uri) => IsOkResponse(await this.ExecuteCommand($"setPlayerCmd:play:{this.SetCurrentNetMediaUrl(uri)}"));

    public async Task<bool> PlayPlaylist(Uri uri) => IsOkResponse(await this.ExecuteCommand($"setPlayerCmd:m3u:play:{this.SetCurrentNetMediaUrl(uri)}"));

    public async Task<bool> PlayUrl(string? name, Uri uri) => IsOkResponse(await this.ExecuteCommand($"setPlayerCmd:play:{this.SetCurrentNetMediaUrl(uri, name)}"));

    public async Task<bool> PlayPlaylist(string? name, Uri uri) => IsOkResponse(await this.ExecuteCommand($"setPlayerCmd:m3u:play:{this.SetCurrentNetMediaUrl(uri, name)}"));


    public async Task<bool> PlayLocalList(uint index) => this.ClearCurrentNetMediaUrl(IsOkResponse(await this.ExecuteCommand($"setPlayerCmd:playLocalList:{index}")));

    public async Task<bool> PlayIndex(uint index) => this.ClearCurrentNetMediaUrl(IsOkResponse(await this.ExecuteCommand($"setPlayerCmd:playindex:{index}")));

    public async Task<bool> SetPlayerLoopMode(ShuffleMode mode) => IsOkResponse(await this.ExecuteCommand($"setPlayerCmd:loopmode:{(int)mode}"));

    public async Task<bool> ControlPlayer(PlayerControl command) => IsOkResponse(await this.ExecuteCommand($"setPlayerCmd:{command}"));

    public Task<bool> Pause() => this.ControlPlayer(PlayerControl.Pause);
    public Task<bool> Stop() => this.ControlPlayer(PlayerControl.Stop);
    public Task<bool> Resume() => this.ControlPlayer(PlayerControl.Resume);
    public Task<bool> TogglePause() => this.ControlPlayer(PlayerControl.OnePause);
    public Task<bool> Prev() => this.ControlPlayer(PlayerControl.Prev);
    public Task<bool> Next() => this.ControlPlayer(PlayerControl.Next);

    public async Task<bool> PlayerSeek(uint positionInSeconds) => IsOkResponse(await this.ExecuteCommand($"setPlayerCmd:seek:{positionInSeconds}"));

    public async Task<bool> PlayPreset(uint preset) => this.ClearCurrentNetMediaUrl(IsOkResponse(await this.ExecuteCommand($"MCUKeyShortClick:{preset}")));

    public async Task<bool> PlayerSetVolume(uint vol) => IsOkResponse(await this.ExecuteCommand($"setPlayerCmd:vol:{vol}"));

    public async Task<bool> PlayerVolumeDown() => IsOkResponse(await this.ExecuteCommand($"setPlayerCmd:vol--"));

    public async Task<bool> PlayerVolumeUp() => IsOkResponse(await this.ExecuteCommand($"setPlayerCmd:vol%2b%2b"));

    public async Task<bool> PlayerMuteVolume(bool mute) => IsOkResponse(await this.ExecuteCommand($"setPlayerCmd:mute:{Convert.ToInt16(mute)}"));

    public async Task<T?> ExecuteCommand<T>(string command)
    {
        try
        {
            return await this.httpClient.GetFromJsonAsync<T>(this.GetCommnadUrl(command), this.jsonOptions);
        }
        catch (Exception e)
        {
            this.Logger?.LogWarning("Error executing command {command}: [{ExceptionType}] {ExceptionMessage}", command, e.GetType().FullName, e.Message);
            return default;
        }
    }

    private PlayerStatus? SetNetMediaValues(PlayerStatus? playerStatus)
    {
        if (playerStatus != null)
        {
            playerStatus.NetMediaName = this.currentNetMediaName;
            playerStatus.Url = this.currentNetMediaUrl;
        }

        return playerStatus;
    }

    private Uri SetCurrentNetMediaUrl(Uri url, string? name = null)
    {
        this.currentNetMediaName = name;
        return this.currentNetMediaUrl = url;
    }

    private T ClearCurrentNetMediaUrl<T>(T result)
    {
        this.currentNetMediaName = null;
        this.currentNetMediaUrl = null;
        return result;
    }

    public async Task<string> ExecuteCommand(string command) => this.Log(await this.httpClient.GetStringAsync(this.GetCommnadUrl(command)));

    private static bool IsOkResponse(string response) => response == "OK";

    private Uri GetCommnadUrl(string command) => this.Log(new UriBuilder(schemeName: "http://", hostName: this.host)
    {
        Path = CommandPath,
        Query = $"command={command}"
    }.Uri);

    private T Log<T>(T value)
    {
        if (value is { } && this.Logger != null)
        {
            this.Logger.LogDebug(value.ToString());
        }

        return value;
    }
}
