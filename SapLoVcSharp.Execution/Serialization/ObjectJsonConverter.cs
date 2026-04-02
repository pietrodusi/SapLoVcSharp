using System.Text.Json;
using System.Text.Json.Serialization;

namespace SapLoVcSharp.Execution.Serialization
{
    /// <summary>
    /// Custom JSON converter for object properties.
    /// Properly deserializes JsonElement back to actual values (string, number, boolean).
    /// </summary>
    public class ObjectJsonConverter : JsonConverter<object>
    {
        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.Number => reader.GetDouble(),
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                JsonTokenType.Null => null,
                _ => JsonSerializer.Deserialize<JsonElement>(ref reader)
            };
        }

        public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                JsonSerializer.Serialize(writer, value, value.GetType(), options);
            }
        }
    }
}
