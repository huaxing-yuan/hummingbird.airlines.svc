using System.Text.Json.Nodes;
using Hummingbird.Airlines.Backend.Domain;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Hummingbird.Airlines.Middleware.Api;

/// <summary>
/// Emits a self-describing polymorphic baggage contract. Because Baggage uses a custom
/// tolerant JSON converter (no [JsonPolymorphic] metadata), Swashbuckle would emit a flat
/// schema otherwise. This filter shapes it into a complete discriminated union:
///
///   Baggage -> oneOf { CheckedBaggage, CarryOnBaggage }
///            + discriminator { propertyName: "type", mapping: { checked: CheckedBaggage,
///                                                               carryOn: CarryOnBaggage } }
///            + a declared "type" property (enum of allowed values)
///
/// Derived schemas declare the same "type" property restricted to their own single value
/// and flatten the inherited members, so a generator can always derive the exact value to
/// send (e.g. type = "carryOn" for CarryOnBaggage) without guessing.
/// </summary>
public sealed class PolymorphicBaggageSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema concrete)
        {
            return;
        }

        if (context.Type == typeof(Baggage))
        {
            concrete.Type = JsonSchemaType.Object;
            concrete.Properties = new Dictionary<string, IOpenApiSchema>
            {
                ["type"] = DiscriminatorPropertySchema(["checked", "carryOn"]),
            };
            concrete.Required = new HashSet<string> { "type" };

            concrete.OneOf ??= new List<IOpenApiSchema>();
            concrete.OneOf.Clear();
            var checkedSchema = context.SchemaGenerator.GenerateSchema(typeof(CheckedBaggage), context.SchemaRepository);
            var carryOnSchema = context.SchemaGenerator.GenerateSchema(typeof(CarryOnBaggage), context.SchemaRepository);
            concrete.OneOf.Add(checkedSchema);
            concrete.OneOf.Add(carryOnSchema);

            concrete.Discriminator = new OpenApiDiscriminator
            {
                PropertyName = "type",
                Mapping = new Dictionary<string, OpenApiSchemaReference>
                {
                    ["checked"] = checkedSchema as OpenApiSchemaReference
                        ?? context.SchemaRepository.AddDefinition(nameof(CheckedBaggage), (OpenApiSchema)checkedSchema),
                    ["carryOn"] = carryOnSchema as OpenApiSchemaReference
                        ?? context.SchemaRepository.AddDefinition(nameof(CarryOnBaggage), (OpenApiSchema)carryOnSchema),
                },
            };
            return;
        }

        if (context.Type == typeof(CheckedBaggage) || context.Type == typeof(CarryOnBaggage))
        {
            concrete.AllOf = null;
            concrete.Properties ??= new Dictionary<string, IOpenApiSchema>();

            var ownValue = context.Type == typeof(CheckedBaggage) ? "checked" : "carryOn";
            concrete.Properties["type"] = DiscriminatorPropertySchema([ownValue]);
            AddInheritedMembers(concrete.Properties, context);

            concrete.Required ??= new HashSet<string>();
            concrete.Required.Add("type");
        }
    }

    private static void AddInheritedMembers(
        IDictionary<string, IOpenApiSchema> properties,
        SchemaFilterContext context)
    {
        if (properties.ContainsKey("weightKg"))
        {
            return;
        }

        var generator = context.SchemaGenerator;
        var repository = context.SchemaRepository;
        properties["weightKg"] = generator.GenerateSchema(typeof(double), repository);
        properties["color"] = generator.GenerateSchema(typeof(string), repository);
        properties["tagId"] = generator.GenerateSchema(typeof(string), repository);
    }

    private static OpenApiSchema DiscriminatorPropertySchema(string[] allowedValues) => new()
    {
        Type = JsonSchemaType.String,
        Enum = allowedValues.Select(v => (JsonNode)JsonValue.Create(v)).ToList(),
        Description = "Discriminator value selecting the concrete baggage type.",
        Example = JsonValue.Create(allowedValues[0]),
    };
}