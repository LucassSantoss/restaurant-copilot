using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Timeout;
using RestaurantCopilot.Application.Abstractions.Ai;
using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Infrastructure.Ai.Gemini;

/// <summary>
/// Shared HTTP plumbing for the Gemini clients: JSON settings and translation of transport
/// failures into <see cref="LlmErrors"/>. Retries already happened in the resilience pipeline
/// (see <see cref="GeminiServiceCollectionExtensions"/>) by the time a failure reaches here.
/// </summary>
internal static class GeminiHttp
{
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static async Task<Result<TResponse>> PostAsync<TRequest, TResponse>(
        HttpClient httpClient,
        string path,
        TRequest body,
        ILogger logger,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(path, body, SerializerOptions, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return await ToErrorAsync(response, logger, cancellationToken);
            }

            return await response.Content.ReadFromJsonAsync<TResponse>(SerializerOptions, cancellationToken) is { } parsed
                ? parsed
                : LlmErrors.InvalidResponse("Gemini returned an empty body.");
        }
        catch (Exception exception) when (IsTransportFailure(exception, cancellationToken))
        {
            logger.LogWarning(exception, "Gemini request to {Path} failed", path);
            return LlmErrors.Unavailable;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Gemini returned a body that could not be parsed");
            return LlmErrors.InvalidResponse("Gemini returned a body that could not be parsed.");
        }
    }

    private static async Task<Error> ToErrorAsync(HttpResponseMessage response, ILogger logger, CancellationToken cancellationToken)
    {
        var detail = await ReadErrorMessageAsync(response, cancellationToken);
        logger.LogWarning("Gemini responded {StatusCode}: {Detail}", (int)response.StatusCode, detail);

        return response.StatusCode switch
        {
            HttpStatusCode.TooManyRequests => LlmErrors.RateLimited,
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => LlmErrors.Unauthorized,
            HttpStatusCode.BadRequest or HttpStatusCode.NotFound => LlmErrors.InvalidRequest(detail),
            _ => LlmErrors.Unavailable,
        };
    }

    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync<GeminiErrorResponse>(SerializerOptions, cancellationToken);
            return body?.Error?.Message ?? response.ReasonPhrase ?? "Unknown error.";
        }
        catch (JsonException)
        {
            return response.ReasonPhrase ?? "Unknown error.";
        }
    }

    // Caller cancellation is not a provider failure: let it propagate.
    private static bool IsTransportFailure(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException or TimeoutRejectedException or BrokenCircuitException
        || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested);
}
