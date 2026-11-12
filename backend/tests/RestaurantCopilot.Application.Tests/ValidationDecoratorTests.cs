using Microsoft.Extensions.DependencyInjection;
using RestaurantCopilot.Application.Abstractions.Ai;
using RestaurantCopilot.Application.Abstractions.Messaging;
using RestaurantCopilot.Application.Abstractions.Persistence;
using RestaurantCopilot.Application.Reviews;
using RestaurantCopilot.Application.Reviews.CreateReview;
using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Application.Tests;

/// <summary>Verifies the real DI wiring: handlers resolved from the container are wrapped by validation.</summary>
public sealed class ValidationDecoratorTests
{
    [Fact]
    public async Task ResolvedHandler_RejectsInvalidCommandBeforeTouchingDependencies()
    {
        var orders = Substitute.For<IOrderRepository>();
        var services = new ServiceCollection()
            .AddLogging()
            .AddApplication()
            .AddSingleton(orders)
            .AddSingleton(Substitute.For<IMenuItemRepository>())
            .AddSingleton(Substitute.For<IReviewRepository>())
            .AddSingleton(Substitute.For<IUnitOfWork>())
            .AddSingleton(Substitute.For<ILlmClient>());
        await using var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<ICommandHandler<CreateReviewCommand, ReviewResponse>>();

        var result = await handler.HandleAsync(new CreateReviewCommand(1001, null, 9, "Ótimo"), TestContext.Current.CancellationToken);

        var error = result.Error.ShouldBeOfType<ValidationError>();
        error.Errors.Keys.ShouldBe(["Rating"]);
        await orders.DidNotReceiveWithAnyArgs().GetByNumberAsync(default, TestContext.Current.CancellationToken);
    }
}
