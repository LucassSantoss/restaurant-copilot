using System.ComponentModel.DataAnnotations;

namespace RestaurantCopilot.Infrastructure.Sql;

public sealed class TextToSqlOptions
{
    public const string SectionName = "TextToSql";

    /// <summary>Maximum rows returned to the model; larger results are truncated.</summary>
    [Range(1, 1000)]
    public int MaxRows { get; init; } = 100;

    [Range(typeof(TimeSpan), "00:00:01", "00:01:00")]
    public TimeSpan StatementTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>Maximum length of the SQL text written by the model.</summary>
    [Range(100, 20000)]
    public int MaxSqlLength { get; init; } = 4000;
}
