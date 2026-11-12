using Microsoft.Extensions.DependencyInjection;
using RestaurantCopilot.Application.Abstractions.Messaging;

namespace RestaurantCopilot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Every handler in this assembly is discovered by convention, so adding a use case
        // never requires touching the composition root.
        services.Scan(scan => scan
            .FromAssemblies(typeof(DependencyInjection).Assembly)
            .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }
}
