namespace PizzasFuriosas.Core.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }

    public ApiResponse(T data, string? message = null)
    {
        Success = true;
        Data = data;
        Message = message;
    }

    public ApiResponse(string message)
    {
        Success = false;
        Data = default;
        Message = message;
    }
}

public class ApiResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }

    public static ApiResponse Ok(string? message = null)
    {
        return new ApiResponse()
        {
            Success = true,
            Message = message
        };
    }

    public static ApiResponse Error(string message)
    {
        return new ApiResponse()
        {
            Success = false,
            Message = message
        };
    }

    public static ApiResponse ValidationError(string message, Dictionary<string, string[]> errors)
    {
        return new ApiResponse()
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }
}