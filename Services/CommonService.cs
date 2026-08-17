using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using MidnightApi.Exceptions;
using MidnightApi.Models;
using MidnightApi.Services.Interfaces;
using MidnightApi.Validation.CustomModelValidation;

namespace MidnightApi.Services;

public class CommonService : ICommonService
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

    public void EnsureSuccess<T>(CommonResponse<T> result)
    {
        if (result.Status && result.ResponseCode > 0)
        {
            return;
        }

        var message = result.ResponseMessage ?? "Request failed.";
        throw result.ResponseCode switch
        {
            30 => new NotFoundException(message),
            20 => new ConflictException(message),
            _ => new BadRequestException(message)
        };
    }

    public IActionResult ToActionResult<T>(CommonResponse<T> result, string? traceId = null, int? httpStatus = null)
    {
        EnsureSuccess(result);

        var statusCode = httpStatus ?? StatusCodes.Status200OK;
        return new ObjectResult(new ApiResponse<CommonResponse<T>>
        {
            Success = true,
            StatusCode = statusCode,
            Message = result.ResponseMessage ?? "Success.",
            Data = result,
            TraceId = traceId
        })
        {
            StatusCode = statusCode
        };
    }
}
