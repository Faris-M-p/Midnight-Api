using System.Collections;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MidnightApi.Models;

namespace MidnightApi.Filters;

/// <summary>
/// Wraps any ObjectResult that is not already an ApiResponse into the common envelope.
/// </summary>
public class ApiResponseResultFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult objectResult && objectResult.Value is not null)
        {
            if (!IsApiResponse(objectResult.Value))
            {
                var statusCode = objectResult.StatusCode
                    ?? (context.HttpContext.Response.StatusCode > 0
                        ? context.HttpContext.Response.StatusCode
                        : StatusCodes.Status200OK);

                var traceId = context.HttpContext.TraceIdentifier;

                if (statusCode >= StatusCodes.Status400BadRequest)
                {
                    var message = ExtractMessage(objectResult.Value) ?? "Request failed.";
                    objectResult.Value = ApiResponse<object?>.Fail(
                        message,
                        statusCode,
                        traceId: traceId);
                    objectResult.DeclaredType = typeof(ApiResponse<object?>);
                }
                else
                {
                    objectResult.Value = WrapSuccess(objectResult.Value, statusCode, traceId);
                    objectResult.DeclaredType = objectResult.Value.GetType();
                }

                objectResult.StatusCode = statusCode;
            }
        }
        else if (context.Result is EmptyResult or NoContentResult)
        {
            context.Result = new ObjectResult(ApiResponse<object?>.Ok(
                null,
                "Success",
                StatusCodes.Status200OK,
                context.HttpContext.TraceIdentifier))
            {
                StatusCode = StatusCodes.Status200OK
            };
        }

        await next();
    }

    private static bool IsApiResponse(object value)
    {
        var type = value.GetType();
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ApiResponse<>);
    }

    private static object WrapSuccess(object data, int statusCode, string traceId)
    {
        var method = typeof(ApiResponse<>)
            .MakeGenericType(data.GetType())
            .GetMethod(nameof(ApiResponse<object>.Ok), BindingFlags.Public | BindingFlags.Static);

        return method!.Invoke(null, [data, "Success", statusCode, traceId, string.Empty])!;
    }

    private static string? ExtractMessage(object value)
    {
        if (value is string text)
        {
            return text;
        }

        var messageProperty = value.GetType().GetProperty("message", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
            ?? value.GetType().GetProperty("Message", BindingFlags.Public | BindingFlags.Instance);

        if (messageProperty?.GetValue(value) is string message && !string.IsNullOrWhiteSpace(message))
        {
            return message;
        }

        if (value is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Key?.ToString()?.Equals("message", StringComparison.OrdinalIgnoreCase) == true
                    && entry.Value is string dictMessage)
                {
                    return dictMessage;
                }
            }
        }

        return null;
    }
}
