using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Domain.Tests.Common;

public sealed class ResultTests
{
    private static readonly Error SomeError = Error.Validation("Some.Code", "Something is wrong.");

    [Fact]
    public void Success_HasValueAndNoError()
    {
        Result<int> result = 42;

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
        result.Error.ShouldBe(Error.None);
    }

    [Fact]
    public void Failure_HasErrorAndThrowsOnValueAccess()
    {
        Result<int> result = SomeError;

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SomeError);
        Should.Throw<InvalidOperationException>(() => result.Value);
    }
}
