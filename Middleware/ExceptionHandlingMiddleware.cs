using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MidnightApi.Exceptions;
using MidnightApi.Models.Api;

namespace MidnightApi.Middleware;

public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message, code) = MapException(exception);
        var developerMessage = ResolveDeveloperMessage(exception);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogWarning(exception, "Request failed with {StatusCode} for {Method} {Path}", statusCode, context.Request.Method, context.Request.Path);
        }

        var errors = new List<ApiError>
        {
            new()
            {
                Code = code,
                Message = message
            }
        };

        if (_environment.IsDevelopment() && statusCode >= StatusCodes.Status500InternalServerError)
        {
            errors.Add(new ApiError
            {
                Code = "EXCEPTION_DETAILS",
                Message = exception.ToString()
            });
        }

        var response = ApiResponse<object?>.Fail(
            message,
            statusCode,
            traceId: context.TraceIdentifier,
            errors: errors,
            developerMessage: developerMessage);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private static (int StatusCode, string Message, string Code) MapException(Exception exception)
    {
        return exception switch
        {
            AppException appException => (appException.StatusCode, appException.Message, appException.GetType().Name.Replace("Exception", string.Empty).ToUpperInvariant()),
            KeyNotFoundException keyNotFound => (StatusCodes.Status404NotFound, keyNotFound.Message, "NOT_FOUND"),
            ArgumentException argument => (StatusCodes.Status400BadRequest, argument.Message, "BAD_REQUEST"),
            UnauthorizedAccessException unauthorized => (StatusCodes.Status401Unauthorized, unauthorized.Message, "UNAUTHORIZED"),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "The record was modified by another request.", "CONCURRENCY_CONFLICT"),
            DbUpdateException dbUpdate when IsUniqueConstraintViolation(dbUpdate) =>
                (StatusCodes.Status409Conflict, "A record with the same unique value already exists.", "UNIQUE_CONSTRAINT"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", "INTERNAL_ERROR")
        };
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true
            || exception.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true;
    }

    private string ResolveDeveloperMessage(Exception exception)
    {
        if (!_environment.IsDevelopment())
        {
            return string.Empty;
        }

        if (exception is AppException appException && !string.IsNullOrWhiteSpace(appException.DeveloperMessage))
        {
            return appException.DeveloperMessage!;
        }

        var inner = exception.InnerException is null
            ? string.Empty
            : $" | Inner: {exception.InnerException.GetType().Name}: {exception.InnerException.Message}";

        return $"{exception.GetType().Name}: {exception.Message}{inner}";
    }
}
