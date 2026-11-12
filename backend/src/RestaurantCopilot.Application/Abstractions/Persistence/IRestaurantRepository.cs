using RestaurantCopilot.Domain.Restaurants;

namespace RestaurantCopilot.Application.Abstractions.Persistence;

public interface IRestaurantRepository
{
    /// <summary>The restaurant profile; null only before the database is seeded.</summary>
    Task<Restaurant?> GetAsync(CancellationToken cancellationToken);
}
