using RestaurantCopilot.Application.Abstractions.Messaging;

namespace RestaurantCopilot.Application.Chat.AskQuestion;

/// <param name="BypassCache">Skip the cached answer (the fresh answer is still cached).</param>
public sealed record AskQuestionCommand(string Question, bool BypassCache = false) : ICommand<ChatResponse>
{
    public const int QuestionMaxLength = 1000;
}
