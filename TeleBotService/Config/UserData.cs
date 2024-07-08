using System.Collections;

namespace TeleBotService.Config;

public class UserData
{
    public const string DeleteSettingValue = "DELETED_\0\0\n\n\t\t\t\n\n\0\0_DELETED";
    public const string NetClientMonitorChatIdKeyName = "NetClientMonitorChatId";

    private readonly Dictionary<string, string?> settings = [];

    private bool areSettingsDirty;
    private bool areSettingsLoaded;

    public string? UserName { get; internal set; }

    public bool IsAdmin { get; init; }
    public string? Language { get; init; }
    public bool Enabled { get; init; }

    public int GetIntSetting(string key, int defaultValue = default) =>
        this.settings?.GetValueOrDefault(key) is { } strVal && int.TryParse(strVal, out var result) ? result : defaultValue;

    public bool GetBoolSetting(string key, bool defaultValue = default) =>
        this.settings?.GetValueOrDefault(key) is { } strVal && bool.TryParse(strVal, out var result) ? result : defaultValue;

    public string? GeStringSetting(string key, string? defaultValue = default) =>
        this.settings?.GetValueOrDefault(key, defaultValue) is { } result && result != DeleteSettingValue ? result : defaultValue;

    public void SetSetting<T>(string key, T? value)
    {
        if (this.settings != null)
        {
            this.settings[key] = value?.ToString();
            this.areSettingsDirty = true;
        }
    }

    public void RemoveSetting(string key)
    {
        if (this.settings != null)
        {
            if (this.settings.ContainsKey(key))
            {
                this.settings[key] = DeleteSettingValue;
            }

            this.areSettingsDirty = true;
        }
    }

    public async ValueTask<bool> SaveSettings(Func<UserData, UserSettings, ValueTask<bool>> saveAction)
    {
        var saved = false;

        if (this.areSettingsDirty)
        {
            saved = await saveAction.Invoke(this, new(this.settings));
            this.areSettingsDirty = !saved;
        }

        return saved;
    }

    public async ValueTask<bool> LoadSettings(Func<UserData, ValueTask<UserSettings>> loadAction)
    {
        if (!this.areSettingsLoaded)
        {
            var settings = await loadAction.Invoke(this);
            foreach (var kvp in settings)
            {
                this.settings[kvp.Key] = kvp.Value;
            }

            this.areSettingsLoaded = true;
        }

        return this.areSettingsLoaded;
    }
}

public class UserSettings : IEnumerable<KeyValuePair<string, string?>>
{
    public static readonly UserSettings Empty = new ([]);

    private readonly IEnumerable<KeyValuePair<string, string?>> settings;

    public UserSettings(IEnumerable<KeyValuePair<string, string?>> settings)
    {
        this.settings = settings;
    }

    public IEnumerator<KeyValuePair<string, string?>> GetEnumerator() => this.settings.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.settings.GetEnumerator();
}