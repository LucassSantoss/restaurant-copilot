using RestaurantCopilot.Api.Http;
using RestaurantCopilot.Application.Abstractions.Messaging;
using RestaurantCopilot.Application.Reviews;
using RestaurantCopilot.Application.Reviews.CreateReview;
using RestaurantCopilot.Application.Reviews.GetReviewInsights;

namespace RestaurantCopilot.Api.Endpoints;

internal static class ReviewEndpoints
{
    public static IEndpointRouteBuilder MapReviewEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reviews").WithTags("Reviews");

        group.MapPost("/", CreateAsync)
            .WithSummary("Registers a customer review and classifies its sentiment and themes with the LLM.")
            .WithDescription("If the LLM is unavailable the review is still saved, with a null classification, and is classified later by POST /api/ingestion/classify-reviews.")
            .Produces<ReviewResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/insights", GetInsightsAsync)
            .WithSummary("Summarizes review sentiment, themes and most complained dishes for a period.")
            .WithDescription("Dates are inclusive, in the restaurant's time zone. Without dates, covers the last 30 days.")
            .Produces<ReviewInsightsResponse>()
            .ProducesValidationProblem();

        return app;
    }

    private static async Task<IResult> CreateAsync(
        CreateReviewRequest request,
        ICommandHandler<CreateReviewCommand, ReviewResponse> handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateReviewCommand(request.OrderNumber, request.MenuItemId, request.Rating, request.Comment);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToCreatedHttpResult();
    }

    private static async Task<IResult> GetInsightsAsync(
        IQueryHandler<GetReviewInsightsQuery, ReviewInsightsResponse> handler,
        CancellationToken cancellationToken,
        DateOnly? from = null,
        DateOnly? to = null)
    {
        var result = await handler.HandleAsync(new GetReviewInsightsQuery(from, to), cancellationToken);
        return result.ToHttpResult();
    }

    internal sealed record CreateReviewRequest(int OrderNumber, Guid? MenuItemId, int Rating, string Comment);
}
