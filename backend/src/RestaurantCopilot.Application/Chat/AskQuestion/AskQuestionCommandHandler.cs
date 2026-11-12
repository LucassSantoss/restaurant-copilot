using RestaurantCopilot.Application.Abstractions.Messaging;
using RestaurantCopilot.Application.Agent;
using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Application.Chat.AskQuestion;

internal sealed class AskQuestionCommandHandler(AgentOrchestrator orchestrator) : ICommandHandler<AskQuestionCommand, ChatResponse>
{
    public async Task<Result<ChatResponse>> HandleAsync(AskQuestionCommand command, CancellationToken cancellationToken)
    {
        var answer = await orchestrator.AnswerAsync(command.Question.Trim(), cancellationToken);
        return answer.IsSuccess ? ChatResponse.FromAnswer(answer.Value) : answer.Error;
    }
}
