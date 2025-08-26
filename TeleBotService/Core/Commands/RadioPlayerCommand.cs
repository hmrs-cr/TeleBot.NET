using HtmlAgilityPack;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TeleBotService.Config;
using TeleBotService.Core.Model;
using TeleBotService.Data;
using TeleBotService.Extensions;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Commands;

public class RadioPlayerCommand : MusicPlayerCommandBase
{
    private readonly InternetRadioConfig radioConfig;
    private readonly IMemoryCache memoryCache;
    private readonly IInternetRadioRepository internetRadioRepository;
    private HttpClient? client;

    public RadioPlayerCommand(
        IOptions<MusicPlayersConfig> config,
        IOptions<TapoConfig> tapoConfig,
        IOptions<InternetRadioConfig> radioConfig,
        ILogger<RadioPlayerCommand> logger,
        IMemoryCache memoryCache,
        IInternetRadioRepository internetRadioRepository) : base(config, tapoConfig, logger)
    {
        this.radioConfig = radioConfig.Value;
        this.memoryCache = memoryCache;
        this.internetRadioRepository = internetRadioRepository;
        _ = AutoRebuildRadioCache();
    }

    public override string Description => "Internet Radio";

    public override string Usage => "{play} {radio}";

    protected override bool CanAutoTurnOn => true;

    public override bool CanExecuteCommand(Message message) =>
        this.ContainsText(message, "radio") &&
        (this.ContainsText(message, "play") || this.ContainsText(message, "rebuild-cache"));

