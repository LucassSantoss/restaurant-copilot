using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RestaurantCopilot.Api.HealthChecks;

/// <summary>
/// Writes the health report as JSON with the status of every dependency, instead of the
/// default plain-text "Healthy"/"Unhealthy", so operators can see which dependency failed.
/// </summary>
internal static class HealthReportResponseWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        var response = new HealthResponse(
            report.Status.ToString(),
            report.TotalDuration.TotalMilliseconds,
            report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new HealthEntryResponse(
                    entry.Value.Status.ToString(),
                    entry.Value.Description ?? entry.Value.Exception?.Message,
                    entry.Value.Duration.TotalMilliseconds)));

        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(response, SerializerOptions);
    }

    private sealed record HealthResponse(
        string Status,
        double DurationMs,
        IReadOnlyDictionary<string, HealthEntryResponse> Checks);

    private sealed record HealthEntryResponse(string Status, string? Description, double DurationMs);
}
