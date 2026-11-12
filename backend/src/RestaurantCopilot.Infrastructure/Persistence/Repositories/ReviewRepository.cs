using Microsoft.EntityFrameworkCore;
using RestaurantCopilot.Application.Abstractions.Persistence;
using RestaurantCopilot.Application.Abstractions.Search;
using RestaurantCopilot.Domain.Reviews;
using RestaurantCopilot.Infrastructure.Search;

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

    public async Task<(IReadOnlyList<ReviewListRow> Reviews, int TotalCount)> SearchAsync(
        ReviewSearchFilter filter,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Reviews.AsNoTracking();

        if (filter.StartUtc is { } start)
        {
            query = query.Where(review => review.CreatedAt >= start);
        }

        if (filter.EndUtc is { } end)
        {
            query = query.Where(review => review.CreatedAt < end);
        }

        if (filter.Sentiment is { } sentiment)
        {
            query = query.Where(review => review.Sentiment == sentiment);
        }

        if (filter.Theme is { } theme)
        {
            // Translates to "theme = ANY(themes)" on the text[] column.
            query = query.Where(review => EF.Property<List<ReviewTheme>>(review, "_themes").Contains(theme));
        }

        if (filter.MenuItemId is { } menuItemId)
        {
            query = query.Where(review => review.MenuItemId == menuItemId);
        }

        if (filter.MaxRating is { } maxRating)
        {
            // Rating is a value object behind a converter; filtering by the set of allowed values
            // translates to SQL "rating IN (...)", where a comparison on .Value would not translate.
            var allowed = Enumerable.Range(Rating.Min, Math.Clamp(maxRating, 0, Rating.Max))
                .Select(value => Rating.Create(value).Value)
                .ToList();
            query = query.Where(review => allowed.Contains(review.Rating));
        }

        var total = await query.CountAsync(cancellationToken);
        var page = await query
            .OrderByDescending(review => review.CreatedAt)
            .Take(filter.Limit)
            .Select(review => new
            {
                review,
                OrderNumber = dbContext.Orders.Where(order => order.Id == review.OrderId).Select(order => order.Number).First(),
                MenuItemName = dbContext.MenuItems.Where(item => item.Id == review.MenuItemId).Select(item => item.Name).FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        var rows = page
            .Select(row => new ReviewListRow(
                row.review.Id,
                row.OrderNumber,
                row.review.CreatedAt,
                row.review.Rating.Value,
                row.review.Comment,
                row.MenuItemName,
                row.review.Sentiment,
                row.review.Themes))
            .ToList();
        return (rows, total);
    }

    public async Task<IReadOnlyList<ReviewDocumentRow>> ListNotIndexedAsync(int limit, CancellationToken cancellationToken) =>
        await ProjectForIndexing(dbContext.Reviews
            .AsNoTracking()
            .Where(review => !dbContext.Set<SearchDocument>()
                .Any(document => document.Source == DocumentSource.Review && document.SourceId == review.Id))
            .OrderBy(review => review.CreatedAt)
            .Take(limit))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ReviewDocumentRow>> ListForIndexingAsync(CancellationToken cancellationToken) =>
        await ProjectForIndexing(dbContext.Reviews.AsNoTracking().OrderBy(review => review.CreatedAt))
            .ToListAsync(cancellationToken);

    private IQueryable<ReviewDocumentRow> ProjectForIndexing(IQueryable<Review> reviews) =>
        reviews
            .Select(review => new ReviewDocumentRow(
                review.Id,
                review.Rating.Value,
                review.Comment,
                review.CreatedAt,
                dbContext.MenuItems.Where(item => item.Id == review.MenuItemId).Select(item => item.Name).FirstOrDefault()));

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
