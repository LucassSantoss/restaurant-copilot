using RestaurantCopilot.Evaluation.Grading;

namespace RestaurantCopilot.Evaluation;

internal sealed class EvaluationRunner(ChatApiClient api, LlmJudge judge)
{
    public async Task<IReadOnlyList<QuestionResult>> RunAsync(
        IReadOnlyList<EvaluationQuestion> questions,
        TimeSpan delayBetweenQuestions,
        CancellationToken cancellationToken)
    {
        var results = new List<QuestionResult>();
        foreach (var (question, index) in questions.Select((question, index) => (question, index)))
        {
            if (index > 0)
            {
                // Spreads requests to stay under the free tier's requests-per-minute limit.
                await Task.Delay(delayBetweenQuestions, cancellationToken);
            }

            var outcome = await api.AskAsync(question.Question, cancellationToken);
            var grade = await GradeAsync(question, outcome, cancellationToken);
            results.Add(new QuestionResult(question, outcome, grade));

            Console.WriteLine($"[{index + 1}/{questions.Count}] {(grade.Passed ? "PASS" : "FAIL")} {question.Id}: {grade.Reason}");
        }

        return results;
    }

    private async Task<GradeResult> GradeAsync(EvaluationQuestion question, ChatOutcome outcome, CancellationToken cancellationToken)
    {
        if (outcome.Answer is null)
        {
            return GradeResult.Fail($"No answer ({outcome.Error}).");
        }

        var grading = question.Grading;
        return grading.Type switch
        {
            GradingType.Numeric => DeterministicGrader.GradeNumeric(outcome.Answer, grading.Expected ?? [], grading.Tolerance),
            GradingType.Keywords => DeterministicGrader.GradeKeywords(outcome.Answer, grading.Keywords ?? []),
            GradingType.Judge => await judge.GradeAsync(question.Question, outcome.Answer, grading.Criteria ?? string.Empty, cancellationToken),
            _ => GradeResult.Fail($"Unknown grading type {grading.Type}."),
        };
    }
}

internal sealed record QuestionResult(EvaluationQuestion Question, ChatOutcome Outcome, GradeResult Grade)
{
    /// <summary>Whether the agent used at least one of the tools the question expects (informational).</summary>
    public bool? UsedExpectedTool => Question.ExpectedTools is { Count: > 0 } expected
        ? Outcome.ToolsUsed.Intersect(expected).Any()
        : null;
}
