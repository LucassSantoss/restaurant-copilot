using FluentValidation;

namespace RestaurantCopilot.Application.Chat.AskQuestion;

internal sealed class AskQuestionCommandValidator : AbstractValidator<AskQuestionCommand>
{
    public AskQuestionCommandValidator()
    {
        RuleFor(command => command.Question).NotEmpty().MaximumLength(AskQuestionCommand.QuestionMaxLength);
    }
}
