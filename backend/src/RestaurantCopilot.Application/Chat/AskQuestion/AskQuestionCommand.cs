using RestaurantCopilot.Application.Abstractions.Messaging;

namespace RestaurantCopilot.Application.Chat.AskQuestion;

public sealed record AskQuestionCommand(string Question) : ICommand<ChatResponse>
{
    public const int QuestionMaxLength = 1000;
}
