using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using MidnightApi.Exceptions;
using MidnightApi.Models;
using MidnightApi.Validation.CustomModelValidation;

namespace MidnightApi.Services;

public class CommonService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null
    };

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

    public string? ToJson(object? value) =>
        value is null ? null : JsonSerializer.Serialize(value, JsonOptions);

    public void EnsureSuccess(CommonResponse result)
    {
        if (result.ResponseCode == 0)
        {
            return;
        }

        throw result.StatusCode switch
        {
            StatusCodes.Status404NotFound => new NotFoundException(result.ResponseMessage),
            StatusCodes.Status409Conflict => new ConflictException(result.ResponseMessage),
            StatusCodes.Status401Unauthorized => new UnauthorizedAccessException(result.ResponseMessage),
            _ => new BadRequestException(result.ResponseMessage)
        };
    }

    public IActionResult ToActionResult<T>(T result, string? traceId = null)
        where T : CommonResponse
    {
        EnsureSuccess(result);

        return new ObjectResult(new ApiResponse<T>
        {
            Success = true,
            StatusCode = result.StatusCode,
            Message = result.ResponseMessage,
            Data = result,
            TraceId = traceId
        })
        {
            StatusCode = result.StatusCode
        };
    }
}
