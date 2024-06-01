namespace Linkplay.HttpApi.Model;

public partial record struct LocalInfolist
{
    public int Num { get; init; }
    public ICollection<InfolistEntry> Infolist { get; init; }
}


