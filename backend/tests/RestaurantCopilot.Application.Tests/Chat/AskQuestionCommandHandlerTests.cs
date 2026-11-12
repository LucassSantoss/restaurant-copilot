using RestaurantCopilot.Application.Abstractions.Ai;
using RestaurantCopilot.Application.Abstractions.Persistence;
using RestaurantCopilot.Application.Agent;
using RestaurantCopilot.Application.Chat;
using RestaurantCopilot.Application.Chat.AskQuestion;
using RestaurantCopilot.Application.Tests.TestDoubles;
using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Application.Tests.Chat;

public sealed class AskQuestionCommandHandlerTests
{
    private static readonly DateOnly Today = new(2026, 11, 11);

    private readonly IAgentOrchestrator _orchestrator = Substitute.For<IAgentOrchestrator>();
    private readonly IChatCache _cache = Substitute.For<IChatCache>();
    private readonly IRestaurantRepository _restaurants = Substitute.For<IRestaurantRepository>();
    private readonly AskQuestionCommandHandler _handler;

    public AskQuestionCommandHandlerTests()
    {
        _restaurants.GetAsync(Arg.Any<CancellationToken>()).Returns(TestData.Restaurant());
        _handler = new AskQuestionCommandHandler(_orchestrator, _cache, _restaurants, new FixedTimeProvider(TestData.Now));
    }

    [Fact]
    public async Task HandleAsync_OnCacheHit_SkipsTheAgent()
    {
        _cache.GetAsync("Quanto vendi?", Today, Arg.Any<CancellationToken>())
            .Returns(new ChatResponse("R$ 100", [], [], new ChatUsage(10, 5)));

        var result = await _handler.HandleAsync(new AskQuestionCommand(" Quanto vendi? "), TestContext.Current.CancellationToken);

        result.Value.Cached.ShouldBeTrue();
        await _orchestrator.DidNotReceiveWithAnyArgs().AnswerAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_OnCacheMiss_RunsAgentAndCachesAnswer()
    {
        _orchestrator.AnswerAsync("Quanto vendi?", Arg.Any<CancellationToken>())
            .Returns(new AgentAnswer("R$ 100", [], [], new LlmUsage(10, 5)));

        var result = await _handler.HandleAsync(new AskQuestionCommand("Quanto vendi?"), TestContext.Current.CancellationToken);

        result.Value.Cached.ShouldBeFalse();
        await _cache.Received(1).SetAsync("Quanto vendi?", Today, result.Value, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenBypassingCache_RunsAgentEvenIfCached()
    {
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new ChatResponse("velho", [], [], new ChatUsage(0, 0)));
        _orchestrator.AnswerAsync("Quanto vendi?", Arg.Any<CancellationToken>())
            .Returns(new AgentAnswer("novo", [], [], new LlmUsage(10, 5)));

        var result = await _handler.HandleAsync(new AskQuestionCommand("Quanto vendi?", BypassCache: true), TestContext.Current.CancellationToken);

        result.Value.Answer.ShouldBe("novo");
        await _cache.Received(1).SetAsync("Quanto vendi?", Today, result.Value, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenAgentFails_DoesNotCacheTheError()
    {
        _orchestrator.AnswerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<AgentAnswer>(LlmErrors.RateLimited));

        var result = await _handler.HandleAsync(new AskQuestionCommand("Quanto vendi?"), TestContext.Current.CancellationToken);

        result.Error.ShouldBe(LlmErrors.RateLimited);
        await _cache.DidNotReceiveWithAnyArgs().SetAsync(default!, default, default!, TestContext.Current.CancellationToken);
    }
}
