namespace Linkplay.HttpApi.Model;

// The audio source that has to be switched
public sealed class PlayerMode : StringEnum
{
    public static readonly PlayerMode Wifi = new("wifi"); // wifi mode
    public static readonly PlayerMode LineIn = new("line-in"); // line analogue input
    public static readonly PlayerMode Bluetooth = new("bluetooth"); // bluetooth mode
    public static readonly PlayerMode Optical = new("optical"); // optical digital input
    public static readonly PlayerMode Coaxial = new("co-axial"); // co-axial digital input
    public static readonly PlayerMode LineIn2 = new("line-in2"); // line analogue input #2
    public static readonly PlayerMode UDisk = new("udisk"); // UDisk mode
    public static readonly PlayerMode PCUSB = new("PCUSB"); // USBDAC mode

    private PlayerMode(string value) : base(value) { }
}
