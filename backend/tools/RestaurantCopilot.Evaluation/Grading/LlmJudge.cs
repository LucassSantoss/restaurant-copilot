using RestaurantCopilot.Application.Abstractions.Ai;

namespace RestaurantCopilot.Evaluation.Grading;

/// <summary>LLM-as-judge for open answers (advice, summaries), with schema-constrained output.</summary>
internal sealed class LlmJudge(ILlmClient llmClient)
{
    private const string SystemInstruction = """
        You grade answers of an analytics assistant for a restaurant owner. Answers are in Brazilian Portuguese.
        Decide whether the answer satisfies ALL the grading criteria. Be strict: vague, generic or partially
        correct answers fail. Do not reward style. Explain the decision in one or two sentences.
        """;

    public async Task<GradeResult> GradeAsync(string question, string answer, string criteria, CancellationToken cancellationToken)
    {
        var prompt = $"""
            Question: {question}

            Grading criteria: {criteria}

            Answer to grade:
            <answer>
            {answer}
            </answer>
            """;

        var verdict = await llmClient.GenerateJsonAsync<Verdict>(SystemInstruction, prompt, cancellationToken);
        if (verdict.IsFailure)
        {
            return GradeResult.Fail($"Judge unavailable: {verdict.Error.Message}");
        }

        return new GradeResult(verdict.Value.Passed, verdict.Value.Reasoning);
    }

    internal sealed record Verdict(bool Passed, string Reasoning);
}
