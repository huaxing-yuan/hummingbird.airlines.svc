using Hummingbird.Airlines.Backend.Domain;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Hummingbird.Airlines.Middleware.Api;

/// <summary>
/// Emits the polymorphic baggage contract in the OpenAPI document. Because Baggage uses a
/// custom tolerant JSON converter (no [JsonPolymorphic] metadata), Swashbuckle would emit a
/// flat schema otherwise. This filter shapes it into a discriminated union:
///   Baggage -> oneOf { CheckedBaggage, CarryOnBaggage } + discriminator propertyName "type"
/// and flattens inherited members into the derived schemas (no allOf cycle).
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
            concrete.Type = null;
            concrete.Properties?.Clear();
            concrete.Required?.Clear();

            concrete.OneOf ??= new List<IOpenApiSchema>();
            concrete.OneOf.Clear();
            concrete.OneOf.Add(context.SchemaGenerator.GenerateSchema(typeof(CheckedBaggage), context.SchemaRepository));
            concrete.OneOf.Add(context.SchemaGenerator.GenerateSchema(typeof(CarryOnBaggage), context.SchemaRepository));

            concrete.Discriminator = new OpenApiDiscriminator { PropertyName = "type" };
            return;
        }

        if (context.Type == typeof(CheckedBaggage) || context.Type == typeof(CarryOnBaggage))
        {
            concrete.AllOf = null;
            concrete.Properties ??= new Dictionary<string, IOpenApiSchema>();
            AddInheritedMembers(concrete.Properties, context);
            concrete.Required ??= new HashSet<string>();
            concrete.Required.Add("type");
        }
    }

    private static void AddInheritedMembers(IDictionary<string, IOpenApiSchema> properties, SchemaFilterContext context)
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
}