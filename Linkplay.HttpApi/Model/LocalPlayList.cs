namespace Linkplay.HttpApi.Model;

public record struct LocalPlayList
{
    public LocalPlayList() { }

    public int Num { get; init; }

    public ICollection<LocalPlayListEntry>? Locallist { get; init; }
}