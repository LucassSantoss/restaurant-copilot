namespace RestaurantCopilot.Evaluation.Grading;

internal sealed record GradeResult(bool Passed, string Reason)
{
    public static GradeResult Pass(string reason) => new(true, reason);

    public static GradeResult Fail(string reason) => new(false, reason);
}
