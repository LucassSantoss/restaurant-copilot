using Microsoft.EntityFrameworkCore;
using RestaurantCopilot.Application.Abstractions.Persistence;
using RestaurantCopilot.Domain.Restaurants;

namespace RestaurantCopilot.Infrastructure.Persistence.Repositories;

internal sealed class RestaurantRepository(ApplicationDbContext dbContext) : IRestaurantRepository
{
    public Task<Restaurant?> GetAsync(CancellationToken cancellationToken) =>
        dbContext.Restaurants.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
}
