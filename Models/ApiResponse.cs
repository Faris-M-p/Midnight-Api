using System.Text.Json.Serialization;

namespace MidnightApi.Models;

public class ApiError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Field { get; set; }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string DeveloperMessage { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ApiError>? Errors { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TraceId { get; set; }

    public static ApiResponse<T> Ok(
        T? data,
        string message = "Success",
        int statusCode = StatusCodes.Status200OK,
        string? traceId = null,
        string developerMessage = "")
    {
        return new ApiResponse<T>
        {
            Success = true,
            StatusCode = statusCode,
            Message = message,
            DeveloperMessage = developerMessage,
            Data = data,
            Errors = null,
            TraceId = traceId
        };
    }

    public static ApiResponse<T> Fail(
        string message,
        int statusCode,
        string? traceId = null,
        List<ApiError>? errors = null,
        string developerMessage = "")
    {
        return new ApiResponse<T>
        {
            Success = false,
            StatusCode = statusCode,
            Message = message,
            DeveloperMessage = developerMessage,
            Data = default,
            Errors = errors ??
            [
                new ApiError
                {
                    Code = statusCode.ToString(),
                    Message = message
                }
            ],
            TraceId = traceId
        };
    }

    public static ApiResponse<T> Fail(
        string message,
        int statusCode,
        string code,
        string? field = null,
        string? traceId = null,
        string developerMessage = "")
    {
        return Fail(
            message,
            statusCode,
            traceId,
            [
                new ApiError
                {
                    Code = code,
                    Message = message,
                    Field = field
                }
            ],
            developerMessage);
    }
}
