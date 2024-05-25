using System.Text.Json;
using System.Text.Json.Serialization;

namespace Linkplay.HttpApi.Json;

public class BoolStringJsonConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetString() == "1";

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value ? "1" : "0");
}
