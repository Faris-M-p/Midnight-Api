using System.ComponentModel.DataAnnotations;

namespace MidnightApi.Validation.CustomModelValidation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class GreaterThanZeroAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        var valid = value switch
        {
            byte number => number > 0,
            short number => number > 0,
            int number => number > 0,
            long number => number > 0,
            float number => number > 0,
            double number => number > 0,
            decimal number => number > 0,
            _ => true
        };

        if (valid)
        {
            return ValidationResult.Success;
        }

        var message = ErrorMessage ?? $"{validationContext.DisplayName} must be greater than zero.";
        return new ValidationResult(message);
    }
}
