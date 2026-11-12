// Agent evaluation runner. Runs every question in evaluation/questions.json against the running API,
// grades the answers (exact numbers, keywords, or an LLM judge) and writes a Markdown report.
//
//   dotnet run --project backend/tools/RestaurantCopilot.Evaluation -- [--suite smoke|full] [--max-llm-calls N]
//       [--only ID,ID] [--api URL] [--prepare] [--questions PATH] [--output PATH] [--delay SECONDS]
//
// It is never run automatically (CI only runs the unit tests): every question spends real LLM quota.
// By default it runs the small "smoke" suite and stops after ~10 LLM calls, leaving most of the
// Gemini free-tier daily quota for normal use. The judge (open questions only) uses the same Gemini
// configuration as the API (user-secrets or Gemini__ApiKey).
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestaurantCopilot.Application.Abstractions.Ai;
using RestaurantCopilot.Evaluation;
using RestaurantCopilot.Evaluation.Grading;
using RestaurantCopilot.Infrastructure.Ai.Gemini;

var arguments = CommandLineArguments.Parse(args);
var configuration = new ConfigurationBuilder()
    .AddUserSecrets(typeof(EvaluationRunner).Assembly, optional: true)
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection()
    .AddLogging(logging => logging.AddSimpleConsole().SetMinimumLevel(LogLevel.Warning))
    .AddGemini(configuration);
services.AddHttpClient<ChatApiClient>(client =>
{
    client.BaseAddress = arguments.ApiUrl;
    client.Timeout = TimeSpan.FromMinutes(3);
});
services.AddTransient<LlmJudge>();
services.AddTransient<EvaluationRunner>();
await using var provider = services.BuildServiceProvider();

var dataset = await EvaluationDataset.LoadAsync(arguments.QuestionsPath, CancellationToken.None);
var questions = dataset.Questions
    .Where(question => arguments.OnlyIds.Count > 0 ? arguments.OnlyIds.Contains(question.Id) : question.Contains(arguments.Suite))
    .ToList();

var geminiOptions = provider.GetRequiredService<IOptions<GeminiOptions>>().Value;
if (questions.Any(question => question.Grading.Type == GradingType.Judge) && !geminiOptions.IsConfigured)
{
    Console.Error.WriteLine("The LLM judge needs a Gemini API key. Set it with user-secrets (same as the API) or Gemini__ApiKey.");
    return 1;
}

Console.WriteLine($"Running {questions.Count} question(s), budget of {arguments.MaxLlmCalls} LLM calls.");

var api = provider.GetRequiredService<ChatApiClient>();
if (arguments.Prepare)
{
    // The agent's review and RAG answers need classified reviews and a fresh search index.
    await api.PostAsync("api/ingestion/classify-reviews", CancellationToken.None);
    await api.PostAsync("api/ingestion/reindex", CancellationToken.None);
}

var startedAt = DateTimeOffset.UtcNow;
var results = await provider.GetRequiredService<EvaluationRunner>()
    .RunAsync(questions, arguments.Delay, arguments.MaxLlmCalls, CancellationToken.None);

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(arguments.OutputPath))!);
await File.WriteAllTextAsync(arguments.OutputPath, ReportWriter.Render(results, geminiOptions.GenerationModel, startedAt));

var answered = results.Where(result => !result.WasSkipped).ToList();
Console.WriteLine($"Accuracy: {answered.Count(result => result.Grade.Passed)}/{answered.Count} ({results.Count - answered.Count} skipped). Report written to {arguments.OutputPath}.");
return 0;
