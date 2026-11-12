namespace RestaurantCopilot.Domain.Reviews;

/// <summary>What a review talks about. A review can touch several themes, or none.</summary>
public enum ReviewTheme
{
    /// <summary>Delivery time (atraso).</summary>
    Delay,

    /// <summary>Taste and food quality (sabor).</summary>
    Taste,

    /// <summary>Packaging (embalagem).</summary>
    Packaging,

    /// <summary>Price and portion size (preço).</summary>
    Price,

    /// <summary>Customer service (atendimento).</summary>
    Service,
}
