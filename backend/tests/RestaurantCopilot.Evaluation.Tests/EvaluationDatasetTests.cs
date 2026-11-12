namespace RestaurantCopilot.Evaluation.Tests;

/// <summary>Guards the committed question set: it must load and every item must be gradable.</summary>
public sealed class EvaluationDatasetTests
{
    [Fact]
    public async Task QuestionSet_IsValid()
    {
        var dataset = await EvaluationDataset.LoadAsync(QuestionsPath(), TestContext.Current.CancellationToken);

        dataset.Questions.Count.ShouldBeInRange(20, 30);
        dataset.Questions.Select(question => question.Id).ShouldBeUnique();
        dataset.Questions.ShouldAllBe(question => question.Grading.Type != GradingType.Numeric || question.Grading.Expected!.Count > 0);
        dataset.Questions.ShouldAllBe(question => question.Grading.Type != GradingType.Keywords || question.Grading.Keywords!.Count > 0);
        dataset.Questions.ShouldAllBe(question => question.Grading.Type != GradingType.Judge || !string.IsNullOrWhiteSpace(question.Grading.Criteria));
        dataset.Questions.Where(question => question.Grading.Type == GradingType.Numeric)
            .ShouldAllBe(question => question.ReferenceSql != null);
    }

    [Fact]
    public async Task SmokeSuite_IsSmallAndNeedsNoJudge()
    {
        var dataset = await EvaluationDataset.LoadAsync(QuestionsPath(), TestContext.Current.CancellationToken);

        var smoke = dataset.Questions.Where(question => question.Contains(Suites.Smoke)).ToList();

        // Must fit the Gemini free tier (20 requests/day) with room left for normal use.
        smoke.Count.ShouldBeInRange(1, 5);
        smoke.ShouldAllBe(question => question.Grading.Type != GradingType.Judge);
    }

    [Fact]
    public void EstimateLlmCalls_CountsEachToolTurnTheAnswerAndTheJudge()
    {
        var numeric = new EvaluationQuestion("q", "sales", "?", new GradingSpec(GradingType.Numeric, Expected: [1m]));
        var judged = numeric with { Grading = new GradingSpec(GradingType.Judge, Criteria: "x") };
        var outcome = new ChatOutcome("resposta", ["search_sales", "search_sales"], 0, 0, TimeSpan.Zero, null);

        EvaluationRunner.EstimateLlmCalls(numeric, outcome).ShouldBe(3);
        EvaluationRunner.EstimateLlmCalls(judged, outcome).ShouldBe(4);
    }

    private static string QuestionsPath()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "evaluation", "questions.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException("evaluation/questions.json not found.");
    }
}
