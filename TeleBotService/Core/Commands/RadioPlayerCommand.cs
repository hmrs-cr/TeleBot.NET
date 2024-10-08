﻿using Microsoft.Extensions.Caching.Memory;
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
    }

    public override string Description => "Internet Radio";

    public override string Usage => "{play} {radio}";

    protected override bool CanAutoTurnOn => true;

    public override bool CanExecuteCommand(Message message) =>
        this.ContainsText(message, "radio") && this.ContainsText(message, "play");

    protected override async Task<int> ExecuteMusicPlayerCommand(
        MessageContext messageContext,
        PlayersConfig playersConfig,
        MusicPlayersPresetConfig? musicPlayersPresetConfig,
        CancellationToken cancellationToken = default)
    {
        var message = messageContext.Message;
        // Need to figure out a better way to implement this:
        var radioName = message.Text?.Replace(Localize(message, "play"), string.Empty, StringComparison.OrdinalIgnoreCase)
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
        else if (int.TryParse(radioName, out var internalId) && (radio = this.radioConfig.Stations?.FirstOrDefault(r => r.InternalId == internalId)) != null)
        {
            // ??
        }
        else
        {
            radio = this.radioConfig.Stations?.FirstOrDefault(s => s.Id?.Contains(radioName) == true || s.Name?.Contains(radioName, StringComparison.InvariantCultureIgnoreCase) == true);
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
                    name = name.Contains(radioLocalizedStr, StringComparison.OrdinalIgnoreCase) ? name : $"{radioLocalizedStr.Capitalize()} {name}";
                }

                if (url.IsContainer == true)
                {
                    await playersConfig.Client.PlayPlaylist(name, url.Url);
                }
                else
                {
                    await playersConfig.Client.PlayUrl(name, url.Url);
                }

                await this.Reply(messageContext, "Playing radio '[radio]'".Format(new { radio = radio?.Name }), cancellationToken);
                return ReplyPlayerStatusDelayLong;
            }
            catch (Exception)
            {
                return 1000;
            }

        }
        else
        {
            await this.Reply(messageContext, this.Localize(message, "Can't find [radio]").Format(new { radio = radio?.Name ?? radioName }), cancellationToken);
        }


        return DoNotReplyPlayerStatus;
    }

    private async Task<IUrlData?> GetRadioUrl(InternetRadioStationConfig? radio)
    {
        if (radio is { })
        {
            return radio.Url != null ? radio : await this.memoryCache.GetOrCreateAsync(radio, async (k) =>
            {
                k.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
                return this.SaveDicoveredUrl(radio, await this.DiscoverRadioUrl(radio.Id));
            });
        }

        return null;
    }

    private IUrlData? SaveDicoveredUrl(InternetRadioStationConfig radio, RadioDiscoverResponse.ResultData.Stream? stream)
    {
        _ = this.internetRadioRepository.SaveDiscoveredUrl(radio.Id!, stream?.Url);
        return stream;
    }

    private async Task<RadioDiscoverResponse.ResultData.Stream?> DiscoverRadioUrl(string? radioId)
    {
        if (this.radioConfig.DiscoverUrl == null)
        {
            return null;
        }

        using var client = new HttpClient();
        var url = this.radioConfig.DiscoverUrl;
        url += url.EndsWith('/') ? radioId : "/" + radioId;

        this.LogDebug("Getting DiscoverRadioUrl: {url}", url);
        client.DefaultRequestHeaders.UserAgent.TryParseAdd(this.radioConfig.DiscoverUserAgent);
        var response = await client.GetFromJsonAsync<RadioDiscoverResponse>(url);
        this.LogDebug("Getting DiscoverRadioUrl: {url} = {response}", url, response);
        var streamInfo = response?.Result?.Streams?.OrderBy(s => s.IsContainer).FirstOrDefault(s => s.MediaType != "HTML" && s.MediaType != "HLS");
        this.LogDebug("Returning stream info for radio {radio}: {streamInfo}", radioId, streamInfo);
        return streamInfo;
    }
}
