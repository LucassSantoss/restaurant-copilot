using System.ComponentModel.DataAnnotations;

namespace RestaurantCopilot.Infrastructure.Ai.Gemini;

/// <summary>
/// Gemini settings, bound from the "Gemini" configuration section. Model names are
/// configuration, never code: the models offered on the free tier change over time.
/// </summary>
public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    /// <summary>Google AI Studio API key. Comes from user-secrets or the Gemini__ApiKey variable.</summary>
    public string? ApiKey { get; init; }

    [Required]
    public Uri BaseUrl { get; init; } = new("https://generativelanguage.googleapis.com/v1beta/");

    [Required]
    public string GenerationModel { get; init; } = "gemini-2.5-flash";

    [Required]
    public string EmbeddingModel { get; init; } = "gemini-embedding-001";

    /// <summary>Output size requested from the embedding model (it supports truncated vectors).</summary>
    [Range(128, 3072)]
    public int EmbeddingDimensions { get; init; } = 768;

    /// <summary>Retries for transient failures (429 and 5xx), with exponential backoff and jitter.</summary>
    [Range(0, 10)]
    public int MaxRetryAttempts { get; init; } = 4;

    public TimeSpan RetryBaseDelay { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>Timeout of a single HTTP attempt.</summary>
    public TimeSpan AttemptTimeout { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>Timeout of the whole call, retries included.</summary>
    public TimeSpan TotalTimeout { get; init; } = TimeSpan.FromMinutes(3);

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
