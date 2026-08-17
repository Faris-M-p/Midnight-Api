namespace MidnightApi.Exceptions;

public abstract class AppException : Exception
{
    protected AppException(
        string message,
        int statusCode,
        string? developerMessage = null,
        string? errorCode = null) : base(message)
    {
        StatusCode = statusCode;
        DeveloperMessage = developerMessage;
        ErrorCode = errorCode;
    }

    public int StatusCode { get; }
    public string? DeveloperMessage { get; }
    public string? ErrorCode { get; }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message, string? developerMessage = null)
        : base(message, StatusCodes.Status404NotFound, developerMessage)
    {
    }
}

public class BadRequestException : AppException
{
    public BadRequestException(string message, string? developerMessage = null)
        : base(message, StatusCodes.Status400BadRequest, developerMessage)
    {
    }
}

public class ConflictException : AppException
{
    public ConflictException(string message, string? developerMessage = null)
        : base(message, StatusCodes.Status409Conflict, developerMessage)
    {
    }
}

public class ForbiddenException : AppException
{
    public ForbiddenException(string message = "You don't have permission to perform this action.", string? developerMessage = null)
        : base(message, StatusCodes.Status403Forbidden, developerMessage)
    {
    }
}

public class StorageLimitReachedException : AppException
{
    public StorageLimitReachedException(string message, string? developerMessage = null)
        : base(message, StatusCodes.Status400BadRequest, developerMessage, "STORAGE_LIMIT_REACHED")
    {
    }
}
