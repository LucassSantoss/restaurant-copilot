using System.Text.Json;
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

    public static JsonElement JsonSchemaFor<T>() =>
        JsonSerializer.SerializeToElement(SerializerOptions.GetJsonSchemaAsNode(typeof(T), SchemaOptions));
}
