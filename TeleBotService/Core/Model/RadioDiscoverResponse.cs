namespace TeleBotService.Core.Model;

public record RadioDiscoverResponse
{
    public ResultData? Result { get; init; }

    public record ResultData
    {
        public IReadOnlyList<Stream>? Streams { get; init; }

        public record Stream
        {
            public bool IsContainer { get; init; }
            public Uri? Url { get; init; }
        }
    }
}




