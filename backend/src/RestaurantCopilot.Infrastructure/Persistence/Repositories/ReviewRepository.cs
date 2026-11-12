using Microsoft.EntityFrameworkCore;
using RestaurantCopilot.Application.Abstractions.Persistence;
using RestaurantCopilot.Domain.Reviews;

namespace RestaurantCopilot.Infrastructure.Persistence.Repositories;

internal sealed class ReviewRepository(ApplicationDbContext dbContext) : IReviewRepository
{
    public void Add(Review review) => dbContext.Reviews.Add(review);

    public async Task<IReadOnlyList<Review>> ListUnclassifiedAsync(int limit, CancellationToken cancellationToken) =>
        await dbContext.Reviews
            .Where(review => review.ClassifiedAt == null)
            .OrderBy(review => review.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public Task<int> CountUnclassifiedAsync(CancellationToken cancellationToken) =>
        dbContext.Reviews.CountAsync(review => review.ClassifiedAt == null, cancellationToken);

    public async Task<IReadOnlyList<ReviewDocumentRow>> ListForIndexingAsync(CancellationToken cancellationToken) =>
        await dbContext.Reviews
            .AsNoTracking()
            .OrderBy(review => review.CreatedAt)
            .Select(review => new ReviewDocumentRow(
                review.Id,
                review.Rating.Value,
                review.Comment,
                review.CreatedAt,
                dbContext.MenuItems.Where(item => item.Id == review.MenuItemId).Select(item => item.Name).FirstOrDefault()))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ReviewInsightRow>> ListForInsightsAsync(
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.Reviews
            .AsNoTracking()
            .Where(review => review.CreatedAt >= startUtc && review.CreatedAt < endUtc)
            .GroupJoin(
                dbContext.MenuItems,
                review => review.MenuItemId,
                item => item.Id,
                (review, items) => new { review, items })
            .SelectMany(
                joined => joined.items.DefaultIfEmpty(),
                (joined, item) => new { joined.review, MenuItemName = item == null ? null : item.Name })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new ReviewInsightRow(
                row.review.Rating.Value,
                row.review.Sentiment,
                row.review.Themes,
                row.review.MenuItemId,
                row.MenuItemName))
            .ToList();
    }
}
