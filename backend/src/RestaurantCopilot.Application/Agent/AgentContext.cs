using RestaurantCopilot.Domain.Restaurants;

namespace RestaurantCopilot.Application.Agent;

/// <summary>What every tool call needs to interpret its arguments (time zone, "today").</summary>
public sealed record AgentContext(Restaurant Restaurant, DateOnly Today);
