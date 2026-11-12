using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace RestaurantCopilot.Application.Agent.Tools;

/// <summary>
/// Serializer settings for everything exchanged with the model through tools: snake_case names and
/// enum values (the convention LLMs follow best), and constructor parameters without a default value
/// are required, so a missing argument fails deserialization with a message the model can act on.
/// </summary>
internal static class AgentJson
{
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        RespectRequiredConstructorParameters = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
    };
}
