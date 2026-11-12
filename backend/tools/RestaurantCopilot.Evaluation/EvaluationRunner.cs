using RestaurantCopilot.Evaluation.Grading;

namespace RestaurantCopilot.Evaluation;

internal sealed class EvaluationRunner(ChatApiClient api, LlmJudge judge)
{
    /// <param name="maxLlmCalls">
    /// Budget of LLM calls (agent turns plus judge calls). Once spent, the remaining questions are
    /// skipped instead of run, so an evaluation never consumes the whole free-tier daily quota.
    /// </param>
    public async Task<IReadOnlyList<QuestionResult>> RunAsync(
        IReadOnlyList<EvaluationQuestion> questions,
        TimeSpan delayBetweenQuestions,
        int maxLlmCalls,
        CancellationToken cancellationToken)
    {
        var results = new List<QuestionResult>();
        var llmCalls = 0;
        foreach (var (question, index) in questions.Select((question, index) => (question, index)))
        {
            if (llmCalls >= maxLlmCalls)
            {
                results.Add(QuestionResult.Skipped(question));
                Console.WriteLine($"[{index + 1}/{questions.Count}] SKIP {question.Id}: LLM call budget ({maxLlmCalls}) spent.");
                continue;
            }

            if (index > 0)
            {
                // Spreads requests to stay under the free tier's requests-per-minute limit.
                await Task.Delay(delayBetweenQuestions, cancellationToken);
            }

            var outcome = await api.AskAsync(question.Question, cancellationToken);
            var grade = await GradeAsync(question, outcome, cancellationToken);
            llmCalls += EstimateLlmCalls(question, outcome);
            results.Add(new QuestionResult(question, outcome, grade));

            Console.WriteLine($"[{index + 1}/{questions.Count}] {(grade.Passed ? "PASS" : "FAIL")} {question.Id}: {grade.Reason}");
        }

        return results;
    }

    /// <summary>
    /// Conservative estimate: one model turn per tool call plus the final answer (parallel calls
    /// share a turn in reality), plus the judge. Failed requests may still have used the quota.
    /// </summary>
    internal static int EstimateLlmCalls(EvaluationQuestion question, ChatOutcome outcome) =>
        outcome.ToolsUsed.Count + 1 + (question.Grading.Type == GradingType.Judge && outcome.Answer is not null ? 1 : 0);

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

internal sealed record QuestionResult(EvaluationQuestion Question, ChatOutcome Outcome, GradeResult Grade, bool WasSkipped = false)
{
    public static QuestionResult Skipped(EvaluationQuestion question) =>
        new(question, new ChatOutcome(null, [], 0, 0, TimeSpan.Zero, "skipped"), GradeResult.Fail("Skipped: LLM call budget spent."), WasSkipped: true);

    /// <summary>Whether the agent used at least one of the tools the question expects (informational).</summary>
    public bool? UsedExpectedTool => Question.ExpectedTools is { Count: > 0 } expected
        ? Outcome.ToolsUsed.Intersect(expected).Any()
        : null;
}
