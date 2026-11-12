using RestaurantCopilot.Application.Abstractions.Messaging;
using RestaurantCopilot.Application.Abstractions.Search;

namespace RestaurantCopilot.Application.Search.SemanticSearch;

/// <summary>Finds menu items and reviews whose meaning is closest to a natural-language query.</summary>
public sealed record SemanticSearchQuery(string Query, int Limit = SemanticSearchQuery.DefaultLimit, DocumentSource? Source = null)
    : IQuery<IReadOnlyList<SearchResultResponse>>
{
    public const int DefaultLimit = 5;
    public const int MaxLimit = 20;
    public const int QueryMaxLength = 500;
}

public sealed record SearchResultResponse(DocumentSource Source, Guid SourceId, string Content, double Similarity);
