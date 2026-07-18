using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace MidnightApi.Validation.CustomModelValidation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class MatchRegexAttribute : ValidationAttribute
{
    private readonly Regex _regex;

    public MatchRegexAttribute(string pattern)
    {
        _regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        if (value is not string text || _regex.IsMatch(text))
        {
            return ValidationResult.Success;
        }

        var displayName = ValidationDisplayNameHelper.Resolve(validationContext);
        var message = ErrorMessage ?? $"{displayName} format is invalid.";
        return new ValidationResult(message);
    }
}
