// Agent evaluation runner. Runs every question in evaluation/questions.json against the running API,
// grades the answers (exact numbers, keywords, or an LLM judge) and writes a Markdown report.
//
//   dotnet run --project backend/tools/RestaurantCopilot.Evaluation -- [--api URL] [--prepare]
//       [--questions PATH] [--output PATH] [--delay SECONDS] [--only ID,ID]
//
// The judge uses the same Gemini configuration as the API (user-secrets or Gemini__ApiKey).
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

var geminiOptions = provider.GetRequiredService<IOptions<GeminiOptions>>().Value;
if (!geminiOptions.IsConfigured)
{
    Console.Error.WriteLine("Gemini API key not found. Set it with user-secrets (same as the API) or Gemini__ApiKey.");
    return 1;
}

var api = provider.GetRequiredService<ChatApiClient>();
if (arguments.Prepare)
{
    // The agent's review and RAG answers need classified reviews and a fresh search index.
    await api.PostAsync("api/ingestion/classify-reviews", CancellationToken.None);
    await api.PostAsync("api/ingestion/reindex", CancellationToken.None);
}

var dataset = await EvaluationDataset.LoadAsync(arguments.QuestionsPath, CancellationToken.None);
var questions = arguments.OnlyIds.Count == 0
    ? dataset.Questions
    : dataset.Questions.Where(question => arguments.OnlyIds.Contains(question.Id)).ToList();

var startedAt = DateTimeOffset.UtcNow;
var results = await provider.GetRequiredService<EvaluationRunner>()
    .RunAsync(questions, arguments.Delay, CancellationToken.None);

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(arguments.OutputPath))!);
await File.WriteAllTextAsync(arguments.OutputPath, ReportWriter.Render(results, geminiOptions.GenerationModel, startedAt));

var passed = results.Count(result => result.Grade.Passed);
Console.WriteLine($"Accuracy: {passed}/{results.Count}. Report written to {arguments.OutputPath}.");
return 0;
