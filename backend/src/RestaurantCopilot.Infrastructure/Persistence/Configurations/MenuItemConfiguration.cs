using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantCopilot.Domain.MenuItems;

namespace RestaurantCopilot.Infrastructure.Persistence.Configurations;

internal sealed class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Name).HasMaxLength(MenuItem.NameMaxLength);
        builder.Property(item => item.Description).HasMaxLength(MenuItem.DescriptionMaxLength);

        // Enums are stored as text: readable in ad-hoc queries and in LLM-generated SQL.
        builder.Property(item => item.Category).HasConversion<string>().HasMaxLength(20);
        builder.Property(item => item.Price).HasConversion(ValueConverters.Money).HasPrecision(10, 2);

        builder.HasIndex(item => item.Name).IsUnique();
    }
}
