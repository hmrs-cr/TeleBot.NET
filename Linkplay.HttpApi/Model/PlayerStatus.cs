namespace Linkplay.HttpApi.Model;

public record PlayerStatus
{
    public PlayerType Type { get; set; }
    public Channels Ch { get; set; }
    public PlaybackMode Mode { get; set; }
    public ShuffleMode Loop { get; set; }
    public int Eq { get; set; }
    public string Status { get; set; }
    public int Curpos { get; set; }
    public int OffsetPts { get; set; }
    public int Totlen { get; set; }
    public HexedString Title { get; set; }
    public HexedString Artist { get; set; }
    public HexedString Album { get; set; }
    public int AlarmFlag { get; set; }
    public int Plicount { get; set; }
    public int Plicurr { get; set; }
    public int Vol { get; set; }
    public bool Mute { get; set; }
}
