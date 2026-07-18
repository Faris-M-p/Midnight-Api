using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace MidnightApi.Validation.CustomModelValidation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class NoScriptTagsAttribute : ValidationAttribute
{
    private static readonly Regex ScriptTagRegex = new(@"<\s*/?\s*script\b", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        if (value is string text && !ScriptTagRegex.IsMatch(text))
        {
            return ValidationResult.Success;
        }

        var message = ErrorMessage ?? $"{validationContext.DisplayName} contains disallowed script content.";
        return new ValidationResult(message);
    }
}
