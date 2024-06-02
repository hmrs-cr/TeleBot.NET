﻿using System.Text;
using Microsoft.Extensions.Options;
using TeleBotService.Config;
using TeleBotService.Core.Model;
using TeleBotService.Extensions;
using Telegram.Bot.Types;

namespace TeleBotService.Core.Commands;

public class RadioPlayerCommand : MusicPlayerCommandBase
{
    private readonly InternetRadioConfig radioConfig;

    public RadioPlayerCommand(
        IOptions<MusicPlayersConfig> config,
        IOptions<TapoConfig> tapoConfig,
        IOptions<InternetRadioConfig> radioConfig,
        ILogger<RadioPlayerCommand> logger) : base(config, tapoConfig, logger)
    {
        this.radioConfig = radioConfig.Value;
    }

    public override string Description => "Internet Radio";

    public override string Usage => "{play} {play}\n{list} {radio}";

    public override bool CanExecuteCommand(Message message) =>
        this.ContainsText(message, "radio") && (this.ContainsText(message, "play") || this.ContainsText(message, "list"));

    protected override async Task<int> ExecuteMusicPlayerCommand(
        Message message,
        PlayersConfig playersConfig,
        MusicPlayersPresetConfig? musicPlayersPresetConfig,
        CancellationToken cancellationToken = default)
    {
        if (this.ContainsText(message, "play"))
        {
            var radioId = message.GetLastString("__")?.Replace('_', '-');
            var radio = this.radioConfig.Stations?.FirstOrDefault(s => s.Id == radioId);
            var url = await this.GetRadioUrl(radio);
            if (url?.Url is { })
            {
                if (url.IsContainer == true)
                {
                    await playersConfig.Client.PlayPlaylist(url.Url);
                }
                else
                {
                    await playersConfig.Client.PlayUrl(url.Url);
                }

                await this.Reply(message, "Playing radio '[radio]'".Format(new { radio = radio?.Name }), cancellationToken);
            }
            else
            {
                await this.Reply(message, this.Localize(message, "Can't find [radio]").Format(new { radio = radio?.Name ?? radioId }), cancellationToken);
            }
        }
        else if (this.ContainsText(message, "list"))
        {
            if (this.radioConfig.Stations?.Count > 0)
            {
                var sb = new StringBuilder();
                foreach (var radio in this.radioConfig.Stations)
                {
                    sb.Append("<b>").Append(radio.Name).Append("</b> (<i>").Append(radio.Id).Append("</i>) ==> ")
                      .Append('/').Append(this.Localize(message, "play")).Append(this.Localize(message, "radio")).Append("__").Append(radio.Id?.Replace('-', '_'))
                      .AppendLine();
                }

                await this.ReplyFormated(message, sb.ToString(), cancellationToken);
            }
            else
            {
                await this.Reply(message, "No radio stations configured", cancellationToken);
            }
        }

        return DoNotReplyPlayerStatus;
    }

    private async Task<IUrlData?> GetRadioUrl(InternetRadioStationConfig? radio)
    {
        if (radio is { })
        {
            return radio.Url != null ? radio : await this.DiscoverRadioUrl(radio.Id);
        }

        return null;
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
        var streamInfo = response?.Result?.Streams?.OrderBy(s => s.IsContainer).FirstOrDefault(s => s.MediaType != "HTML");
        this.LogDebug("Returning stream info for radio {radio}: {streamInfo}", radioId, streamInfo);
        return streamInfo;
    }
}
