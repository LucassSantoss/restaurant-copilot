using RestaurantCopilot.Application.Abstractions.Search;
using RestaurantCopilot.Infrastructure.Persistence.Repositories;
using RestaurantCopilot.Infrastructure.Search;
using RestaurantCopilot.Infrastructure.Tests.Persistence;

namespace RestaurantCopilot.Infrastructure.Tests.Search;

[Collection(PostgresTestCollectionDefinition.Name)]
public sealed class PgvectorVectorStoreTests(PostgresFixture fixture)
{
    [Fact]
    public async Task SearchAsync_ReturnsNearestDocumentsByCosineSimilarity()
    {
        await using var dbContext = fixture.CreateDbContext();
        var store = new PgvectorVectorStore(dbContext, TimeProvider.System);
        var packaging = Document(DocumentSource.Review, "Embalagem vazou.", axis: 0);
        var taste = Document(DocumentSource.Review, "Sabor incrível.", axis: 1);
        var menu = Document(DocumentSource.MenuItem, "Lasanha.", axis: 2);
        await store.ReplaceAllAsync([packaging, taste, menu], TestContext.Current.CancellationToken);

        // Mostly along axis 0, a little along axis 1.
        var query = Vector((0, 0.9f), (1, 0.3f));
        var results = await store.SearchAsync(query, limit: 2, source: null, TestContext.Current.CancellationToken);

        results.Select(result => result.Content).ShouldBe(["Embalagem vazou.", "Sabor incrível."]);
        results[0].Similarity.ShouldBeGreaterThan(results[1].Similarity);
        results[0].SourceId.ShouldBe(packaging.SourceId);
    }

    [Fact]
    public async Task SearchAsync_WithSource_FiltersDocuments()
    {
        await using var dbContext = fixture.CreateDbContext();
        var store = new PgvectorVectorStore(dbContext, TimeProvider.System);
        await store.ReplaceAllAsync(
            [Document(DocumentSource.Review, "Avaliação.", axis: 0), Document(DocumentSource.MenuItem, "Prato.", axis: 0)],
            TestContext.Current.CancellationToken);

        var results = await store.SearchAsync(Vector((0, 1f)), limit: 5, DocumentSource.MenuItem, TestContext.Current.CancellationToken);

        results.ShouldHaveSingleItem().Content.ShouldBe("Prato.");
    }

    [Fact]
    public async Task ReplaceAllAsync_RemovesPreviousDocuments()
    {
        await using var dbContext = fixture.CreateDbContext();
        var store = new PgvectorVectorStore(dbContext, TimeProvider.System);
        await store.ReplaceAllAsync([Document(DocumentSource.Review, "Antigo.", axis: 0)], TestContext.Current.CancellationToken);

        await store.ReplaceAllAsync([Document(DocumentSource.Review, "Novo.", axis: 0)], TestContext.Current.CancellationToken);
        var results = await store.SearchAsync(Vector((0, 1f)), limit: 5, source: null, TestContext.Current.CancellationToken);

        results.ShouldHaveSingleItem().Content.ShouldBe("Novo.");
    }

    [Fact]
    public async Task UpsertAsync_ReplacesTheDocumentOfTheSameSourceAndKeepsOthers()
    {
        await using var dbContext = fixture.CreateDbContext();
        var store = new PgvectorVectorStore(dbContext, TimeProvider.System);
        var kept = Document(DocumentSource.Review, "Outra avaliação.", axis: 1);
        var original = Document(DocumentSource.Review, "Versão antiga.", axis: 0);
        await store.ReplaceAllAsync([kept, original], TestContext.Current.CancellationToken);

        await store.UpsertAsync([original with { Content = "Versão nova." }], TestContext.Current.CancellationToken);
        var results = await store.SearchAsync(Vector((0, 1f), (1, 1f)), limit: 5, source: null, TestContext.Current.CancellationToken);

        results.Select(result => result.Content).ShouldBe(["Versão nova.", "Outra avaliação."], ignoreOrder: true);
    }

    [Fact]
    public async Task NotIndexedQueries_ReturnOnlyItemsWithoutSearchDocument()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = fixture.CreateDbContext();
        var reviews = new ReviewRepository(dbContext);
        var menu = new MenuItemRepository(dbContext);
        var allReviews = await reviews.ListForIndexingAsync(cancellationToken);
        var anyMenuItem = (await menu.ListAsync(null, false, cancellationToken))[0];
        await new PgvectorVectorStore(dbContext, TimeProvider.System).ReplaceAllAsync(
            [
                new VectorDocument(DocumentSource.Review, allReviews[0].Id, "x", Vector((0, 1f))),
                new VectorDocument(DocumentSource.MenuItem, anyMenuItem.Id, "y", Vector((0, 1f))),
            ],
            cancellationToken);

        var pendingReviews = await reviews.ListNotIndexedAsync(limit: 1000, cancellationToken);
        var pendingMenu = await menu.ListNotIndexedAsync(cancellationToken);

        pendingReviews.Count.ShouldBe(allReviews.Count - 1);
        pendingReviews.ShouldNotContain(review => review.Id == allReviews[0].Id);
        pendingMenu.Count.ShouldBe(23);
        pendingMenu.ShouldNotContain(item => item.Id == anyMenuItem.Id);
    }

    private static VectorDocument Document(DocumentSource source, string content, int axis) =>
        new(source, Guid.NewGuid(), content, Vector((axis, 1f)));

    private static float[] Vector(params (int Index, float Value)[] components)
    {
        var vector = new float[SearchDocument.Dimensions];
        foreach (var (index, value) in components)
        {
            vector[index] = value;
        }

        return vector;
    }
}
