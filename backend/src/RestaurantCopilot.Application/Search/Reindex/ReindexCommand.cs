using RestaurantCopilot.Application.Abstractions.Messaging;

namespace RestaurantCopilot.Application.Search.Reindex;

/// <summary>Rebuilds the semantic search index (embeddings) from the current menu and reviews.</summary>
public sealed record ReindexCommand : ICommand<ReindexResponse>;

public sealed record ReindexResponse(int MenuItems, int Reviews);
