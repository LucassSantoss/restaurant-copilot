using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RestaurantCopilot.Application.Abstractions.Messaging;
using RestaurantCopilot.Application.Reviews.Classification;

namespace RestaurantCopilot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // Every handler in this assembly is discovered by convention, so adding a use case
        // never requires touching the composition root.
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableToAny(typeof(IQueryHandler<,>), typeof(ICommandHandler<,>)), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        services.Decorate(typeof(IQueryHandler<,>), typeof(ValidationQueryHandlerDecorator<,>));
        services.Decorate(typeof(ICommandHandler<,>), typeof(ValidationCommandHandlerDecorator<,>));

        services.AddScoped<IReviewClassifier, LlmReviewClassifier>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
