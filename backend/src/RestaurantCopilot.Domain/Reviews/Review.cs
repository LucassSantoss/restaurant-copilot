using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Domain.Reviews;

/// <summary>A customer review of an order, optionally about a specific dish.</summary>
public sealed class Review : Entity
{
    public const int CommentMaxLength = 2000;

    private Review(Guid id, Guid orderId, Guid? menuItemId, Rating rating, string comment, DateTimeOffset createdAt)
        : base(id)
    {
        OrderId = orderId;
        MenuItemId = menuItemId;
        Rating = rating;
        Comment = comment;
        CreatedAt = createdAt;
    }

    public Guid OrderId { get; private set; }

    public Guid? MenuItemId { get; private set; }

    public Rating Rating { get; private set; }

    public string Comment { get; private set; }

    /// <summary>When the review was written, always stored in UTC.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<Review> Create(
        Guid orderId,
        Guid? menuItemId,
        Rating rating,
        string comment,
        DateTimeOffset createdAt)
    {
        if (orderId == Guid.Empty)
        {
            return Error.Validation("Review.Order", "A review must reference an order.");
        }

        if (menuItemId == Guid.Empty)
        {
            return Error.Validation("Review.MenuItem", "Menu item reference cannot be empty.");
        }

        if (Guard.Required(comment, CommentMaxLength, "Review.Comment") is { } error)
        {
            return error;
        }

        return new Review(Guid.CreateVersion7(), orderId, menuItemId, rating, comment.Trim(), createdAt.ToUniversalTime());
    }
}
