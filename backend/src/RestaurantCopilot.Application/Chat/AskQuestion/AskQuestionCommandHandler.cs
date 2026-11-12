using RestaurantCopilot.Application.Abstractions.Messaging;
using RestaurantCopilot.Application.Abstractions.Persistence;
using RestaurantCopilot.Application.Agent;
using RestaurantCopilot.Application.Common;
using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Application.Chat.AskQuestion;

/// <summary>Answers from the cache when possible; otherwise runs the agent and caches its answer.</summary>
internal sealed class AskQuestionCommandHandler(
    IAgentOrchestrator orchestrator,
    IChatCache cache,
    IRestaurantRepository restaurants,
    TimeProvider timeProvider) : ICommandHandler<AskQuestionCommand, ChatResponse>
{
    public async Task<Result<ChatResponse>> HandleAsync(AskQuestionCommand command, CancellationToken cancellationToken)
    {
        var restaurant = await restaurants.GetAsync(cancellationToken);
        if (restaurant is null)
        {
            return ApplicationErrors.RestaurantNotConfigured;
        }

        var question = command.Question.Trim();
        var today = ReportingPeriod.Today(restaurant, timeProvider);

        if (!command.BypassCache && await cache.GetAsync(question, today, cancellationToken) is { } cached)
        {
            return cached with { Cached = true };
        }

        var answer = await orchestrator.AnswerAsync(question, cancellationToken);
        if (answer.IsFailure)
        {
            return answer.Error;
        }

        var response = ChatResponse.FromAnswer(answer.Value);
        await cache.SetAsync(question, today, response, cancellationToken);
        return response;
    }
}
