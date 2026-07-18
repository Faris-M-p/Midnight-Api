using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace MidnightApi.Validation.CustomModelValidation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class PhoneNumberAttribute : ValidationAttribute
{
    private static readonly Regex PhoneRegex = new(@"^\+?[0-9\s\-().]{7,20}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        if (value is string text && (string.IsNullOrWhiteSpace(text) || PhoneRegex.IsMatch(text)))
        {
            return ValidationResult.Success;
        }

        var displayName = ValidationDisplayNameHelper.Resolve(validationContext);
        var message = ErrorMessage ?? $"{displayName} is not a valid phone number.";
        return new ValidationResult(message);
    }
}
