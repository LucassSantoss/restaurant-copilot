using System.Globalization;
using System.Text;

namespace RestaurantCopilot.Evaluation;

/// <summary>Writes the evaluation report as Markdown, ready to be linked from docs/evaluation.md.</summary>
internal static class ReportWriter
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public static string Render(IReadOnlyList<QuestionResult> results, string model, DateTimeOffset startedAt)
    {
        var report = new StringBuilder();
        var passed = results.Count(result => result.Grade.Passed);

        report.AppendLine("# Resultado da avaliação do agente").AppendLine();
        report.AppendLine(Invariant, $"- **Data:** {startedAt:yyyy-MM-dd HH:mm} UTC");
        report.AppendLine(Invariant, $"- **Modelo:** `{model}`");
        report.AppendLine(Invariant, $"- **Taxa de acerto:** **{passed}/{results.Count} ({Percent(passed, results.Count)})**");
        report.AppendLine(Invariant, $"- **Latência média:** {results.Average(result => result.Outcome.Elapsed.TotalSeconds):0.0} s");
        report.AppendLine(Invariant, $"- **Tokens:** {results.Sum(result => result.Outcome.InputTokens):N0} de entrada / {results.Sum(result => result.Outcome.OutputTokens):N0} de saída");

        var withExpectation = results.Where(result => result.UsedExpectedTool is not null).ToList();
        if (withExpectation.Count > 0)
        {
            var usedExpected = withExpectation.Count(result => result.UsedExpectedTool == true);
            report.AppendLine(Invariant, $"- **Usou uma ferramenta esperada:** {usedExpected}/{withExpectation.Count} ({Percent(usedExpected, withExpectation.Count)})");
        }

        report.AppendLine().AppendLine("## Por categoria").AppendLine();
        report.AppendLine("| Categoria | Acertos | Taxa |").AppendLine("|---|---|---|");
        foreach (var group in results.GroupBy(result => result.Question.Category))
        {
            var groupPassed = group.Count(result => result.Grade.Passed);
            report.AppendLine(Invariant, $"| {group.Key} | {groupPassed}/{group.Count()} | {Percent(groupPassed, group.Count())} |");
        }

        report.AppendLine().AppendLine("## Por pergunta").AppendLine();
        report.AppendLine("| | Pergunta | Correção | Ferramentas | Tempo | Motivo |").AppendLine("|---|---|---|---|---|---|");
        foreach (var result in results)
        {
            report.AppendLine(Invariant,
                $"| {(result.Grade.Passed ? "✅" : "❌")} | `{result.Question.Id}` {Escape(result.Question.Question)} | {result.Question.Grading.Type} | {string.Join(", ", result.Outcome.ToolsUsed.Distinct())} | {result.Outcome.Elapsed.TotalSeconds:0.0} s | {Escape(result.Grade.Reason)} |");
        }

        report.AppendLine().AppendLine("## Respostas").AppendLine();
        foreach (var result in results)
        {
            report.AppendLine(Invariant, $"### {result.Question.Id}: {result.Question.Question}").AppendLine();
            report.AppendLine(result.Outcome.Answer is { } answer ? $"> {answer.ReplaceLineEndings("\n> ")}" : $"_Sem resposta: {result.Outcome.Error}_");
            report.AppendLine();
        }

        return report.ToString();
    }

    private static string Percent(int part, int total) =>
        total == 0 ? "-" : (part / (double)total).ToString("P0", CultureInfo.GetCultureInfo("pt-BR"));

    private static string Escape(string text) => text.ReplaceLineEndings(" ").Replace("|", "\\|", StringComparison.Ordinal);
}
