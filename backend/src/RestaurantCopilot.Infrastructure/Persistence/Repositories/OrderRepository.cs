using Microsoft.EntityFrameworkCore;
using RestaurantCopilot.Application.Abstractions.Persistence;
using RestaurantCopilot.Domain.Orders;

namespace RestaurantCopilot.Infrastructure.Persistence.Repositories;

internal sealed class OrderRepository(ApplicationDbContext dbContext) : IOrderRepository
{
    public Task<Order?> GetByNumberAsync(int number, CancellationToken cancellationToken) =>
        dbContext.Orders
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Number == number, cancellationToken);
}
