using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantCopilot.Domain.MenuItems;
using RestaurantCopilot.Domain.Orders;
using RestaurantCopilot.Domain.Reviews;

namespace RestaurantCopilot.Infrastructure.Persistence.Configurations;

internal sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(review => review.Id);
        builder.Property(review => review.Rating).HasConversion(ValueConverters.Rating);
        builder.Property(review => review.Comment).HasMaxLength(Review.CommentMaxLength);

        builder.Property(review => review.Sentiment).HasConversion<string>().HasMaxLength(20);

        // Themes are a native text[] column: simple to filter in SQL ('Packaging' = ANY(themes)).
        builder.Ignore(review => review.Themes);
        builder.PrimitiveCollection<List<ReviewTheme>>("_themes")
            .HasColumnName("themes")
            .ElementType(element => element.HasConversion<string>());
        builder.Ignore(review => review.IsClassified);

        builder.HasIndex(review => review.CreatedAt);
        builder.HasIndex(review => review.ClassifiedAt);

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(review => review.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<MenuItem>()
            .WithMany()
            .HasForeignKey(review => review.MenuItemId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
