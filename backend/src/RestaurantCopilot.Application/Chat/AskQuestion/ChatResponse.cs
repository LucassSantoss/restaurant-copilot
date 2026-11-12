using RestaurantCopilot.Application.Agent;

namespace RestaurantCopilot.Application.Chat.AskQuestion;

public sealed record ChatResponse(
    string Answer,
    IReadOnlyList<AgentSource> Sources,
    IReadOnlyList<AgentStep> ToolCalls,
    ChatUsage Usage)
{
    public static ChatResponse FromAnswer(AgentAnswer answer) =>
        new(answer.Answer, answer.Sources, answer.Steps, new ChatUsage(answer.Usage.InputTokens, answer.Usage.OutputTokens));
}

public sealed record ChatUsage(int InputTokens, int OutputTokens);
