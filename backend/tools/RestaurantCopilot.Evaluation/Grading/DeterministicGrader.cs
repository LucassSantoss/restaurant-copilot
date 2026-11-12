using System.Globalization;
using System.Text;

namespace RestaurantCopilot.Evaluation.Grading;

/// <summary>Grades answers whose correctness can be checked without an LLM: numbers and keywords.</summary>
internal static class DeterministicGrader
{
    private const decimal DefaultRelativeTolerance = 0.005m;
    private const decimal MinimumTolerance = 0.01m;

    public static GradeResult GradeNumeric(string answer, IReadOnlyList<decimal> expected, decimal? tolerance)
    {
        var found = NumberExtractor.Extract(answer);
        var missing = expected
            .Where(value =>
            {
                var allowed = tolerance ?? Math.Max(Math.Abs(value) * DefaultRelativeTolerance, MinimumTolerance);
                return !found.Any(number => Math.Abs(number - value) <= allowed);
            })
            .ToList();

        return missing.Count == 0
            ? GradeResult.Pass($"Found {string.Join(", ", expected.Select(Format))}.")
            : GradeResult.Fail($"Missing {string.Join(", ", missing.Select(Format))}.");
    }

    public static GradeResult GradeKeywords(string answer, IReadOnlyList<string> keywords)
    {
        var normalizedAnswer = Normalize(answer);
        var missing = keywords
            .Where(keyword => !keyword.Split('|').Any(alternative => normalizedAnswer.Contains(Normalize(alternative), StringComparison.Ordinal)))
            .ToList();

        return missing.Count == 0
            ? GradeResult.Pass($"Mentions {string.Join(", ", keywords)}.")
            : GradeResult.Fail($"Does not mention {string.Join(", ", missing)}.");
    }

    internal static string Normalize(string text)
    {
        var decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        return new string(decomposed.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
    }

    private static string Format(decimal value) => value.ToString(CultureInfo.InvariantCulture);
}
