using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RestaurantCopilot.Application.Abstractions.Messaging;
using RestaurantCopilot.Application.Agent;
using RestaurantCopilot.Application.Agent.Tools;
using RestaurantCopilot.Application.Reviews.Classification;
using RestaurantCopilot.Application.Sales;

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
        services.AddScoped<SalesAnalytics>();
        services.AddAgent();
        services.AddSingleton(TimeProvider.System);

        return services;
    }

    private static void AddAgent(this IServiceCollection services)
    {
        services.AddOptions<AgentOptions>();
        services.AddScoped<AgentOrchestrator>();
        services.AddScoped<MenuItemResolver>();

        services.Scan(scan => scan
            .FromAssemblies(typeof(DependencyInjection).Assembly)
            .AddClasses(classes => classes.AssignableTo<IAgentTool>(), publicOnly: false)
            .As<IAgentTool>()
            .WithScopedLifetime());
    }
}
