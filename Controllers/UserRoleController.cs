
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
    [Route("api/user-role")]
    public class UserRoleController : ControllerBase
    {
        private readonly string _connectionString;

        public UserRoleController(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Database connection string missing.");
        }

        [HttpPost("save")]
        public async Task<IActionResult> Save(
            [FromBody] UserRoleSaveDto request)
        {
            if (request == null)
            {
                return BadRequest(ApiResponse(
                    400, false, "Request body is required."));
            }

            string action =
                request.Action?.Trim().ToUpperInvariant()
                ?? string.Empty;

            if (action != "ADD" && action != "REMOVE")
            {
                return BadRequest(ApiResponse(
                    400, false,
                    "Action must be ADD or REMOVE."));
            }

            if (request.UserId <= 0 ||
                request.OrganisationId <= 0 ||
                !request.RoleId.HasValue ||
                request.RoleId.Value <= 0)
            {
                return BadRequest(ApiResponse(
                    400, false,
                    "Valid UserId, OrganisationId and RoleId are required."));
            }

            return await ExecuteAsync(request, action);
        }

        [HttpGet("list")]
        public async Task<IActionResult> List(
            [FromQuery] int userId,
            [FromQuery] int organisationId)
        {
            if (userId <= 0 || organisationId <= 0)
            {
                return BadRequest(ApiResponse(
                    400, false,
                    "Invalid user or organisation."));
            }

            return await ExecuteAsync(
                new UserRoleSaveDto
                {
                    UserId = userId,
                    OrganisationId = organisationId
                },
                "LIST");
        }

        private async Task<IActionResult> ExecuteAsync(
            UserRoleSaveDto request,
            string action)
        {
            // Actual JWT claims
            string? actorIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            string? sessionIdValue =
                User.FindFirstValue("session_id");

            string? roleCode =
                User.FindFirstValue(ClaimTypes.Role)
                ?? User.FindFirstValue("role");

            string? orgValue =
                User.FindFirstValue("organisation_id");

            if (!int.TryParse(actorIdValue, out int actorUserId)
                || !Guid.TryParse(sessionIdValue, out Guid sessionId))
            {
                return Unauthorized(ApiResponse(
                    401, false, "Invalid user session."));
            }

            bool isSuperAdmin =
                roleCode == "SUPER_ADMIN";

            bool isOrgAdmin =
                roleCode == "ORG_ADMIN"
                && int.TryParse(orgValue, out int actorOrgId)
                && actorOrgId == request.OrganisationId;

            if (!isSuperAdmin && !isOrgAdmin)
            {
                return StatusCode(
                    403,
                    ApiResponse(
                        403, false, "Access denied."));
            }

            await using var con =
                new SqlConnection(_connectionString);

            await using var cmd =
                new SqlCommand(
                    "dbo.usp_CMP_UserRole_Save", con);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(
                "@Action", SqlDbType.VarChar, 10
            ).Value = action;

            cmd.Parameters.Add(
                "@UserId", SqlDbType.Int
            ).Value = request.UserId;

            cmd.Parameters.Add(
                "@RoleId", SqlDbType.Int
            ).Value =
                (object?)request.RoleId ?? DBNull.Value;

            cmd.Parameters.Add(
                "@OrganisationId", SqlDbType.Int
            ).Value = request.OrganisationId;

            cmd.Parameters.Add(
                "@CreatedBy", SqlDbType.Int
            ).Value = actorUserId;

            cmd.Parameters.Add(
                "@SessionId", SqlDbType.UniqueIdentifier
            ).Value = sessionId;

            await con.OpenAsync();

            await using var reader =
                await cmd.ExecuteReaderAsync();

            if (action == "LIST")
            {
                var roles = new List<object>();

                while (await reader.ReadAsync())
                {
                    roles.Add(new
                    {
                        userRoleMappingId =
                            Convert.ToInt64(
                                reader["UserRoleMappingId"]),

                        userId =
                            Convert.ToInt32(reader["UserId"]),

                        organisationId =
                            Convert.ToInt32(
                                reader["OrganisationId"]),

                        roleId =
                            Convert.ToInt32(reader["RoleId"]),

                        roleCode =
                            Convert.ToString(
                                reader["RoleCode"])
                            ?? string.Empty,

                        roleName =
                            Convert.ToString(
                                reader["RoleName"])
                            ?? string.Empty
                    });
                }

                return Ok(ApiResponse(
                    200,
                    true,
                    "User roles fetched successfully.",
                    roles));
            }

            // Consume the stored procedure result.
            // The SP is responsible for validation and saving.
            if (!await reader.ReadAsync())
            {
                throw new InvalidOperationException(
                    "User role save returned no result.");
            }

            return Ok(ApiResponse(
                200,
                true,
                action == "ADD"
                    ? "User role assigned successfully."
                    : "User role removed successfully."));
        }

        private object ApiResponse(
            int statusCode,
            bool success,
            string message,
            object? data = null)
        {
            return new
            {
                statusCode,
                success,
                message,
                data,
                errors = Array.Empty<string>(),
                traceId = HttpContext.TraceIdentifier
            };
        }
    }
}
