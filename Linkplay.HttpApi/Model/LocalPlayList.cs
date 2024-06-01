namespace Linkplay.HttpApi.Model;

public record LocalPlayList
{
    public LocalPlayList() { }

    public int Num { get; init; }

    public ICollection<LocalPlayListEntry>? Locallist { get; init; }
}