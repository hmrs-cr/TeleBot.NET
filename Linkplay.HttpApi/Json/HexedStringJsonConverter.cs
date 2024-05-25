using System.Text.Json;
using System.Text.Json.Serialization;
using Linkplay.HttpApi.Model;

namespace Linkplay.HttpApi.Json;

public class HexedStringJsonConverter : JsonConverter<HexedString>
{
    public override HexedString Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(reader.GetString());

    public override void Write(Utf8JsonWriter writer, HexedString value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
