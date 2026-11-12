using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantCopilot.Domain.MenuItems;
using RestaurantCopilot.Domain.Orders;

namespace RestaurantCopilot.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(order => order.Id);
        builder.Property(order => order.Channel).HasConversion<string>().HasMaxLength(20);
        builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(20);
        builder.Ignore(order => order.Total);

        builder.HasIndex(order => order.Number).IsUnique();
        builder.HasIndex(order => order.PlacedAt);

        builder.HasMany(order => order.Items)
            .WithOne()
            .HasForeignKey("OrderId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(order => order.Items).HasField("_items");
    }
}

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.UnitPrice).HasConversion(ValueConverters.Money).HasPrecision(10, 2);
        builder.Ignore(item => item.Total);

        builder.HasOne<MenuItem>()
            .WithMany()
            .HasForeignKey(item => item.MenuItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
