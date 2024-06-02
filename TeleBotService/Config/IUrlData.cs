namespace TeleBotService.Config;

public interface IUrlData
{
    public Uri? Url { get; init; }

    public bool? IsContainer { get; init; }
}


