using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Application.Agent;

public interface IAgentOrchestrator
{
    Task<Result<AgentAnswer>> AnswerAsync(string question, CancellationToken cancellationToken);
}
