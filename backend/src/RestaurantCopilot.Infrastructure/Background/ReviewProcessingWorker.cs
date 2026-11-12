using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestaurantCopilot.Application.Abstractions.Messaging;
using RestaurantCopilot.Application.Reviews.ClassifyPendingReviews;
using RestaurantCopilot.Application.Search.IndexPendingDocuments;

namespace RestaurantCopilot.Infrastructure.Background;

/// <summary>
/// Keeps reviews classified and searchable without anyone calling the ingestion endpoints. Pending work
/// lives in the database (unclassified reviews, items without a search document), so nothing is lost if
/// the API restarts or the LLM is down: the next run simply picks it up. Failures back off exponentially
/// to avoid hammering a rate-limited provider.
/// </summary>
internal sealed class ReviewProcessingWorker(
    IServiceScopeFactory scopeFactory,
    ReviewProcessingTrigger trigger,
    IOptions<ReviewProcessingOptions> options,
    ILogger<ReviewProcessingWorker> logger) : BackgroundService
{
    private readonly ReviewProcessingOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var failures = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            var succeeded = await RunOnceAsync(stoppingToken);
            failures = succeeded ? 0 : failures + 1;

            try
            {
                await trigger.WaitAsync(NextDelay(failures, _options.PollInterval, _options.MaxBackoff), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    /// <summary>The poll interval, doubled for each consecutive failure and capped at the max backoff.</summary>
    internal static TimeSpan NextDelay(int consecutiveFailures, TimeSpan pollInterval, TimeSpan maxBackoff)
    {
        var factor = Math.Pow(2, Math.Min(consecutiveFailures, 16));
        var delay = TimeSpan.FromTicks((long)Math.Min(pollInterval.Ticks * factor, maxBackoff.Ticks));
        return delay < pollInterval ? pollInterval : delay;
    }

    private async Task<bool> RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var services = scope.ServiceProvider;

            var classified = true;
            if (_options.ClassifyReviews)
            {
                var classification = await services
                    .GetRequiredService<ICommandHandler<ClassifyPendingReviewsCommand, ClassifyPendingReviewsResponse>>()
                    .HandleAsync(new ClassifyPendingReviewsCommand(), cancellationToken);
                classified = Report("classified", classification.IsSuccess ? classification.Value.Classified : 0,
                    classification.IsSuccess ? classification.Value.StoppedReason : classification.Error.Message);
            }

            var indexing = await services
                .GetRequiredService<ICommandHandler<IndexPendingDocumentsCommand, IndexPendingDocumentsResponse>>()
                .HandleAsync(new IndexPendingDocumentsCommand(), cancellationToken);
            var indexed = Report("indexed", indexing.IsSuccess ? indexing.Value.Indexed : 0,
                indexing.IsSuccess ? indexing.Value.StoppedReason : indexing.Error.Message);

            return classified && indexed;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // A broken run (e.g. database restarting) must not kill the worker; it retries with backoff.
            logger.LogError(exception, "Review processing failed");
            return false;
        }
    }

    private bool Report(string action, int count, string? failure)
    {
        if (count > 0)
        {
            logger.LogInformation("Review processing {Action} {Count} item(s)", action, count);
        }

        if (failure is not null)
        {
            logger.LogWarning("Review processing stopped before finishing ({Action}): {Reason}", action, failure);
        }

        return failure is null;
    }
}
