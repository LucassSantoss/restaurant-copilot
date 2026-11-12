using RestaurantCopilot.Api.Http;
using RestaurantCopilot.Application.Abstractions.Messaging;
using RestaurantCopilot.Application.Abstractions.Search;
using RestaurantCopilot.Application.Search.SemanticSearch;

namespace RestaurantCopilot.Api.Endpoints;

internal static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/search", SearchAsync)
            .WithTags("Search")
            .WithSummary("Semantic search over the menu and reviews (the retrieval step of the RAG).")
            .WithDescription("Results are ordered by cosine similarity. Requires a prior POST /api/ingestion/reindex.")
            .Produces<IReadOnlyList<SearchResultResponse>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return app;
    }

    private static async Task<IResult> SearchAsync(
        string query,
        IQueryHandler<SemanticSearchQuery, IReadOnlyList<SearchResultResponse>> handler,
        CancellationToken cancellationToken,
        int limit = SemanticSearchQuery.DefaultLimit,
        DocumentSource? source = null)
    {
        var result = await handler.HandleAsync(new SemanticSearchQuery(query, limit, source), cancellationToken);
        return result.ToHttpResult();
    }
}
