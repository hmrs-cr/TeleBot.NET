namespace Linkplay.HttpApi.Model;

public enum ShuffleMode
{
    ShuffleDisabledRepeatEnabledLoop = 0, // Shuffle disabled, Repeat enabled - loop
    ShuffleDisabledRepeatEnabledLoopOnce = 1, // Shuffle disabled, Repeat enabled - loop once
    ShuffleEnabledRepeatEnabledLoop = 2, // Shuffle enabled, Repeat enabled - loop
    ShuffleEnabledRepeatDisabled = 3, // Shuffle enabled, Repeat disabled
    ShuffleDisabledRepeatDisabled = 4, // Shuffle disabled, Repeat disabled
    ShuffleEnabledRepeatEnabledLoopOnce = 5, // Shuffle enabled, Repeat enabled - loop once
}
