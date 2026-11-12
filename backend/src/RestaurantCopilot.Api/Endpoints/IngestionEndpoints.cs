using RestaurantCopilot.Api.Http;
using RestaurantCopilot.Application.Abstractions.Messaging;
using RestaurantCopilot.Application.Reviews.ClassifyPendingReviews;
using RestaurantCopilot.Application.Search.Reindex;

namespace RestaurantCopilot.Api.Endpoints;

internal static class IngestionEndpoints
{
    public static IEndpointRouteBuilder MapIngestionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ingestion").WithTags("Ingestion");

        group.MapPost("/classify-reviews", ClassifyReviewsAsync)
            .WithSummary("Classifies every review that is still pending, in batches.")
            .WithDescription("Safe to call repeatedly: only pending reviews are sent to the LLM. Stops early, reporting why, if the LLM fails (e.g. free-tier rate limit).")
            .Produces<ClassifyPendingReviewsResponse>();

        group.MapPost("/reindex", ReindexAsync)
            .WithSummary("Rebuilds the semantic search index (embeddings of the menu and every review).")
            .WithDescription("The new index replaces the old one atomically, so searches keep working during a reindex.")
            .Produces<ReindexResponse>()
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return app;
    }

    private static async Task<IResult> ClassifyReviewsAsync(
        ICommandHandler<ClassifyPendingReviewsCommand, ClassifyPendingReviewsResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ClassifyPendingReviewsCommand(), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ReindexAsync(
        ICommandHandler<ReindexCommand, ReindexResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ReindexCommand(), cancellationToken);
        return result.ToHttpResult();
    }
}
