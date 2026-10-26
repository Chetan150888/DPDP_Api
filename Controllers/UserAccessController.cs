
using System.Data;
using System.Security.Claims;
using DPDP_Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace DPDP_Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/access")]
    public class UserAccessController : ControllerBase
    {
        private readonly string _connectionString;

        public UserAccessController(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Database connection string missing.");
        }

        [HttpGet("my-permissions")]
        public async Task<IActionResult> GetMyPermissions()
        {
            string? userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            string? sessionIdValue =
                User.FindFirstValue("session_id");

            if (!int.TryParse(userIdValue, out int userId)
                || !Guid.TryParse(sessionIdValue, out Guid sessionId))
            {
                return Unauthorized(new
                {
                    statusCode = 401,
                    success = false,
                    message = "Invalid user session.",
                    data = (object?)null,
                    errors = Array.Empty<string>(),
                    traceId = HttpContext.TraceIdentifier
                });
            }

            var result = new UserAccessDto
            {
                UserId = userId,
                Roles = new List<UserAccessRoleDto>(),
                Permissions = new List<UserAccessPermissionDto>()
            };

            await using var connection =
                new SqlConnection(_connectionString);

            await connection.OpenAsync();

            // Validate the JWT session against the database.
            // Existing SessionValidationMiddleware remains unchanged.
            const string sessionSql = @"
                SELECT COUNT(1)
                FROM dbo.UserSession
                WHERE SessionId = @SessionId
                  AND UserId = @UserId
                  AND IsRevoked = 0
                  AND RefreshExpiresAt > SYSDATETIME();";

            await using (var sessionCommand =
                new SqlCommand(sessionSql, connection))
            {
                sessionCommand.Parameters.Add(
                    "@SessionId", SqlDbType.UniqueIdentifier
                ).Value = sessionId;

                sessionCommand.Parameters.Add(
                    "@UserId", SqlDbType.Int
                ).Value = userId;

                var count = Convert.ToInt32(
                    await sessionCommand.ExecuteScalarAsync());

                if (count == 0)
                {
                    return Unauthorized(new
                    {
                        statusCode = 401,
                        success = false,
                        message = "Session expired or revoked.",
                        data = (object?)null,
                        errors = Array.Empty<string>(),
                        traceId = HttpContext.TraceIdentifier
                    });
                }
            }

            await using var command =
                new SqlCommand(
                    "dbo.usp_CMP_UserAccess_Get",
                    connection);

            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add(
                "@UserId", SqlDbType.Int
            ).Value = userId;

            await using var reader =
                await command.ExecuteReaderAsync();

            // Result set 1: User access information
            if (await reader.ReadAsync())
            {
                result.UserId =
                    Convert.ToInt32(reader["UserId"]);

                result.OrganisationId =
                    reader["OrganisationId"] == DBNull.Value
                        ? null
                        : Convert.ToInt32(reader["OrganisationId"]);

                result.IsSuperAdmin =
                    Convert.ToBoolean(reader["IsSuperAdmin"]);

                result.IsOrgAdmin =
                    Convert.ToBoolean(reader["IsOrgAdmin"]);
            }
            else
            {
                return NotFound(new
                {
                    statusCode = 404,
                    success = false,
                    message = "Active user not found.",
                    data = (object?)null,
                    errors = Array.Empty<string>(),
                    traceId = HttpContext.TraceIdentifier
                });
            }

            // Result set 2: User roles
            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    result.Roles.Add(new UserAccessRoleDto
                    {
                        RoleId =
                            Convert.ToInt32(reader["RoleId"]),

                        RoleCode =
                            Convert.ToString(reader["RoleCode"])
                            ?? string.Empty,

                        RoleName =
                            Convert.ToString(reader["RoleName"])
                            ?? string.Empty
                    });
                }
            }

            // Result set 3: Combined permissions
            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    result.Permissions.Add(
                        new UserAccessPermissionDto
                        {
                            PermissionId =
                                Convert.ToInt32(
                                    reader["PermissionId"]),

                            PermissionCode =
                                Convert.ToString(
                                    reader["PermissionCode"])
                                ?? string.Empty,

                            PermissionName =
                                Convert.ToString(
                                    reader["PermissionName"])
                                ?? string.Empty,

                            ModuleCode =
                                Convert.ToString(
                                    reader["ModuleCode"])
                                ?? string.Empty
                        });
                }
            }

            return Ok(new
            {
                statusCode = 200,
                success = true,
                message = "User access retrieved successfully.",
                data = result,
                errors = Array.Empty<string>(),
                traceId = HttpContext.TraceIdentifier
            });
        }
    }
}
