using System.Globalization;
using System.Text.RegularExpressions;

namespace RestaurantCopilot.Evaluation.Grading;

/// <summary>
/// Pulls every number out of an answer. Brazilian answers write "R$ 13.143,60" but models sometimes
/// use "13,143.60" or "13143.6", so each token is read both ways and all readings are candidates.
/// </summary>
internal static partial class NumberExtractor
{
    public static IReadOnlyList<decimal> Extract(string text)
    {
        var numbers = new List<decimal>();
        foreach (Match match in NumberToken().Matches(text))
        {
            var token = match.Value;
            AddIfParsed(numbers, token.Replace(".", string.Empty, StringComparison.Ordinal).Replace(',', '.'));
            AddIfParsed(numbers, token.Replace(",", string.Empty, StringComparison.Ordinal));
        }

        return numbers.Distinct().ToList();
    }

    private static void AddIfParsed(List<decimal> numbers, string candidate)
    {
        if (decimal.TryParse(candidate, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
        {
            numbers.Add(value);
        }
    }

    [GeneratedRegex(@"\d+(?:[.,]\d+)*")]
    private static partial Regex NumberToken();
}
