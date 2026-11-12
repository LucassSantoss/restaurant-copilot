using Microsoft.Extensions.Options;
using RestaurantCopilot.Application.Chat;
using RestaurantCopilot.Application.Chat.AskQuestion;
using RestaurantCopilot.Application.Tests.TestDoubles;

namespace RestaurantCopilot.Application.Tests.Chat;

public sealed class ChatCacheTests
{
    private static readonly DateOnly Today = new(2026, 11, 11);
    private static readonly ChatResponse Response = new("R$ 100", [], [], new ChatUsage(10, 5));

    private readonly InMemoryCacheService _store = new();

    [Theory]
    [InlineData("Quanto vendi?", "quanto vendi")]
    [InlineData("  QUANTO   vendi\n hoje?! ", "quanto vendi hoje")]
    [InlineData("Ótima SEMANA.", "ótima semana")]
    public void NormalizeQuestion_IgnoresCaseSpacingAndTrailingPunctuation(string question, string expected)
    {
        ChatCache.NormalizeQuestion(question).ShouldBe(expected);
    }

    [Fact]
    public async Task GetAsync_ReturnsAnswerCachedForAnEquivalentQuestion()
    {
        var cache = CreateCache();
        await cache.SetAsync("Quanto vendi?", Today, Response, TestContext.Current.CancellationToken);

        var cached = await cache.GetAsync("  quanto VENDI ", Today, TestContext.Current.CancellationToken);

        cached.ShouldNotBeNull();
        cached.Answer.ShouldBe("R$ 100");
    }

    [Fact]
    public async Task GetAsync_OnAnotherDay_Misses()
    {
        var cache = CreateCache();
        await cache.SetAsync("Quanto vendi esta semana?", Today, Response, TestContext.Current.CancellationToken);

        (await cache.GetAsync("Quanto vendi esta semana?", Today.AddDays(1), TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task InvalidateAsync_MakesEveryCachedAnswerStale()
    {
        var cache = CreateCache();
        await cache.SetAsync("Quanto vendi?", Today, Response, TestContext.Current.CancellationToken);

        await cache.InvalidateAsync(TestContext.Current.CancellationToken);

        (await cache.GetAsync("Quanto vendi?", Today, TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task WhenDisabled_NeverStoresOrReturnsAnswers()
    {
        var cache = CreateCache(enabled: false);

        await cache.SetAsync("Quanto vendi?", Today, Response, TestContext.Current.CancellationToken);

        (await cache.GetAsync("Quanto vendi?", Today, TestContext.Current.CancellationToken)).ShouldBeNull();
        _store.Keys.ShouldBeEmpty();
    }

    private ChatCache CreateCache(bool enabled = true) =>
        new(_store, Options.Create(new ChatCacheOptions { Enabled = enabled }));
}
