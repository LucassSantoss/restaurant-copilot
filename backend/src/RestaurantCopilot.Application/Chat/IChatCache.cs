using RestaurantCopilot.Application.Chat.AskQuestion;

namespace RestaurantCopilot.Application.Chat;

public interface IChatCache
{
    Task<ChatResponse?> GetAsync(string question, DateOnly today, CancellationToken cancellationToken);

    Task SetAsync(string question, DateOnly today, ChatResponse response, CancellationToken cancellationToken);

    /// <summary>Makes every cached answer stale. Called whenever the data the agent reads changes.</summary>
    Task InvalidateAsync(CancellationToken cancellationToken);
}
