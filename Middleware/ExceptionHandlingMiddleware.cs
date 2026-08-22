using System.Text.Json;
using MidnightApi.Exceptions;
using MidnightApi.Models;
using Npgsql;

namespace MidnightApi.Middleware;

public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _iLogger;
    private readonly IHostEnvironment _iHostEnvironment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _iLogger = logger;
        _iHostEnvironment = environment;
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
            _iLogger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        else
        {
            _iLogger.LogWarning(exception, "Request failed with {StatusCode} for {Method} {Path}", statusCode, context.Request.Method, context.Request.Path);
        }

        var errors = new List<ApiError>
        {
            new()
            {
                Code = code,
                Message = message
            }
        };

        if (_iHostEnvironment.IsDevelopment() && statusCode >= StatusCodes.Status500InternalServerError)
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
            PostgresException pg when pg.SqlState == PostgresErrorCodes.UniqueViolation =>
                (StatusCodes.Status409Conflict, "A record with the same unique value already exists.", "UNIQUE_CONSTRAINT"),
            PostgresException pg when pg.SqlState == PostgresErrorCodes.SerializationFailure =>
                (StatusCodes.Status409Conflict, "The record was modified by another request.", "CONCURRENCY_CONFLICT"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", "INTERNAL_ERROR")
        };
    }

    private string ResolveDeveloperMessage(Exception exception)
    {
        if (!_iHostEnvironment.IsDevelopment())
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
