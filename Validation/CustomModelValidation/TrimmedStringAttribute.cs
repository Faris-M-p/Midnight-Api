using System.ComponentModel.DataAnnotations;

namespace MidnightApi.Validation.CustomModelValidation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class TrimmedStringAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        if (value is string text && (string.IsNullOrEmpty(text) || text == text.Trim()))
        {
            return ValidationResult.Success;
        }

        var message = ErrorMessage ?? $"{validationContext.DisplayName} must not have leading or trailing spaces.";
        return new ValidationResult(message);
    }
}
