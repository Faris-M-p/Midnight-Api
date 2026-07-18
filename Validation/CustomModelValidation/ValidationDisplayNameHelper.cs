using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MidnightApi.Validation.CustomModelValidation;

internal static class ValidationDisplayNameHelper
{
    private static readonly Regex AcronymBoundaryRegex = new("([A-Z]+)([A-Z][a-z])", RegexOptions.Compiled);
    private static readonly Regex WordBoundaryRegex = new("([a-z0-9])([A-Z])", RegexOptions.Compiled);

    public static string Resolve(ValidationContext context, string? memberName = null)
    {
        var propertyName = memberName ?? context.MemberName ?? context.DisplayName;
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return "Value";
        }

        var property = context.ObjectType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        var displayAttribute = property?.GetCustomAttribute<DisplayAttribute>();
        if (!string.IsNullOrWhiteSpace(displayAttribute?.GetName()))
        {
            return displayAttribute!.GetName()!;
        }

        if (!string.IsNullOrWhiteSpace(context.DisplayName) && !string.Equals(context.DisplayName, propertyName, StringComparison.Ordinal))
        {
            return context.DisplayName;
        }

        return ToReadableName(propertyName);
    }

    public static string ToReadableName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Value";
        }

        var normalized = name.Replace("_", " ").Trim();
        normalized = AcronymBoundaryRegex.Replace(normalized, "$1 $2");
        normalized = WordBoundaryRegex.Replace(normalized, "$1 $2");
        return normalized;
    }
}
