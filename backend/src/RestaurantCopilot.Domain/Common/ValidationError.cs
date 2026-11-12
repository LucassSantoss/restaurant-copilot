namespace RestaurantCopilot.Domain.Common;

/// <summary>A validation failure that carries the error messages of each invalid field.</summary>
public sealed record ValidationError(IReadOnlyDictionary<string, string[]> Errors)
    : Error("Validation.Failed", "One or more validation errors occurred.", ErrorType.Validation);
