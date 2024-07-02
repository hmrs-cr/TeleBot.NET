using System.Text.Json.Serialization;

namespace Omada.OpenApi.Client.Requests;

public record AuthorizeTokenRequest
{
    [JsonPropertyName("omadacId")]
    public required string OmadacId { get; init; }

    [JsonPropertyName("client_id")]
    public required string ClientId { get; init; }

    [JsonPropertyName("client_secret")]
    public required string ClientSecret { get; init; }
}