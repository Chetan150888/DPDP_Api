using DPDP_Api.Data;
using DPDP_Api.Models;
using Microsoft.Data.SqlClient;

namespace DPDP_Api.Middleware;

public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task Invoke(
        HttpContext context,
        Db db)
    {
        try
        {
            await _next(context);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            await WriteResponse(
                context,
                409,
                "A duplicate record already exists.");
        }
        catch (SqlException ex) when (ex.Number == 51000)
        {
            var status = ex.Message.Contains(
                "Access denied",
                StringComparison.OrdinalIgnoreCase)
                ? 403
                : 400;

            var message = status == 403
                ? "Access denied."
                : ex.Message;

            await WriteResponse(
                context,
                status,
                message);
        }
        catch (Exception ex)
        {
            var traceId = context.TraceIdentifier;

            _logger.LogError(
                ex,
                "Unhandled API error. TraceId {TraceId}",
                traceId);

            await LogError(
                context,
                db,
                ex,
                traceId);

            await WriteResponse(
                context,
                500,
                "An unexpected error occurred. Please contact support with the TraceId.");
        }
    }

    private async Task LogError(
        HttpContext context,
        Db db,
        Exception ex,
        string traceId)
    {
        try
        {
            var logDirectory = Path.Combine(
                _environment.ContentRootPath,
                "Logs");

            Directory.CreateDirectory(logDirectory);

            var logLine =
                $"{DateTime.UtcNow:o} | " +
                $"{traceId} | " +
                $"{context.Request.Method} {context.Request.Path} | " +
                $"{ex}{Environment.NewLine}";

            var logFile = Path.Combine(
                logDirectory,
                $"error-{DateTime.UtcNow:yyyy-MM-dd}.log");

            await File.AppendAllTextAsync(
                logFile,
                logLine);

            await db.ExecuteAsync(
                "dbo.log_api_error",
                new[]
                {
                    db.P("@TraceId", traceId),
                    db.P(
                        "@ApiPath",
                        context.Request.Path.Value),
                    db.P(
                        "@HttpMethod",
                        context.Request.Method),
                    db.P(
                        "@UserId",
                        context.User.Identity?.IsAuthenticated == true
                            ? context.User
                                .FindFirst(
                                    System.Security.Claims.ClaimTypes.NameIdentifier)
                                ?.Value
                            : null),
                    db.P(
                        "@OrganisationId",
                        context.User
                            .FindFirst("organisation_id")
                            ?.Value),
                    db.P(
                        "@ExceptionMessage",
                        ex.Message),
                    db.P(
                        "@StackTrace",
                        ex.StackTrace),
                    db.P(
                        "@InnerException",
                        ex.InnerException?.Message),
                    db.P(
                        "@IpAddress",
                        context.Connection
                            .RemoteIpAddress
                            ?.ToString())
                });
        }
        catch
        {
            // Error logging must not replace the original API error.
        }
    }

    private static async Task WriteResponse(
        HttpContext context,
        int statusCode,
        string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(
            ApiResponse<object>.Fail(
                statusCode,
                message,
                context.TraceIdentifier));
    }
}