using RestaurantCopilot.Application.Abstractions.Persistence;
using RestaurantCopilot.Application.Search;
using RestaurantCopilot.Application.Tests.TestDoubles;
using RestaurantCopilot.Domain.Common;
using RestaurantCopilot.Domain.MenuItems;

namespace RestaurantCopilot.Application.Tests.Search;

public sealed class SearchDocumentBuilderTests
{
    [Fact]
    public void ForMenuItem_DescribesDishInPortuguese()
    {
        var item = MenuItem.Create("Pizza Margherita", "Muçarela de búfala e manjericão.", MenuCategory.Pizza, Money.Create(64.90m).Value).Value;

        var text = SearchDocumentBuilder.ForMenuItem(item);

        text.ShouldBe("Prato do cardápio: Pizza Margherita (pizza). Muçarela de búfala e manjericão. Preço: R$ 64,90.");
    }

    [Fact]
    public void ForMenuItem_FlagsUnavailableItems()
    {
        var item = MenuItem.Create("Cannoli", "Doce.", MenuCategory.Dessert, Money.Create(26.90m).Value).Value;
        item.MarkUnavailable();

        SearchDocumentBuilder.ForMenuItem(item).ShouldContain("(indisponível no momento)");
    }

    [Fact]
    public void ForReview_IncludesRatingLocalDateAndDish()
    {
        // 01:00 UTC on Oct 10 is still Friday, Oct 9 in São Paulo.
        var review = new ReviewDocumentRow(
            Guid.NewGuid(), 2, "Veio vazando.", new DateTimeOffset(2026, 10, 10, 1, 0, 0, TimeSpan.Zero), "Lasanha à Bolonhesa");

        var text = SearchDocumentBuilder.ForReview(review, TestData.Restaurant());

        text.ShouldBe("Avaliação de cliente, nota 2/5, em sexta-feira, 09/10/2026, sobre Lasanha à Bolonhesa: \"Veio vazando.\"");
    }

    [Fact]
    public void ForReview_WithoutDish_RefersToTheOrder()
    {
        var review = new ReviewDocumentRow(Guid.NewGuid(), 5, "Rápido!", TestData.Now, null);

        SearchDocumentBuilder.ForReview(review, TestData.Restaurant()).ShouldContain("sobre o pedido:");
    }
}
