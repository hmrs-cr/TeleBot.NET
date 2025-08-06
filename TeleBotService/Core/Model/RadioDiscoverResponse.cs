using System.Text.Json;
using System.Text.Json.Serialization;
using TeleBotService.Config;

namespace TeleBotService.Core.Model;

public record RadioDiscoverResponse
{
    public ResultData? Result { get; init; }

    public record ResultData
    {
        public IReadOnlyList<Stream>? Streams { get; init; }

        public record Stream : IUrlData
        {
            private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
            };
            
            public string? Name { get; set; }
            
            public string? MediaType { get; init; }
            public bool? IsContainer { get; init; }
            public Uri? Url { get; init; }

            public override string ToString() => JsonSerializer.Serialize(this, JsonOptions);
            
            public static Stream FromJson(string json) => JsonSerializer.Deserialize<Stream>(json, JsonOptions) ?? throw new NullReferenceException();
        }
    }
}




