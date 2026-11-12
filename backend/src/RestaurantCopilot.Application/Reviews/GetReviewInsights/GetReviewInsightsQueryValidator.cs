using FluentValidation;

namespace RestaurantCopilot.Application.Reviews.GetReviewInsights;

internal sealed class GetReviewInsightsQueryValidator : AbstractValidator<GetReviewInsightsQuery>
{
    public GetReviewInsightsQueryValidator()
    {
        RuleFor(query => query)
            .Must(query => query.From is null == query.To is null)
            .WithName("period")
            .WithMessage("Provide both 'from' and 'to', or neither.");

        When(query => query.From is not null && query.To is not null, () =>
        {
            RuleFor(query => query.To)
                .GreaterThanOrEqualTo(query => query.From)
                .WithMessage("'to' must be on or after 'from'.");
            RuleFor(query => query)
                .Must(query => query.To!.Value.DayNumber - query.From!.Value.DayNumber < GetReviewInsightsQuery.MaxDays)
                .WithName("period")
                .WithMessage($"The period cannot exceed {GetReviewInsightsQuery.MaxDays} days.");
        });
    }
}
