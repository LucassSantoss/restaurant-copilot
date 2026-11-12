using System.Globalization;

namespace RestaurantCopilot.Evaluation;

internal sealed record CommandLineArguments(
    Uri ApiUrl,
    string QuestionsPath,
    string OutputPath,
    TimeSpan Delay,
    bool Prepare,
    IReadOnlySet<string> OnlyIds,
    string Suite,
    int MaxLlmCalls)
{
    public static CommandLineArguments Parse(string[] args)
    {
        string? Value(string name) =>
            args.SkipWhile(arg => arg != name).Skip(1).FirstOrDefault();

        return new CommandLineArguments(
            new Uri(Value("--api") ?? "http://localhost:8080/"),
            Value("--questions") ?? "evaluation/questions.json",
            Value("--output") ?? "evaluation/results/latest.md",
            TimeSpan.FromSeconds(double.Parse(Value("--delay") ?? "4", CultureInfo.InvariantCulture)),
            args.Contains("--prepare"),
            (Value("--only") ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(),
            Value("--suite") ?? Suites.Smoke,
            int.Parse(Value("--max-llm-calls") ?? "10", CultureInfo.InvariantCulture));
    }
}
