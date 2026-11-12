using System.Text.Json;
using System.Text.Json.Serialization;

namespace RestaurantCopilot.Evaluation;

/// <summary>The question set in evaluation/questions.json.</summary>
internal sealed record EvaluationDataset(string Description, IReadOnlyList<EvaluationQuestion> Questions)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static async Task<EvaluationDataset> LoadAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<EvaluationDataset>(stream, SerializerOptions, cancellationToken)
            ?? throw new InvalidDataException($"'{path}' is empty.");
    }
}

/// <param name="Suites">Named subsets the question belongs to (e.g. "smoke"); every question is in "full".</param>
/// <param name="ExpectedTools">Tools a good answer is expected to use; reported, not graded.</param>
/// <param name="ReferenceSql">SQL that reproduces the expected value from the seed data.</param>
internal sealed record EvaluationQuestion(
    string Id,
    string Category,
    string Question,
    GradingSpec Grading,
    IReadOnlyList<string>? Suites = null,
    IReadOnlyList<string>? ExpectedTools = null,
    string? ReferenceSql = null);

internal static class Suites
{
    /// <summary>A handful of cheap questions that fit the free-tier daily quota. The default.</summary>
    public const string Smoke = "smoke";

    /// <summary>Every question.</summary>
    public const string Full = "full";

    public static bool Contains(this EvaluationQuestion question, string suite) =>
        suite == Full || (question.Suites?.Contains(suite) ?? false);
}

internal enum GradingType
{
    /// <summary>Every expected number must appear in the answer (exact match within a tolerance).</summary>
    Numeric,

    /// <summary>Every keyword must appear; "a|b" accepts either alternative. Case and accents are ignored.</summary>
    Keywords,

    /// <summary>An LLM judge checks the answer against free-text criteria.</summary>
    Judge,
}

/// <param name="Tolerance">Allowed absolute difference for numeric grading; default is 0.5% of the value (min 0.01).</param>
internal sealed record GradingSpec(
    GradingType Type,
    IReadOnlyList<decimal>? Expected = null,
    decimal? Tolerance = null,
    IReadOnlyList<string>? Keywords = null,
    string? Criteria = null);
