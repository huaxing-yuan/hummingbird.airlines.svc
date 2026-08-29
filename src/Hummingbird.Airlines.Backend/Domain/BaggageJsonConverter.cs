using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hummingbird.Airlines.Backend.Domain;

/// <summary>
/// Tolerant polymorphic baggage codec.
///
/// Reading accepts both discriminators:
///   canonical STJ       { "type": "checked" | "carryOn", ... }
///   generated client    { "$type": "...CheckedBaggage, ...", ... }  (Newtonsoft-style)
/// The type is matched on the discriminator value or on the type name embedded in
/// "$type". Writing always emits the canonical "type" discriminator.
/// </summary>
public sealed class BaggageJsonConverter : JsonConverter<Baggage>
{
    public override Baggage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        string? discriminator = null;
        if (root.TryGetProperty("type", out var typeProperty))
        {
            discriminator = typeProperty.GetString();
        }
        else if (root.TryGetProperty("$type", out var dollarTypeProperty))
        {
            discriminator = dollarTypeProperty.GetString();
        }

        var targetType = discriminator switch
        {
            "checked" => typeof(CheckedBaggage),
            "carryOn" => typeof(CarryOnBaggage),
            _ when discriminator?.Contains("CheckedBaggage", StringComparison.Ordinal) == true => typeof(CheckedBaggage),
            _ when discriminator?.Contains("CarryOnBaggage", StringComparison.Ordinal) == true => typeof(CarryOnBaggage),
            _ => null,
        };

        if (targetType is null)
        {
            throw new JsonException("The polymorphic baggage payload must specify a 'type' (or '$type') discriminator.");
        }

        return (Baggage?)root.Deserialize(targetType, options);
    }

    public override void Write(Utf8JsonWriter writer, Baggage value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("type", value is CheckedBaggage ? "checked" : "carryOn");

        using var snapshot = JsonSerializer.SerializeToDocument(value, value.GetType(), options);
        foreach (var property in snapshot.RootElement.EnumerateObject())
        {
            property.WriteTo(writer);
        }

        writer.WriteEndObject();
    }
}