namespace WebjetAPI.Utilities;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Custom JSON converter to handle decimal values that may come as strings.
/// </summary>
public class JsonStringConverterForDecimal : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && decimal.TryParse(reader.GetString(), out var result))
        {
            return result;
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetDecimal();
        }
        throw new JsonException($"Unexpected token {reader.TokenType} when parsing decimal.");
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}
