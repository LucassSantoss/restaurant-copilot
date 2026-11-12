using System.Globalization;
using System.Text;
using RestaurantCopilot.Application.Abstractions.Ai;
using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Api.Tests.Infrastructure;

/// <summary>
/// Deterministic stand-in for the embedding model: a normalized bag of words hashed into the
/// vector. Texts sharing words end up close, which is enough to test the RAG pipeline end to end
/// without calling Gemini.
/// </summary>
internal sealed class KeywordEmbeddingService : IEmbeddingService
{
    public int Dimensions => 768;

    public Task<Result<IReadOnlyList<float[]>>> EmbedAsync(
        IReadOnlyList<string> texts,
        EmbeddingPurpose purpose,
        CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success<IReadOnlyList<float[]>>(texts.Select(Embed).ToList()));

    private float[] Embed(string text)
    {
        var vector = new float[Dimensions];
        foreach (var word in Words(text))
        {
            vector[(int)((uint)StableHash(word) % Dimensions)] += 1;
        }

        var norm = MathF.Sqrt(vector.Sum(value => value * value));
        return norm == 0 ? vector : [.. vector.Select(value => value / norm)];
    }

    private static IEnumerable<string> Words(string text) =>
        text.ToLower(CultureInfo.InvariantCulture)
            .Split([' ', ',', '.', ':', '!', '?', '"', '(', ')', '/'], StringSplitOptions.RemoveEmptyEntries)
            .Where(word => word.Length > 3);

    private static int StableHash(string word) =>
        Encoding.UTF8.GetBytes(word).Aggregate(17, (hash, value) => unchecked((hash * 31) + value));
}
