using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantCopilot.Domain.Restaurants;

namespace RestaurantCopilot.Infrastructure.Persistence.Configurations;

internal sealed class RestaurantConfiguration : IEntityTypeConfiguration<Restaurant>
{
    public void Configure(EntityTypeBuilder<Restaurant> builder)
    {
        builder.HasKey(restaurant => restaurant.Id);
        builder.Property(restaurant => restaurant.Name).HasMaxLength(Restaurant.NameMaxLength);
        builder.Property(restaurant => restaurant.Cuisine).HasMaxLength(Restaurant.CuisineMaxLength);
        builder.Property(restaurant => restaurant.City).HasMaxLength(Restaurant.CityMaxLength);
        builder.Property(restaurant => restaurant.TimeZoneId).HasMaxLength(64);
    }
}
