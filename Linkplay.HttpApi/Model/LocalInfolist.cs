namespace Linkplay.HttpApi.Model;

public record LocalInfolist
{
    public int Num { get; init; }
    public ICollection<InfolistEntry>? Infolist { get; init; }
}


