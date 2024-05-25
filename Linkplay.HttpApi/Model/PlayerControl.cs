namespace Linkplay.HttpApi.Model;

public sealed class PlayerControl : StringEnum
{
    public static readonly PlayerControl Pause = new("pause"); //	Pause playback
    public static readonly PlayerControl Resume = new("resume"); //	Resume playback
    public static readonly PlayerControl OnePause = new("onepause", 32, 5000); //	Toggle Play/Pause
    public static readonly PlayerControl Stop = new("stop"); //	Stop current playback and removes slected source from device
    public static readonly PlayerControl Prev = new("prev", 64, 500, 2); //	Play previous song in playlist
    public static readonly PlayerControl Next = new("next", 64, 500); //	Play next song in playlist

    private PlayerControl(string value, int maxRepeat = 1, int repeatDelay = -1, int defaultRepeat = 1) : base(value)
    {
        this.MaxRepeat = maxRepeat;
        this.RepeatDelay = repeatDelay;
        this.DefaultRepeat = defaultRepeat;
    }

    public int MaxRepeat { get; }
    public int RepeatDelay { get; }
    public int DefaultRepeat { get; }

    public bool CanBeRepeated => this.MaxRepeat > 1;
}
