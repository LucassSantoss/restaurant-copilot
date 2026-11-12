using RestaurantCopilot.Application.Abstractions.Ai;
using RestaurantCopilot.Application.Abstractions.Persistence;
using RestaurantCopilot.Application.Chat;
using RestaurantCopilot.Application.Reviews.Classification;
using RestaurantCopilot.Application.Reviews.ClassifyPendingReviews;
using RestaurantCopilot.Application.Tests.TestDoubles;
using RestaurantCopilot.Domain.Common;
using RestaurantCopilot.Domain.Reviews;

namespace RestaurantCopilot.Application.Tests.Reviews;

public sealed class ClassifyPendingReviewsCommandHandlerTests
{
    private readonly IReviewRepository _reviews = Substitute.For<IReviewRepository>();
    private readonly IReviewClassifier _classifier = Substitute.For<IReviewClassifier>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IChatCache _chatCache = Substitute.For<IChatCache>();
    private readonly ClassifyPendingReviewsCommandHandler _handler;

    public ClassifyPendingReviewsCommandHandlerTests()
    {
        _handler = new ClassifyPendingReviewsCommandHandler(_reviews, _classifier, _unitOfWork, _chatCache, new FixedTimeProvider(TestData.Now));
    }

    [Fact]
    public async Task HandleAsync_ClassifiesBatchesUntilNothingIsPending()
    {
        IReadOnlyList<Review> firstBatch = [TestData.Review(), TestData.Review()];
        IReadOnlyList<Review> secondBatch = [TestData.Review()];
        _reviews.ListUnclassifiedAsync(ClassifyPendingReviewsCommandHandler.BatchSize, Arg.Any<CancellationToken>())
            .Returns(firstBatch, secondBatch, []);
        _classifier.ClassifyAsync(Arg.Any<IReadOnlyList<Review>>(), Arg.Any<CancellationToken>())
            .Returns(call => ClassifyAll(call.Arg<IReadOnlyList<Review>>()));

        var result = await _handler.HandleAsync(new ClassifyPendingReviewsCommand(), TestContext.Current.CancellationToken);

        result.Value.ShouldBe(new ClassifyPendingReviewsResponse(Classified: 3, Pending: 0, StoppedReason: null));
        firstBatch.Concat(secondBatch).ShouldAllBe(review => review.IsClassified);
        await _unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _chatCache.Received(1).InvalidateAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenLlmFails_StopsAndReportsWhy()
    {
        _reviews.ListUnclassifiedAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([TestData.Review()]);
        _reviews.CountUnclassifiedAsync(Arg.Any<CancellationToken>()).Returns(1);
        _classifier.ClassifyAsync(Arg.Any<IReadOnlyList<Review>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyDictionary<Guid, ReviewClassification>>(LlmErrors.RateLimited));

        var result = await _handler.HandleAsync(new ClassifyPendingReviewsCommand(), TestContext.Current.CancellationToken);

        result.Value.ShouldBe(new ClassifyPendingReviewsResponse(0, 1, LlmErrors.RateLimited.Message));
        await _chatCache.DidNotReceiveWithAnyArgs().InvalidateAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_WhenModelSkipsWholeBatch_StopsInsteadOfLooping()
    {
        _reviews.ListUnclassifiedAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([TestData.Review()]);
        _reviews.CountUnclassifiedAsync(Arg.Any<CancellationToken>()).Returns(1);
        _classifier.ClassifyAsync(Arg.Any<IReadOnlyList<Review>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyDictionary<Guid, ReviewClassification>>(new Dictionary<Guid, ReviewClassification>()));

        var result = await _handler.HandleAsync(new ClassifyPendingReviewsCommand(), TestContext.Current.CancellationToken);

        result.Value.Classified.ShouldBe(0);
        result.Value.StoppedReason.ShouldNotBeNull();
        await _classifier.Received(1).ClassifyAsync(Arg.Any<IReadOnlyList<Review>>(), Arg.Any<CancellationToken>());
    }

    private static Result<IReadOnlyDictionary<Guid, ReviewClassification>> ClassifyAll(IReadOnlyList<Review> reviews) =>
        reviews.ToDictionary(review => review.Id, _ => new ReviewClassification(ReviewSentiment.Negative, [ReviewTheme.Delay]));
}
