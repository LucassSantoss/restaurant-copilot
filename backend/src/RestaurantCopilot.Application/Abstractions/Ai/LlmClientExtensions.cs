using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Application.Abstractions.Ai;

public static class LlmClientExtensions
{
    /// <summary>Serializer settings for structured LLM output: camelCase and enums as strings.</summary>
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
    };

    private static readonly JsonSchemaExporterOptions SchemaOptions = new()
    {
        TreatNullObliviousAsNonNullable = true,
        TransformSchemaNode = AdaptForLlm,
    };

    /// <summary>
    /// Asks the model for JSON matching the schema of <typeparamref name="T"/> (derived from the
    /// type itself, so schema and parser never drift apart) and deserializes the answer.
    /// </summary>
    public static async Task<Result<T>> GenerateJsonAsync<T>(
        this ILlmClient client,
        string systemInstruction,
        string prompt,
        CancellationToken cancellationToken)
    {
        var request = new LlmRequest(systemInstruction, [new UserMessage(prompt)])
        {
            ResponseJsonSchema = JsonSchemaFor<T>(),
            Temperature = 0,
        };

        var response = await client.GenerateAsync(request, cancellationToken);
        if (response.IsFailure)
        {
            return response.Error;
        }

        if (string.IsNullOrWhiteSpace(response.Value.Text))
        {
            return LlmErrors.InvalidResponse("The model returned an empty structured response.");
        }

        try
        {
            return JsonSerializer.Deserialize<T>(response.Value.Text, SerializerOptions) is { } value
                ? value
                : LlmErrors.InvalidResponse("The model returned null instead of a JSON object.");
        }
        catch (JsonException exception)
        {
            return LlmErrors.InvalidResponse($"The model returned invalid JSON: {exception.Message}");
        }
    }

    public static JsonElement JsonSchemaFor<T>() => JsonSchemaFor<T>(SerializerOptions);

    /// <summary>JSON Schema of <typeparamref name="T"/> as serialized with <paramref name="serializerOptions"/>.</summary>
    public static JsonElement JsonSchemaFor<T>(JsonSerializerOptions serializerOptions) =>
        JsonSerializer.SerializeToElement(serializerOptions.GetJsonSchemaAsNode(typeof(T), SchemaOptions));

    /// <summary>
    /// Makes generated schemas friendlier to LLM providers: copies <see cref="DescriptionAttribute"/>
    /// texts into "description" (the model reads them to fill arguments) and replaces nullable
    /// unions like ["string", "null"] with the plain type, expressing optionality by not being required.
    /// </summary>
    private static JsonNode AdaptForLlm(JsonSchemaExporterContext context, JsonNode schema)
    {
        if (schema is not JsonObject node)
        {
            return schema;
        }

        if (node["type"] is JsonArray types && types.Count == 2 && types.Any(type => type?.GetValue<string>() == "null"))
        {
            node["type"] = types.First(type => type?.GetValue<string>() != "null")!.GetValue<string>();
        }

        if (node["enum"] is JsonArray values)
        {
            values.Where(value => value is null).ToList().ForEach(value => values.Remove(value));
        }

        var description = (context.PropertyInfo?.AttributeProvider ?? context.TypeInfo.Type)
            .GetCustomAttributes(typeof(DescriptionAttribute), inherit: false)
            .OfType<DescriptionAttribute>()
            .FirstOrDefault()?.Description;
        if (description is not null)
        {
            node["description"] = description;
        }

        return node;
    }
}
