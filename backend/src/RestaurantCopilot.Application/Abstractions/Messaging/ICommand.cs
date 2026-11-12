namespace RestaurantCopilot.Application.Abstractions.Messaging;

/// <summary>A request that changes state and returns <typeparamref name="TResponse"/>.</summary>
public interface ICommand<TResponse>;
