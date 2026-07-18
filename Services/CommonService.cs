using Microsoft.AspNetCore.Mvc.ModelBinding;
using MidnightApi.Exceptions;
using MidnightApi.Validation.CustomModelValidation;

namespace MidnightApi.Services;

public class CommonService
{
    public void ValidateModelState(ModelStateDictionary modelState)
    {
        if (modelState.IsValid)
        {
            return;
        }

        var errors = modelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage)
                ? "Invalid request payload."
                : e.ErrorMessage)
            .Distinct()
            .ToList();

        var developerDetails = modelState
            .Where(x => x.Value?.Errors.Count > 0)
            .SelectMany(pair =>
            {
                var key = string.IsNullOrWhiteSpace(pair.Key) ? "Request" : pair.Key;
                var readableKey = ValidationDisplayNameHelper.ToReadableName(key.Split('.').Last());
                var rawValue = pair.Value?.RawValue;
                var rawText = rawValue is null ? "null" : rawValue.ToString();

                return pair.Value!.Errors.Select(error =>
                    $"Validation failed for '{readableKey}' ({key}). Value: '{rawText}'. Error: {error.ErrorMessage}");
            })
            .Distinct()
            .ToList();

        throw new BadRequestException(
            string.Join(" | ", errors),
            developerDetails.Count == 0 ? "Validation failed." : string.Join(" || ", developerDetails));
    }
}
