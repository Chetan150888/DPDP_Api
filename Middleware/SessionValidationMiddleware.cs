
using System;
using System.Data;
using System.Security.Claims;
using System.Threading.Tasks;
using DPDP_Api.Data;
using DPDP_Api.Models;
using Microsoft.Data.SqlClient;

namespace DPDP_Api.Middleware
{
    public sealed class SessionValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(
            HttpContext context,
            Db db,
            IConfiguration configuration)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                string? sessionId = context.User
                    .FindFirst("session_id")?.Value;

                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    await WriteUnauthorizedResponse(context);
                    return;
                }

                string? tokenType = context.User
                    .FindFirst("token_type")?.Value;

                // CUSTOMER JWT:
                // Validate against CMP_CustomerSession.
                if (tokenType == "CUSTOMER")
                {
                    string? email = context.User
                        .FindFirst(ClaimTypes.Email)?.Value
                        ?? context.User.FindFirst("email")?.Value;

                    if (string.IsNullOrWhiteSpace(email) ||
                        !Guid.TryParse(sessionId, out Guid customerSessionId))
                    {
                        await WriteUnauthorizedResponse(context);
                        return;
                    }

                    bool isValidCustomerSession =
                        await ValidateCustomerSessionAsync(
                            configuration,
                            customerSessionId,
                            email,
                            context.RequestAborted);

                    if (!isValidCustomerSession)
                    {
                        await WriteUnauthorizedResponse(context);
                        return;
                    }
                }
                else
                {
                    // EMPLOYEE JWT:
                    // Existing validation remains unchanged.
                    var isValidSession = await db.ScalarIntAsync(
                        "dbo.validate_session",
                        new[]
                        {
                            db.P("@SessionId", sessionId)
                        },
                        context.RequestAborted);

                    if (isValidSession != 1)
                    {
                        await WriteUnauthorizedResponse(context);
                        return;
                    }
                }
            }

            await _next(context);
        }

        private static async Task<bool> ValidateCustomerSessionAsync(
            IConfiguration configuration,
            Guid sessionId,
            string email,
            CancellationToken cancellationToken)
        {
            string connectionString =
                configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "DefaultConnection is missing.");

            await using var connection =
                new SqlConnection(connectionString);

            await using var command = new SqlCommand(
                "dbo.usp_CMP_CustomerSession_Validate",
                connection);

            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add(
                "@SessionId",
                SqlDbType.UniqueIdentifier).Value = sessionId;

            command.Parameters.Add(
                "@Email",
                SqlDbType.NVarChar,
                254).Value = email.Trim().ToLowerInvariant();

            await connection.OpenAsync(cancellationToken);

            object? result =
                await command.ExecuteScalarAsync(cancellationToken);

            return result != null &&
                   result != DBNull.Value &&
                   Convert.ToInt32(result) == 1;
        }

        private static async Task WriteUnauthorizedResponse(
            HttpContext context)
        {
            context.Response.StatusCode =
                StatusCodes.Status401Unauthorized;

            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(
                ApiResponse<object>.Fail(
                    401,
                    "Authentication session is invalid or expired.",
                    context.TraceIdentifier));
        }
    }
}
