using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Application.Search;

public static class SearchErrors
{
    public static Error DimensionMismatch(int embeddingDimensions, int storeDimensions) => Error.Failure(
        "Search.DimensionMismatch",
        $"The embedding model produces {embeddingDimensions}-dimension vectors but the vector store expects {storeDimensions}. "
        + "Align Gemini:EmbeddingDimensions with the database schema.");
}
