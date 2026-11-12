using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Domain.Restaurants;

/// <summary>
/// The restaurant profile. The application serves a single restaurant; its profile gives
/// the agent context (name, cuisine) and the time zone used to interpret dates.
/// </summary>
public sealed class Restaurant : Entity
{
    public const int NameMaxLength = 120;
    public const int CuisineMaxLength = 60;
    public const int CityMaxLength = 80;

    private Restaurant(Guid id, string name, string cuisine, string city, string timeZoneId)
        : base(id)
    {
        Name = name;
        Cuisine = cuisine;
        City = city;
        TimeZoneId = timeZoneId;
    }

    public string Name { get; private set; }

    public string Cuisine { get; private set; }

    public string City { get; private set; }

    /// <summary>IANA time zone (e.g. "America/Sao_Paulo") used for day/week/month boundaries.</summary>
    public string TimeZoneId { get; private set; }

    public static Result<Restaurant> Create(string name, string cuisine, string city, string timeZoneId)
    {
        if (Guard.FirstError(
                Guard.Required(name, NameMaxLength, "Restaurant.Name"),
                Guard.Required(cuisine, CuisineMaxLength, "Restaurant.Cuisine"),
                Guard.Required(city, CityMaxLength, "Restaurant.City")) is { } error)
        {
            return error;
        }

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _))
        {
            return Error.Validation("Restaurant.TimeZone", $"Unknown time zone '{timeZoneId}'.");
        }

        return new Restaurant(Guid.CreateVersion7(), name.Trim(), cuisine.Trim(), city.Trim(), timeZoneId);
    }
}
