using Microsoft.EntityFrameworkCore;
using RestaurantCopilot.Domain.Reviews;
using RestaurantCopilot.Infrastructure.Persistence.Repositories;

namespace RestaurantCopilot.Infrastructure.Tests.Persistence;

[Collection(PostgresTestCollectionDefinition.Name)]
public sealed class ReviewRepositoryTests(PostgresFixture fixture)
{
    // The whole seed range, as UTC instants.
    private static readonly DateTimeOffset SeedStart = new(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset SeedEnd = new(2026, 12, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ListForInsightsAsync_ReturnsReviewsInRangeWithDishNames()
    {
        await using var dbContext = fixture.CreateDbContext();
        var repository = new ReviewRepository(dbContext);

        var rows = await repository.ListForInsightsAsync(SeedStart, SeedEnd, TestContext.Current.CancellationToken);

        rows.Count.ShouldBe(215);
        rows.ShouldContain(row => row.MenuItemName == "Lasanha à Bolonhesa");
        rows.Where(row => row.MenuItemId is null).ShouldAllBe(row => row.MenuItemName == null);
    }

    [Fact]
    public async Task ListForInsightsAsync_ExcludesReviewsOutsideRange()
    {
        await using var dbContext = fixture.CreateDbContext();
        var repository = new ReviewRepository(dbContext);

        var rows = await repository.ListForInsightsAsync(SeedEnd, SeedEnd.AddDays(30), TestContext.Current.CancellationToken);

        rows.ShouldBeEmpty();
    }

    [Fact]
    public async Task Classification_RoundTripsSentimentAndThemesArray()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = fixture.CreateDbContext();
        // Rolled back on dispose, so the shared seed data stays untouched.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var repository = new ReviewRepository(dbContext);
        var pending = await repository.ListUnclassifiedAsync(2, cancellationToken);

        pending[0].Classify(ReviewSentiment.Negative, [ReviewTheme.Packaging, ReviewTheme.Delay], DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();

        var reloaded = await dbContext.Reviews.SingleAsync(review => review.Id == pending[0].Id, cancellationToken);
        reloaded.Sentiment.ShouldBe(ReviewSentiment.Negative);
        reloaded.Themes.ShouldBe([ReviewTheme.Delay, ReviewTheme.Packaging]);
        (await repository.CountUnclassifiedAsync(cancellationToken)).ShouldBe(214);
        var themesInSql = await dbContext.Database
            .SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM reviews WHERE 'Packaging' = ANY(themes)")
            .SingleAsync(cancellationToken);
        themesInSql.ShouldBe(1);
    }
}
