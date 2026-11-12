using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using RestaurantCopilot.Application.Abstractions.Caching;
using RestaurantCopilot.Application.Chat.AskQuestion;

namespace RestaurantCopilot.Application.Chat;

/// <summary>
/// Caches agent answers. The key combines:
/// <list type="bullet">
/// <item>the normalized question, so "Quanto vendi?" and "  quanto VENDI " share an answer;</item>
/// <item>the restaurant's current date, because "esta semana" means something else tomorrow;</item>
/// <item>a data version, replaced on every write (new review, classification, reindex), which
/// invalidates all answers at once without scanning or deleting keys.</item>
/// </list>
/// </summary>
internal sealed partial class ChatCache(ICacheService cache, IOptions<ChatCacheOptions> options) : IChatCache
{
    private const string KeyPrefix = "chat:v1";
    private const string DataVersionKey = "chat:data-version";

    /// <summary>The version marker outlives any answer, so answers never survive their version.</summary>
    private static readonly TimeSpan DataVersionTimeToLive = TimeSpan.FromDays(30);

    private readonly ChatCacheOptions _options = options.Value;

    public async Task<ChatResponse?> GetAsync(string question, DateOnly today, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return null;
        }

        var version = await cache.GetAsync<DataVersion>(DataVersionKey, cancellationToken);
        return version is null ? null : await cache.GetAsync<ChatResponse>(Key(question, today, version), cancellationToken);
    }

    public async Task SetAsync(string question, DateOnly today, ChatResponse response, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var version = await cache.GetAsync<DataVersion>(DataVersionKey, cancellationToken) ?? await NewVersionAsync(cancellationToken);
        await cache.SetAsync(Key(question, today, version), response, _options.TimeToLive, cancellationToken);
    }

    public Task InvalidateAsync(CancellationToken cancellationToken) => NewVersionAsync(cancellationToken);

    internal static string NormalizeQuestion(string question)
    {
        var lower = question.Trim().ToLower(CultureInfo.GetCultureInfo("pt-BR"));
        return Whitespace().Replace(lower, " ").TrimEnd('?', '!', '.', ' ');
    }

    private static string Key(string question, DateOnly today, DataVersion version)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(NormalizeQuestion(question)));
        return string.Create(CultureInfo.InvariantCulture, $"{KeyPrefix}:{version.Value}:{today:yyyy-MM-dd}:{Convert.ToHexStringLower(hash)}");
    }

    private async Task<DataVersion> NewVersionAsync(CancellationToken cancellationToken)
    {
        var version = new DataVersion(Guid.CreateVersion7().ToString("N"));
        await cache.SetAsync(DataVersionKey, version, DataVersionTimeToLive, cancellationToken);
        return version;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    internal sealed record DataVersion(string Value);
}
