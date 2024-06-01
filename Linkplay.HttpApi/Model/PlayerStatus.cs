namespace Linkplay.HttpApi.Model;

public record PlayerStatus
{
    public PlayerType Type { get; init; }
    public Channels Ch { get; init; }
    public PlaybackMode Mode { get; init; }
    public ShuffleMode Loop { get; init; }
    public int Eq { get; init; }
    public string? Status { get; init; }
    public int Curpos { get; init; }
    public int OffinitPts { get; init; }
    public int Totlen { get; init; }
    public HexedString Title { get; init; }
    public HexedString Artist { get; init; }
    public HexedString Album { get; init; }
    public int AlarmFlag { get; init; }
    public int Plicount { get; init; }
    public int Plicurr { get; init; }
    public int Vol { get; init; }
    public bool Mute { get; init; }
}
