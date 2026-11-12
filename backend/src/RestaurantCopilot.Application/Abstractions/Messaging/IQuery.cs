namespace RestaurantCopilot.Application.Abstractions.Messaging;

/// <summary>A read-only request that returns <typeparamref name="TResponse"/> and never changes state.</summary>
public interface IQuery<TResponse>;
