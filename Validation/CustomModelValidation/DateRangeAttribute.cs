using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace MidnightApi.Validation.CustomModelValidation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class DateRangeAttribute : ValidationAttribute
{
    private readonly string _fromPropertyName;
    private readonly string _toPropertyName;

    public DateRangeAttribute(string fromPropertyName, string toPropertyName)
    {
        _fromPropertyName = fromPropertyName;
        _toPropertyName = toPropertyName;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var type = validationContext.ObjectType;
        var fromProperty = type.GetProperty(_fromPropertyName, BindingFlags.Public | BindingFlags.Instance);
        var toProperty = type.GetProperty(_toPropertyName, BindingFlags.Public | BindingFlags.Instance);

        if (fromProperty is null || toProperty is null)
        {
            return ValidationResult.Success;
        }

        var from = ToDateOnly(fromProperty.GetValue(validationContext.ObjectInstance));
        var to = ToDateOnly(toProperty.GetValue(validationContext.ObjectInstance));

        if (!from.HasValue || !to.HasValue || from.Value <= to.Value)
        {
            return ValidationResult.Success;
        }

        var fromDisplayName = ValidationDisplayNameHelper.Resolve(validationContext, _fromPropertyName);
        var toDisplayName = ValidationDisplayNameHelper.Resolve(validationContext, _toPropertyName);
        var message = ErrorMessage ?? $"{fromDisplayName} must be less than or equal to {toDisplayName}.";
        return new ValidationResult(message);
    }

    private static DateOnly? ToDateOnly(object? value) =>
        value switch
        {
            DateOnly dateOnly => dateOnly,
            DateTime dateTime => DateOnly.FromDateTime(dateTime),
            _ => null
        };
}
