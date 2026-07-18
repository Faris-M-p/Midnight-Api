using System.ComponentModel.DataAnnotations;

namespace MidnightApi.Validation.CustomModelValidation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class FutureDateAttribute : ValidationAttribute
{
    public bool AllowToday { get; set; }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var isValid = value switch
        {
            DateOnly dateOnly => AllowToday ? dateOnly >= today : dateOnly > today,
            DateTime dateTime => AllowToday ? DateOnly.FromDateTime(dateTime) >= today : DateOnly.FromDateTime(dateTime) > today,
            _ => true
        };

        if (isValid)
        {
            return ValidationResult.Success;
        }

        var displayName = ValidationDisplayNameHelper.Resolve(validationContext);
        var message = ErrorMessage ?? $"{displayName} must be a future date.";
        return new ValidationResult(message);
    }
}
