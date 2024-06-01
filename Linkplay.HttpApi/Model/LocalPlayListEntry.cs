namespace Linkplay.HttpApi.Model;

public record struct LocalPlayListEntry
{
    public HexedString File { get; init; }
}