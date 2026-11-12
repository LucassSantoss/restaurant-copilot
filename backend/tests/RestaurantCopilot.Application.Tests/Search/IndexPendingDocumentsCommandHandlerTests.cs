using RestaurantCopilot.Application.Abstractions.Ai;
using RestaurantCopilot.Application.Abstractions.Persistence;
using RestaurantCopilot.Application.Abstractions.Search;
using RestaurantCopilot.Application.Chat;
using RestaurantCopilot.Application.Search.IndexPendingDocuments;
using RestaurantCopilot.Application.Tests.TestDoubles;
using RestaurantCopilot.Domain.Common;
using RestaurantCopilot.Domain.MenuItems;

namespace RestaurantCopilot.Application.Tests.Search;

public sealed class IndexPendingDocumentsCommandHandlerTests
{
    private readonly IRestaurantRepository _restaurants = Substitute.For<IRestaurantRepository>();
    private readonly IMenuItemRepository _menuItems = Substitute.For<IMenuItemRepository>();
    private readonly IReviewRepository _reviews = Substitute.For<IReviewRepository>();
    private readonly IEmbeddingService _embeddings = Substitute.For<IEmbeddingService>();
    private readonly IVectorStore _vectorStore = Substitute.For<IVectorStore>();
    private readonly IChatCache _chatCache = Substitute.For<IChatCache>();
    private readonly IndexPendingDocumentsCommandHandler _handler;

    public IndexPendingDocumentsCommandHandlerTests()
    {
        _handler = new IndexPendingDocumentsCommandHandler(_restaurants, _menuItems, _reviews, _embeddings, _vectorStore, _chatCache);
        _embeddings.Dimensions.Returns(3);
        _vectorStore.Dimensions.Returns(3);
        _restaurants.GetAsync(Arg.Any<CancellationToken>()).Returns(TestData.Restaurant());
        _menuItems.ListNotIndexedAsync(Arg.Any<CancellationToken>()).Returns([]);
        _reviews.ListNotIndexedAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        _embeddings.EmbedAsync(Arg.Any<IReadOnlyList<string>>(), EmbeddingPurpose.Document, Arg.Any<CancellationToken>())
            .Returns(call => Vectors(call.Arg<IReadOnlyList<string>>().Count));
    }

    [Fact]
    public async Task HandleAsync_IndexesOnlyPendingMenuItemsAndReviews()
    {
        _menuItems.ListNotIndexedAsync(Arg.Any<CancellationToken>())
            .Returns([MenuItem.Create("Tiramisù", "Café.", MenuCategory.Dessert, Money.Create(29.90m).Value).Value]);
        IReadOnlyList<ReviewDocumentRow> firstBatch = [Review("Vazou tudo."), Review("Chegou frio.")];
        IReadOnlyList<ReviewDocumentRow> nothingLeft = [];
        _reviews.ListNotIndexedAsync(IndexPendingDocumentsCommandHandler.BatchSize, Arg.Any<CancellationToken>())
            .Returns(firstBatch, nothingLeft);
        var upserted = new List<VectorDocument>();
        await _vectorStore.UpsertAsync(Arg.Do<IReadOnlyList<VectorDocument>>(upserted.AddRange), Arg.Any<CancellationToken>());

        var result = await _handler.HandleAsync(new IndexPendingDocumentsCommand(), TestContext.Current.CancellationToken);

        result.Value.ShouldBe(new IndexPendingDocumentsResponse(Indexed: 3, StoppedReason: null));
        upserted.Select(document => document.Source).ShouldBe([DocumentSource.MenuItem, DocumentSource.Review, DocumentSource.Review]);
        upserted[1].Content.ShouldContain("Vazou tudo.");
        await _chatCache.Received(1).InvalidateAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithNothingPending_SpendsNoEmbeddingQuota()
    {
        var result = await _handler.HandleAsync(new IndexPendingDocumentsCommand(), TestContext.Current.CancellationToken);

        result.Value.ShouldBe(new IndexPendingDocumentsResponse(0, null));
        await _embeddings.DidNotReceiveWithAnyArgs().EmbedAsync(default!, default, TestContext.Current.CancellationToken);
        await _chatCache.DidNotReceiveWithAnyArgs().InvalidateAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_WhenEmbeddingFails_StopsAndReportsWhy()
    {
        _reviews.ListNotIndexedAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([Review("Ótimo!")]);
        _embeddings.EmbedAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<EmbeddingPurpose>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<float[]>>(LlmErrors.RateLimited));

        var result = await _handler.HandleAsync(new IndexPendingDocumentsCommand(), TestContext.Current.CancellationToken);

        result.Value.ShouldBe(new IndexPendingDocumentsResponse(0, LlmErrors.RateLimited.Message));
        await _vectorStore.DidNotReceiveWithAnyArgs().UpsertAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_WhenIndexDoesNotPersist_StopsInsteadOfLooping()
    {
        _reviews.ListNotIndexedAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([Review("Sempre pendente.")]);

        var result = await _handler.HandleAsync(new IndexPendingDocumentsCommand(), TestContext.Current.CancellationToken);

        result.Value.Indexed.ShouldBe(1);
        result.Value.StoppedReason.ShouldNotBeNull();
        await _embeddings.Received(1).EmbedAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<EmbeddingPurpose>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenDimensionsDiffer_FailsBeforeSpendingQuota()
    {
        _vectorStore.Dimensions.Returns(768);

        var result = await _handler.HandleAsync(new IndexPendingDocumentsCommand(), TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("Search.DimensionMismatch");
        await _embeddings.DidNotReceiveWithAnyArgs().EmbedAsync(default!, default, TestContext.Current.CancellationToken);
    }

    private static ReviewDocumentRow Review(string comment) => new(Guid.NewGuid(), 2, comment, TestData.Now, "Lasanha");

    private static Result<IReadOnlyList<float[]>> Vectors(int count) =>
        Enumerable.Range(0, count).Select(_ => new float[] { 1, 0, 0 }).ToList();
}
