﻿namespace TeleBotService.Config;

public class InternetRadioConfig
{
    public const string InternetRadioConfigName = "InternetRadio";

    public string? DiscoverUrl { get; init; }
    public string? DiscoverUserAgent { get; init; }
    public IReadOnlyList<InternetRadioStationConfig>? Stations { get; init; }
}