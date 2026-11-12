using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Domain.MenuItems;

public sealed class MenuItem : Entity
{
    public const int NameMaxLength = 120;
    public const int DescriptionMaxLength = 500;

    private MenuItem(Guid id, string name, string description, MenuCategory category, Money price)
        : base(id)
    {
        Name = name;
        Description = description;
        Category = category;
        Price = price;
        IsAvailable = true;
    }

    public string Name { get; private set; }

    public string Description { get; private set; }

    public MenuCategory Category { get; private set; }

    public Money Price { get; private set; }

    public bool IsAvailable { get; private set; }

    public static Result<MenuItem> Create(string name, string description, MenuCategory category, Money price)
    {
        if (Guard.FirstError(
                Guard.Required(name, NameMaxLength, "MenuItem.Name"),
                Guard.MaxLength(description, DescriptionMaxLength, "MenuItem.Description")) is { } error)
        {
            return error;
        }

        if (!Enum.IsDefined(category))
        {
            return Error.Validation("MenuItem.Category", "Unknown menu category.");
        }

        if (price == Money.Zero)
        {
            return Error.Validation("MenuItem.Price", "Price must be greater than zero.");
        }

        return new MenuItem(Guid.CreateVersion7(), name.Trim(), description?.Trim() ?? string.Empty, category, price);
    }

    public void MarkUnavailable() => IsAvailable = false;

    public void MarkAvailable() => IsAvailable = true;
}
