namespace DPDP_Api.Models;

public sealed class ApiResponse<T>
{
    public int StatusCode { get; init; }
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public string TraceId { get; init; } = string.Empty;

    public static ApiResponse<T> Ok(int statusCode, string message, T? data, string traceId) => new()
    {
        StatusCode = statusCode, Success = true, Message = message, Data = data, TraceId = traceId
    };

    public static ApiResponse<T> Fail(int statusCode, string message, string traceId, params string[] errors) => new()
    {
        StatusCode = statusCode, Success = false, Message = message, TraceId = traceId, Errors = errors
    };
}
