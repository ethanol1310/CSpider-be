using System.Text.Json.Serialization;

namespace CSpider.Api.DTO;

public static class ErrorCodes
{
    public const string SUCCESS = "success";
    public const string INVALID_PARAMETER = "invalid_parameter";
    public const string INTERNAL_ERROR = "internal_error";
}

public class ApiResponse<T>
{
    [JsonPropertyName("data")]
    public T Data { get; set; }

    [JsonPropertyName("error_code")]
    public string ErrorCode { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }

    public static ApiResponse<T> Success(T data)
    {
        return new ApiResponse<T>
        {
            Data = data,
            ErrorCode = ErrorCodes.SUCCESS,
            Message = "Success"
        };
    }

    public static ApiResponse<T> Error(string errorCode, string message)
    {
        return new ApiResponse<T>
        {
            Data = default,
            ErrorCode = errorCode,
            Message = message
        };
    }
}
