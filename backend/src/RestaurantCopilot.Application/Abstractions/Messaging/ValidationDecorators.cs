using FluentValidation;
using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Application.Abstractions.Messaging;

/// <summary>
/// Runs every FluentValidation validator registered for a request before its handler, so
/// handlers only ever see valid input. Failures become a <see cref="ValidationError"/>.
/// </summary>
internal static class RequestValidation
{
    public static async Task<ValidationError?> ValidateAsync<TRequest>(
        TRequest request,
        IEnumerable<IValidator<TRequest>> validators,
        CancellationToken cancellationToken)
    {
        var failures = new List<FluentValidation.Results.ValidationFailure>();
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken);
            failures.AddRange(result.Errors);
        }

        return failures.Count == 0
            ? null
            : new ValidationError(failures
                .GroupBy(failure => failure.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).ToArray()));
    }
}

internal sealed class ValidationQueryHandlerDecorator<TQuery, TResponse>(
    IQueryHandler<TQuery, TResponse> inner,
    IEnumerable<IValidator<TQuery>> validators) : IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    public async Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken) =>
        await RequestValidation.ValidateAsync(query, validators, cancellationToken) is { } error
            ? error
            : await inner.HandleAsync(query, cancellationToken);
}

internal sealed class ValidationCommandHandlerDecorator<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> inner,
    IEnumerable<IValidator<TCommand>> validators) : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    public async Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken) =>
        await RequestValidation.ValidateAsync(command, validators, cancellationToken) is { } error
            ? error
            : await inner.HandleAsync(command, cancellationToken);
}
