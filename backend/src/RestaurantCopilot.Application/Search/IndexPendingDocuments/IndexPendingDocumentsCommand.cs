using RestaurantCopilot.Application.Abstractions.Messaging;

namespace RestaurantCopilot.Application.Search.IndexPendingDocuments;

/// <summary>
/// Incremental indexing: embeds only the menu items and reviews that have no search document yet
/// (e.g. reviews created after the last reindex), so new data becomes searchable without a full reindex.
/// </summary>
public sealed record IndexPendingDocumentsCommand : ICommand<IndexPendingDocumentsResponse>;

/// <param name="Indexed">Documents added by this run.</param>
/// <param name="StoppedReason">Why the run stopped early (e.g. rate limit), or null when nothing is left.</param>
public sealed record IndexPendingDocumentsResponse(int Indexed, string? StoppedReason);
