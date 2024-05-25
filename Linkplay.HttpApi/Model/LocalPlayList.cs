namespace Linkplay.HttpApi.Model;

public record struct LocalPlayList
{
    public LocalPlayList() { }

    public int Num { get; set; }

    public ICollection<LocalPlayListEntry> Locallist { get; set; }

    public record struct LocalPlayListEntry
    {
        public HexedString File { get; set; }
    }
}


public record struct LocalInfolist
{
    public int Num { get; set; }
    public ICollection<InfolistEntry> Infolist { get; set; }

    public record struct InfolistEntry
    {
        public HexedString Filename { get; set; }
        public string Totlen { get; set; }
        public HexedString Title { get; set; }
        public HexedString Artist { get; set; }
        public HexedString Album { get; set; }
    }
}


