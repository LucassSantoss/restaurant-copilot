namespace RestaurantCopilot.Domain.Common;

/// <summary>Small validation helpers shared by entity factories.</summary>
internal static class Guard
{
    public static Error? Required(string? value, int maxLength, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Error.Validation($"{code}.Required", $"{code} is required.");
        }

        return value.Trim().Length > maxLength
            ? Error.Validation($"{code}.TooLong", $"{code} must have at most {maxLength} characters.")
            : null;
    }

    public static Error? MaxLength(string? value, int maxLength, string code) =>
        value is not null && value.Trim().Length > maxLength
            ? Error.Validation($"{code}.TooLong", $"{code} must have at most {maxLength} characters.")
            : null;

    public static Error? FirstError(params Error?[] errors) => errors.FirstOrDefault(error => error is not null);
}
