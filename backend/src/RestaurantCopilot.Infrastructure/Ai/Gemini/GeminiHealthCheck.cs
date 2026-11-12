using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace RestaurantCopilot.Infrastructure.Ai.Gemini;

/// <summary>
/// Checks that the API key is accepted and the configured model exists, using the model
/// metadata endpoint, which does not consume generation quota.
/// </summary>
internal sealed class GeminiHealthCheck(HttpClient httpClient, IOptions<GeminiOptions> options) : IHealthCheck
{
    private readonly GeminiOptions _options = options.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Gemini API key is not configured.");
        }

        try
        {
            using var response = await httpClient.GetAsync($"models/{_options.GenerationModel}", cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy($"Model {_options.GenerationModel} is reachable.")
                : new HealthCheckResult(
                    context.Registration.FailureStatus,
                    $"Gemini responded {(int)response.StatusCode} for model {_options.GenerationModel}.");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Gemini is unreachable.", exception);
        }
    }
}
