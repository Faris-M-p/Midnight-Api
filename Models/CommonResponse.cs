namespace MidnightApi.Models;

public class IdResponse
{
    public long Id { get; set; }
}

public class CommonResponse<T>
{
    public long ResponseCode { get; set; }
    public bool Status { get; set; }
    public string? ResponseMessage { get; set; }
    public T? Data { get; set; }
}
