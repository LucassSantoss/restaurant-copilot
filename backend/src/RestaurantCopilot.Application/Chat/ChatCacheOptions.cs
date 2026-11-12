namespace RestaurantCopilot.Application.Chat;

/// <summary>Chat answer caching, bound from the "ChatCache" configuration section.</summary>
public sealed class ChatCacheOptions
{
    public const string SectionName = "ChatCache";

    public bool Enabled { get; init; } = true;

    public TimeSpan TimeToLive { get; init; } = TimeSpan.FromHours(1);
}
