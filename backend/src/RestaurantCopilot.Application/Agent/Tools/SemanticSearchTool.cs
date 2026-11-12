using System.ComponentModel;
using RestaurantCopilot.Application.Abstractions.Messaging;
using RestaurantCopilot.Application.Abstractions.Search;
using RestaurantCopilot.Application.Search.SemanticSearch;
using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Application.Agent.Tools;

internal sealed class SemanticSearchTool(IQueryHandler<SemanticSearchQuery, IReadOnlyList<SearchResultResponse>> search)
    : AgentTool<SemanticSearchTool.Arguments>
{
    public override string Name => "semantic_search";

    public override string Description =>
        "Semantic (meaning-based) search over the menu and customer reviews. Use it for open questions about what "
        + "customers say or about dishes, when exact filters are not enough (e.g. 'comida fria', 'porção pequena').";

    protected override async Task<Result<AgentToolResult>> ExecuteAsync(
        Arguments arguments,
        AgentContext context,
        CancellationToken cancellationToken)
    {
        var result = await search.HandleAsync(new SemanticSearchQuery(arguments.Query, arguments.Limit, arguments.Source), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error;
        }

        var sources = result.Value.Select(document => AgentSource.Document(document.SourceId, document.Content)).ToList();
        return new AgentToolResult(new { Results = result.Value }, sources);
    }

    internal sealed record Arguments(
        [property: Description("What to look for, in Portuguese.")] string Query,
        [property: Description("Optional: restrict to menu_item or review documents.")] DocumentSource? Source = null,
        [property: Description("How many documents to return (1-20).")] int Limit = SemanticSearchQuery.DefaultLimit);
}