    protected override async Task<int> ExecuteMusicPlayerCommand(
        MessageContext messageContext,
        PlayersConfig playersConfig,
        MusicPlayersPresetConfig? musicPlayersPresetConfig,
        CancellationToken cancellationToken = default)
    {
        var message = messageContext.Message;
        if (message.Text?.Contains("rebuild-cache") == true)
        {
            await BuildRadioStreamDataCache(overwrite: true);
            return DoNotReplyPlayerStatus;
        }


        // Need to figure out a better way to implement this:
        var radioName = message.Text
            ?.Replace(Localize(message, "play"), string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("play", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(Localize(message, "radio"), string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(Localize(message, " in "), string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("radio", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(playersConfig.Name!, string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace('_', ' ')
            .Trim('/')
            .Trim('_')
            .Trim();

        InternetRadioStationConfig? radio = null;
        if (string.IsNullOrEmpty(radioName) || "random".Equals(radioName, StringComparison.OrdinalIgnoreCase))
        {
            radio = this.radioConfig.Stations?[Random.Shared.Next(this.radioConfig.Stations.Count)];
        }
        else if (int.TryParse(radioName, out var internalId) &&
                 (radio = this.radioConfig.Stations?.FirstOrDefault(r => r.InternalId == internalId)) != null)
        {
            // ??
        }
        else
        {
            radio = this.radioConfig.Stations?.FirstOrDefault(s =>
                s.Id?.Contains(radioName) == true ||
                s.Name?.Contains(radioName, StringComparison.InvariantCultureIgnoreCase) == true);
        }

        var url = await this.GetRadioUrl(radio);
        if (url?.Url is { })
        {
            try
            {
                var name = radio!.Name;
                if (name is { })
                {
                    var radioLocalizedStr = Localize(message, "radio");
                    name = name.Contains(radioLocalizedStr, StringComparison.OrdinalIgnoreCase)
                        ? name
                        : $"{radioLocalizedStr.Capitalize()} {name}";
                }

                if (url.IsContainer == true)
                {
                    await playersConfig.Client.PlayPlaylist(name, url.Url);
                }
                else
                {
                    await playersConfig.Client.PlayUrl(name, url.Url);
                }

                await this.Reply(messageContext, "Playing radio '[radio]'".Format(new { radio = radio?.Name }),
                    cancellationToken);
                return ReplyPlayerStatusDelayLong;
            }
            catch (Exception)
            {
                return 1000;
            }
        }
        else
        {
            await this.Reply(messageContext,
                this.Localize(message, "Can't find [radio]").Format(new { radio = radio?.Name ?? radioName }),
                cancellationToken);
        }


        return DoNotReplyPlayerStatus;
    }

    private async Task AutoRebuildRadioCache()
    {
        await Task.Delay(10000);
        await BuildRadioStreamDataCache();
    }

    private async Task BuildRadioStreamDataCache(bool overwrite = false)
    {
        var scrapedStations = this.radioConfig.ScrapeUrls is not null
            ? await ScrapeRadioStations(this.radioConfig.ScrapeUrls)
            : [];

        var c = 0;

        var cachedRadioInfo = await this.internetRadioRepository.ListStreamData();
        var cachedRadioConfig = cachedRadioInfo?.Select(cri => new InternetRadioStationConfig
        {
            Id = cri.Key,
            Name = cri.Value.Name ?? cri.Key,
            Url = cri.Value.Url,
            IsContainer = cri.Value.IsContainer,
        }) ?? [];

        this.radioConfig.Stations = (this.radioConfig.Stations ?? []).Concat(scrapedStations.Values)
            .Concat(cachedRadioConfig).Distinct().ToList();
        foreach (var radioStation in this.radioConfig.Stations)
        {
            c++;

            var cachedStreamData = cachedRadioInfo?.GetValueOrDefault(radioStation.Id);
            if (!overwrite && cachedStreamData is not null)
            {
                this.LogInformation("Radio stream data for radio '{radio}' is already cached.", radioStation.Id);
                continue;
            }

            var streamData = await DiscoverRadioUrl(radioStation, overwrite);
            if (streamData is null)
            {
                continue;
            }

            cachedStreamData = await this.internetRadioRepository.GetStreamData(radioStation.Id);
            if (cachedStreamData is not null)
            {
                if (cachedStreamData.Url == streamData.Url)
                {
                    this.LogInformation("Successfully rebuilded radio stream data for radio '{radio}'.",
                        radioStation.Id);
                }
            }
            else
            {
                this.LogWarning("Failed to cache radio stream data for radio '{radio}'.", radioStation.Id);
            }
        }

        this.LogDebug("Total stations: {total}", c);
    }

    private async Task<IUrlData?> GetRadioUrl(InternetRadioStationConfig? radio)
    {
        if (radio is { })
        {
            return radio.Url != null
                ? radio
                : await this.memoryCache.GetOrCreateAsync(radio, async (k) =>
                {
                    k.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
                    return await this.DiscoverRadioUrl(radio);
                });
        }

        return null;
    }

    private async Task<RadioDiscoverResponse.ResultData.Stream?> DiscoverRadioUrl(InternetRadioStationConfig radio,
        bool ignoreCache = false)
    {
        var radioId = radio.Id;

        if (!ignoreCache)
        {
            var streamData = await this.internetRadioRepository.GetStreamData(radioId);
            if (streamData is not null)
            {
                return streamData;
            }
        }

        if (this.radioConfig.DiscoverUrl == null)
        {
            return null;
        }

        this.client ??= new HttpClient();
        var url = this.radioConfig.DiscoverUrl;
        url += url.EndsWith('/') ? radioId : "/" + radioId;

        this.LogDebug("Getting DiscoverRadioUrl: {url}", url);
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.UserAgent.TryParseAdd(this.radioConfig.DiscoverUserAgent);
        client.DefaultRequestHeaders.AcceptLanguage.TryParseAdd("en-US");
        client.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
        try
        {
            var response = await client.GetFromJsonAsync<RadioDiscoverResponse>(url);
            this.LogDebug("Getting DiscoverRadioUrl: {url} = {response}", url, response);
            var streamInfo = response?.Result?.Streams?.OrderBy(s => s.IsContainer)
                .FirstOrDefault(s => s.MediaType != "HTML" && s.MediaType != "HLS");
            if (streamInfo != null && string.IsNullOrEmpty(streamInfo.Name))
            {
                streamInfo.Name = radio.Name;
            }

            this.LogDebug("Returning stream info for radio {radio}: {streamInfo}", radioId, streamInfo);

            return await this.internetRadioRepository.SaveStreamData(radioId, streamInfo);
        }
        catch (Exception e)
        {
            LogWarning(e, "Error discovering radio {radioId}", radioId);
            return null;
        }
    }

    private async Task<Dictionary<string, InternetRadioStationConfig>> ScrapeRadioStations(params string[] urls)
    {
        Dictionary<string, InternetRadioStationConfig> result = [];
        foreach (var url in urls)
        {
            try
            {
                using var client = new HttpClient();
                var html = await client.GetStringAsync(url);
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                var radioList = htmlDoc.GetElementbyId("radios");
                var anchorTags = radioList?.SelectNodes(".//a");

                var stationInfo = anchorTags?.Select(a =>
                {
                    var id = a.GetAttributeValue("href", string.Empty);
                    var i = id.LastIndexOf('#') + 1;
                    id = id[i..];

                    var imgNode = a.SelectSingleNode(".//img");
                    var name = imgNode?.GetAttributeValue("alt", string.Empty).Trim() ?? string.Empty;

                    return new InternetRadioStationConfig
                    {
                        Id = id,
                        Name = name
                    };
                }) ?? [];

                foreach (var station in stationInfo)
                {
                    result[station.Id] = station;
                }
            }
            catch (Exception e)
            {
                this.LogWarning(e, "Failed to parse radio stations");
            }
        }

        return result;
    }
}