using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Application.Abstractions.Ai;

/// <summary>
/// Provider-agnostic port to a large language model. One call covers plain text generation,
/// structured JSON output (<see cref="LlmRequest.ResponseJsonSchema"/>) and function calling
/// (<see cref="LlmRequest.Tools"/>). Switching providers means writing a new implementation.
/// </summary>
public interface ILlmClient
{
    Task<Result<LlmResponse>> GenerateAsync(LlmRequest request, CancellationToken cancellationToken);
}
