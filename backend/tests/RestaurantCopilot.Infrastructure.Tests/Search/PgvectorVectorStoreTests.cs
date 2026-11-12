using RestaurantCopilot.Application.Abstractions.Search;
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
