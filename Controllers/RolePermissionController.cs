
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
    [Route("api/role-permission")]
    public class RolePermissionController : ControllerBase
    {
        private readonly string _connectionString;

        public RolePermissionController(
            IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Database connection string missing.");
        }

        [HttpPost("save")]
        public async Task<IActionResult> Save(
            [FromBody] RolePermissionSaveDto request)
        {
            if (request == null)
            {
                return BadRequest(ApiResponse(
                    400, false, "Request body is required."));
            }

            // Read authenticated JWT claims
            string? roleCode =
                User.FindFirstValue(ClaimTypes.Role)
                ?? User.FindFirstValue("role");

            string? orgValue =
                User.FindFirstValue("organisation_id");

            string? userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            string? sessionValue =
                User.FindFirstValue("session_id");

            if (!int.TryParse(userIdValue, out int actorUserId)
                || !Guid.TryParse(sessionValue, out Guid sessionId))
            {
                return Unauthorized(ApiResponse(
                    401, false, "Invalid user session."));
            }

            if (request.OrganisationId <= 0 ||
                request.RoleId <= 0 ||
                request.PermissionId <= 0)
            {
                return BadRequest(ApiResponse(
                    400, false,
                    "Valid OrganisationId, RoleId and PermissionId are required."));
            }

            bool isSuperAdmin =
                roleCode == "SUPER_ADMIN";

            bool isOrgAdmin =
                roleCode == "ORG_ADMIN"
                && int.TryParse(orgValue, out int actorOrgId)
                && actorOrgId == request.OrganisationId;

            if (!isSuperAdmin && !isOrgAdmin)
            {
                return StatusCode(403, ApiResponse(
                    403, false, "Access denied."));
            }

            await using var con =
                new SqlConnection(_connectionString);

            await con.OpenAsync();

            // Revalidate actor and organisation against DB.
            // Do not trust JWT role claims alone.
            const string authorizationSql = @"
                SELECT COUNT(1)
                FROM dbo.AppUser AU
                INNER JOIN dbo.RoleMaster RM
                    ON RM.RoleId = AU.RoleId
                INNER JOIN dbo.UserSession US
                    ON US.UserId = AU.UserId
                WHERE AU.UserId = @ActorUserId
                  AND AU.IsActive = 1
                  AND AU.IsDeleted = 0
                  AND RM.IsActive = 1
                  AND US.SessionId = @SessionId
                  AND US.IsRevoked = 0
                  AND US.RefreshExpiresAt > SYSDATETIME()
                  AND (
                        RM.RoleCode = 'SUPER_ADMIN'
                        OR (
                            RM.RoleCode = 'ORG_ADMIN'
                            AND AU.OrganisationId = @OrganisationId
                        )
                  );";

            await using (var authCmd =
                new SqlCommand(authorizationSql, con))
            {
                authCmd.Parameters.Add(
                    "@ActorUserId", SqlDbType.Int
                ).Value = actorUserId;

                authCmd.Parameters.Add(
                    "@SessionId", SqlDbType.UniqueIdentifier
                ).Value = sessionId;

                authCmd.Parameters.Add(
                    "@OrganisationId", SqlDbType.Int
                ).Value = request.OrganisationId;

                int authorized = Convert.ToInt32(
                    await authCmd.ExecuteScalarAsync());

                if (authorized == 0)
                {
                    return StatusCode(403, ApiResponse(
                        403, false,
                        "Access denied or session invalid."));
                }
            }

            // Save role permission using existing SP
            await using var cmd = new SqlCommand(
                "dbo.usp_CMP_RolePermission_Save", con);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(
                "@OrganisationId", SqlDbType.Int
            ).Value = request.OrganisationId;

            cmd.Parameters.Add(
                "@RoleId", SqlDbType.Int
            ).Value = request.RoleId;

            cmd.Parameters.Add(
                "@PermissionId", SqlDbType.Int
            ).Value = request.PermissionId;

            cmd.Parameters.Add(
                "@IsGranted", SqlDbType.Bit
            ).Value = request.IsGranted;

            cmd.Parameters.Add(
                "@CreatedBy", SqlDbType.Int
            ).Value = actorUserId;

            await cmd.ExecuteNonQueryAsync();

            return Ok(ApiResponse(
                200,
                true,
                "Role permission updated successfully."));
        }

        private object ApiResponse(
            int statusCode,
            bool success,
            string message)
        {
            return new
            {
                statusCode,
                success,
                message,
                data = (object?)null,
                errors = Array.Empty<string>(),
                traceId = HttpContext.TraceIdentifier
            };
        }
    }
}
