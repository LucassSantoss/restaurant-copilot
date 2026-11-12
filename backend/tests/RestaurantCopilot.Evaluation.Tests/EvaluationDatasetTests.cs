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
