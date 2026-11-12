using FluentValidation;

namespace RestaurantCopilot.Application.Search.SemanticSearch;

internal sealed class SemanticSearchQueryValidator : AbstractValidator<SemanticSearchQuery>
{
    public SemanticSearchQueryValidator()
    {
        RuleFor(query => query.Query).NotEmpty().MaximumLength(SemanticSearchQuery.QueryMaxLength);
        RuleFor(query => query.Limit).InclusiveBetween(1, SemanticSearchQuery.MaxLimit);
        RuleFor(query => query.Source).IsInEnum();
    }
}
