namespace MidnightApi.Models;

public class CommonResponse
{
    public int ResponseCode { get; set; }
    public int StatusCode { get; set; }
    public string ResponseMessage { get; set; } = string.Empty;
}
