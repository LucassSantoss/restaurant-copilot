using RestaurantCopilot.Domain.Common;
using RestaurantCopilot.Domain.MenuItems;

namespace RestaurantCopilot.Domain.Tests.MenuItems;

public sealed class MenuItemTests
{
    private static readonly Money Price = Money.Create(49.90m).Value;

    [Fact]
    public void Create_WithValidData_IsAvailableAndTrimmed()
    {
        var item = MenuItem.Create("  Pizza Margherita ", "Tomate e manjericão.", MenuCategory.Pizza, Price).Value;

        item.Name.ShouldBe("Pizza Margherita");
        item.IsAvailable.ShouldBeTrue();
        item.Id.ShouldNotBe(Guid.Empty);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutName_Fails(string name)
    {
        var result = MenuItem.Create(name, "", MenuCategory.Pizza, Price);

        result.Error.Code.ShouldBe("MenuItem.Name.Required");
    }

    [Fact]
    public void Create_WithTooLongName_Fails()
    {
        var result = MenuItem.Create(new string('a', MenuItem.NameMaxLength + 1), "", MenuCategory.Pizza, Price);

        result.Error.Code.ShouldBe("MenuItem.Name.TooLong");
    }

    [Fact]
    public void Create_WithZeroPrice_Fails()
    {
        var result = MenuItem.Create("Água", "", MenuCategory.Drink, Money.Zero);

        result.Error.Code.ShouldBe("MenuItem.Price");
    }

    [Fact]
    public void Create_WithUndefinedCategory_Fails()
    {
        var result = MenuItem.Create("Água", "", (MenuCategory)999, Price);

        result.Error.Code.ShouldBe("MenuItem.Category");
    }

    [Fact]
    public void MarkUnavailable_ThenAvailable_TogglesAvailability()
    {
        var item = MenuItem.Create("Tiramisù", "", MenuCategory.Dessert, Price).Value;

        item.MarkUnavailable();
        item.IsAvailable.ShouldBeFalse();

        item.MarkAvailable();
        item.IsAvailable.ShouldBeTrue();
    }
}
