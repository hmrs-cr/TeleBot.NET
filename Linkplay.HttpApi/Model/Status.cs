namespace Linkplay.HttpApi.Model;

public class Status : StringEnum
{
    public static readonly Status Stop = new("stop"); // stop: no audio selected
    public static readonly Status Play = new("play"); // play: playing audio
    public static readonly Status Load = new("load"); // load: load ??
    public static readonly Status Pause = new("pause"); // pause: audio paused

    private Status(string value) : base(value) { }
}
