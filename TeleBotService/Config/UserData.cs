namespace TeleBotService.Config;

public class UserData
{
    public string? UserName { get; internal set; }

    public bool IsAdmin { get; init; }
    public string? Language { get; init; }
    public bool Enabled { get; init; }

    public Dictionary<string, string?>? Settings { get; init; }

    public int GetIntSetting(string key, int defaultValue = default) =>
        this.Settings?.GetValueOrDefault(key) is { } strVal && int.TryParse(strVal, out var result) ? result : defaultValue;

    public bool GetBoolSetting(string key, bool defaultValue = default) =>
        this.Settings?.GetValueOrDefault(key) is { } strVal && bool.TryParse(strVal, out var result) ? result : defaultValue;

    public string? GeStringSetting(string key, string? defaultValue = default) =>
        this.Settings?.GetValueOrDefault(key, defaultValue);

    public void SetSetting<T>(string key, T? value)
     {
        if (this.Settings != null)
        {
            this.Settings[key] = value?.ToString();
        }
     }

}