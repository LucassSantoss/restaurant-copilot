using System.Globalization;
using RestaurantCopilot.Application.Abstractions.Persistence;
using RestaurantCopilot.Domain.MenuItems;
using RestaurantCopilot.Domain.Restaurants;

namespace RestaurantCopilot.Application.Search;

/// <summary>
/// Turns menu items and reviews into the self-contained Portuguese text that is embedded and later
/// handed to the LLM as context. Each document carries what the model needs to cite it correctly
/// (dish, rating, date) because it is read without the surrounding database rows.
/// </summary>
internal static class SearchDocumentBuilder
{
    private static readonly CultureInfo PortugueseBrazil = CultureInfo.GetCultureInfo("pt-BR");

    public static string ForMenuItem(MenuItem item)
    {
        var availability = item.IsAvailable ? string.Empty : " (indisponível no momento)";
        return $"Prato do cardápio: {item.Name} ({CategoryName(item.Category)}){availability}. "
            + $"{item.Description} Preço: {item.Price.Amount.ToString("C", PortugueseBrazil)}.";
    }

    public static string ForReview(ReviewDocumentRow review, Restaurant restaurant)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(restaurant.TimeZoneId);
        var localDate = TimeZoneInfo.ConvertTime(review.CreatedAt, timeZone);
        var subject = review.MenuItemName is null ? "o pedido" : review.MenuItemName;

        return $"Avaliação de cliente, nota {review.Rating}/5, em "
            + $"{localDate.ToString("dddd, dd/MM/yyyy", PortugueseBrazil)}, sobre {subject}: \"{review.Comment}\"";
    }

    private static string CategoryName(MenuCategory category) => category switch
    {
        MenuCategory.Starter => "entrada",
        MenuCategory.Pizza => "pizza",
        MenuCategory.Pasta => "massa",
        MenuCategory.Risotto => "risoto",
        MenuCategory.MainCourse => "prato principal",
        MenuCategory.Dessert => "sobremesa",
        MenuCategory.Drink => "bebida",
        _ => category.ToString(),
    };
}
