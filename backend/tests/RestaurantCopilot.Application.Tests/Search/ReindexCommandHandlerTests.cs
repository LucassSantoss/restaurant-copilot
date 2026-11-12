using RestaurantCopilot.Application.Abstractions.Ai;
using RestaurantCopilot.Application.Abstractions.Persistence;
using RestaurantCopilot.Application.Abstractions.Search;
using RestaurantCopilot.Application.Chat;
using RestaurantCopilot.Application.Search.Reindex;
using RestaurantCopilot.Application.Tests.TestDoubles;
using RestaurantCopilot.Domain.Common;
using RestaurantCopilot.Domain.MenuItems;

namespace RestaurantCopilot.Application.Tests.Search;

public sealed class ReindexCommandHandlerTests
{
    private readonly IRestaurantRepository _restaurants = Substitute.For<IRestaurantRepository>();
    private readonly IMenuItemRepository _menuItems = Substitute.For<IMenuItemRepository>();
    private readonly IReviewRepository _reviews = Substitute.For<IReviewRepository>();
    private readonly IEmbeddingService _embeddings = Substitute.For<IEmbeddingService>();
    private readonly IVectorStore _vectorStore = Substitute.For<IVectorStore>();
    private readonly IChatCache _chatCache = Substitute.For<IChatCache>();
    private readonly ReindexCommandHandler _handler;

    public ReindexCommandHandlerTests()
    {
        _handler = new ReindexCommandHandler(_restaurants, _menuItems, _reviews, _embeddings, _vectorStore, _chatCache);
        _embeddings.Dimensions.Returns(3);
        _vectorStore.Dimensions.Returns(3);
        _restaurants.GetAsync(Arg.Any<CancellationToken>()).Returns(TestData.Restaurant());
        _menuItems.ListAsync(null, false, Arg.Any<CancellationToken>())
            .Returns([MenuItem.Create("Tiramisù", "Café.", MenuCategory.Dessert, Money.Create(29.90m).Value).Value]);
        _reviews.ListForIndexingAsync(Arg.Any<CancellationToken>())
            .Returns([new ReviewDocumentRow(Guid.NewGuid(), 5, "Delícia!", TestData.Now, "Tiramisù")]);
    }

    [Fact]
    public async Task HandleAsync_EmbedsMenuAndReviewsAndReplacesIndex()
    {
        _embeddings.EmbedAsync(Arg.Any<IReadOnlyList<string>>(), EmbeddingPurpose.Document, Arg.Any<CancellationToken>())
            .Returns(call => Vectors(call.Arg<IReadOnlyList<string>>().Count));
        IReadOnlyList<VectorDocument>? indexed = null;
        await _vectorStore.ReplaceAllAsync(Arg.Do<IReadOnlyList<VectorDocument>>(documents => indexed = documents), Arg.Any<CancellationToken>());

        var result = await _handler.HandleAsync(new ReindexCommand(), TestContext.Current.CancellationToken);

        result.Value.ShouldBe(new ReindexResponse(MenuItems: 1, Reviews: 1));
        indexed.ShouldNotBeNull();
        indexed.Select(document => document.Source).ShouldBe([DocumentSource.MenuItem, DocumentSource.Review]);
        indexed[1].Content.ShouldContain("Delícia!");
        await _chatCache.Received(1).InvalidateAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenDimensionsDiffer_FailsBeforeCallingTheEmbeddingModel()
    {
        _vectorStore.Dimensions.Returns(768);

        var result = await _handler.HandleAsync(new ReindexCommand(), TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("Search.DimensionMismatch");
        await _embeddings.DidNotReceiveWithAnyArgs().EmbedAsync(default!, default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_WhenEmbeddingFails_KeepsTheCurrentIndex()
    {
        _embeddings.EmbedAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<EmbeddingPurpose>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<float[]>>(LlmErrors.RateLimited));

        var result = await _handler.HandleAsync(new ReindexCommand(), TestContext.Current.CancellationToken);

        result.Error.ShouldBe(LlmErrors.RateLimited);
        await _vectorStore.DidNotReceiveWithAnyArgs().ReplaceAllAsync(default!, TestContext.Current.CancellationToken);
    }

    private static Result<IReadOnlyList<float[]>> Vectors(int count) =>
        Enumerable.Range(0, count).Select(_ => new float[] { 1, 0, 0 }).ToList();
}
