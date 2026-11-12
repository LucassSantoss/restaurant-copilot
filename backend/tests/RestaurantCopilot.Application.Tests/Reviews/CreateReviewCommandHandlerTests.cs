using Microsoft.Extensions.Logging.Abstractions;
using RestaurantCopilot.Application.Abstractions.Ai;
using RestaurantCopilot.Application.Abstractions.Persistence;
using RestaurantCopilot.Application.Reviews.Classification;
using RestaurantCopilot.Application.Reviews.CreateReview;
using RestaurantCopilot.Application.Tests.TestDoubles;
using RestaurantCopilot.Domain.Common;
using RestaurantCopilot.Domain.Reviews;

namespace RestaurantCopilot.Application.Tests.Reviews;

public sealed class CreateReviewCommandHandlerTests
{
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IMenuItemRepository _menuItems = Substitute.For<IMenuItemRepository>();
    private readonly IReviewRepository _reviews = Substitute.For<IReviewRepository>();
    private readonly IReviewClassifier _classifier = Substitute.For<IReviewClassifier>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateReviewCommandHandler _handler;

    public CreateReviewCommandHandlerTests()
    {
        _handler = new CreateReviewCommandHandler(
            _orders, _menuItems, _reviews, _classifier, _unitOfWork,
            new FixedTimeProvider(TestData.Now), NullLogger<CreateReviewCommandHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_SavesClassifiedReview()
    {
        var order = TestData.Order(1001);
        _orders.GetByNumberAsync(1001, Arg.Any<CancellationToken>()).Returns(order);
        _classifier.ClassifyAsync(Arg.Any<IReadOnlyList<Review>>(), Arg.Any<CancellationToken>())
            .Returns(call => Classify(call.Arg<IReadOnlyList<Review>>()[0], ReviewSentiment.Negative, ReviewTheme.Packaging));

        var result = await _handler.HandleAsync(new CreateReviewCommand(1001, null, 2, "A embalagem vazou."), TestContext.Current.CancellationToken);

        result.Value.OrderNumber.ShouldBe(1001);
        result.Value.CreatedAt.ShouldBe(TestData.Now);
        result.Value.Classification.ShouldNotBeNull();
        result.Value.Classification.Themes.ShouldBe([ReviewTheme.Packaging]);
        _reviews.Received(1).Add(Arg.Is<Review>(review => review.OrderId == order.Id && review.IsClassified));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenClassificationFails_StillSavesPendingReview()
    {
        _orders.GetByNumberAsync(1001, Arg.Any<CancellationToken>()).Returns(TestData.Order(1001));
        _classifier.ClassifyAsync(Arg.Any<IReadOnlyList<Review>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyDictionary<Guid, ReviewClassification>>(LlmErrors.RateLimited));

        var result = await _handler.HandleAsync(new CreateReviewCommand(1001, null, 5, "Ótimo!"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Classification.ShouldBeNull();
        _reviews.Received(1).Add(Arg.Is<Review>(review => !review.IsClassified));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithUnknownOrder_ReturnsNotFound()
    {
        var result = await _handler.HandleAsync(new CreateReviewCommand(9999, null, 5, "Ótimo!"), TestContext.Current.CancellationToken);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
        _reviews.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownMenuItem_ReturnsNotFound()
    {
        _orders.GetByNumberAsync(1001, Arg.Any<CancellationToken>()).Returns(TestData.Order(1001));
        var menuItemId = Guid.NewGuid();

        var result = await _handler.HandleAsync(new CreateReviewCommand(1001, menuItemId, 5, "Ótimo!"), TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("MenuItem.NotFound");
    }

    private static Result<IReadOnlyDictionary<Guid, ReviewClassification>> Classify(Review review, ReviewSentiment sentiment, params ReviewTheme[] themes) =>
        new Dictionary<Guid, ReviewClassification> { [review.Id] = new(sentiment, themes) };
}
