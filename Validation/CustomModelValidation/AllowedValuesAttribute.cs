using System.ComponentModel.DataAnnotations;

namespace MidnightApi.Validation.CustomModelValidation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class AllowedValuesAttribute : ValidationAttribute
{
    private readonly HashSet<string> _allowedValues;

    public AllowedValuesAttribute(params string[] allowedValues)
    {
        _allowedValues = allowedValues
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        if (value is string text && _allowedValues.Contains(text.Trim()))
        {
            return ValidationResult.Success;
        }

        if (value is string)
        {
            var displayName = ValidationDisplayNameHelper.Resolve(validationContext);
            var message = ErrorMessage ?? $"{displayName} must be one of: {string.Join(", ", _allowedValues)}.";
            return new ValidationResult(message);
        }

        return ValidationResult.Success;
    }
}
