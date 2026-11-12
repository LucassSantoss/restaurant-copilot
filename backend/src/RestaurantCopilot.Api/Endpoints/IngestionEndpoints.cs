using RestaurantCopilot.Api.Http;
using RestaurantCopilot.Application.Abstractions.Messaging;
using RestaurantCopilot.Application.Reviews.ClassifyPendingReviews;

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

        return app;
    }

    private static async Task<IResult> ClassifyReviewsAsync(
        ICommandHandler<ClassifyPendingReviewsCommand, ClassifyPendingReviewsResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ClassifyPendingReviewsCommand(), cancellationToken);
        return result.ToHttpResult();
    }
}
