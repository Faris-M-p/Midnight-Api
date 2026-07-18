using Microsoft.AspNetCore.Mvc.ModelBinding;
using MidnightApi.Exceptions;

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

        throw new BadRequestException(string.Join(" | ", errors));
    }
}
