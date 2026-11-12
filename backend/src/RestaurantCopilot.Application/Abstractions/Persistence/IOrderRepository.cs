using RestaurantCopilot.Domain.Orders;

namespace RestaurantCopilot.Application.Abstractions.Persistence;

public interface IOrderRepository
{
    Task<Order?> GetByNumberAsync(int number, CancellationToken cancellationToken);
}
