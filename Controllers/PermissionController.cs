
using DPDP_Api.DTOs;
using DPDP_CMP.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;

namespace DPDP_Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/permission")]
    public class PermissionController : ControllerBase
    {
        private readonly string _connectionString;

        public PermissionController(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Database connection string missing.");
        }

        // POST: api/permission/save
        [HttpPost("save")]
        public async Task<IActionResult> Save(
            [FromBody] PermissionSaveDto request)
        {
            if (!TryGetAccess(
                request.OrganisationId, out string error))
            {
                return StatusCode(403, new
                {
                    success = false,
                    message = error
                });
            }

            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                "dbo.usp_CMP_PermissionMaster", con);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@Action", SqlDbType.VarChar, 10)
                .Value = "SAVE";

            cmd.Parameters.Add("@OrganisationId", SqlDbType.Int)
                .Value = request.OrganisationId;

            cmd.Parameters.Add("@PermissionCode", SqlDbType.VarChar, 100)
                .Value = request.PermissionCode.Trim();

            cmd.Parameters.Add("@PermissionName", SqlDbType.NVarChar, 200)
                .Value = request.PermissionName.Trim();

            cmd.Parameters.Add("@ModuleCode", SqlDbType.VarChar, 100)
                .Value = request.ModuleCode.Trim();

            await con.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                throw new InvalidOperationException(
                    "Permission save returned no result.");

            int permissionId =
                Convert.ToInt32(reader["PermissionId"]);

            return Ok(new
            {
                success = true,
                message = "Permission created successfully.",
                data = new { permissionId }
            });
        }

        // GET: api/permission/list?organisationId=1001
        [HttpGet("list")]
        public async Task<IActionResult> List(
            [FromQuery] int organisationId)
        {
            if (organisationId <= 0)
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid organisation."
                });

            if (!TryGetAccess(organisationId, out string error))
            {
                return StatusCode(403, new
                {
                    success = false,
                    message = error
                });
            }

            var permissions = new List<PermissionListDto>();

            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                "dbo.usp_CMP_PermissionMaster", con);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@Action", SqlDbType.VarChar, 10)
                .Value = "LIST";

            cmd.Parameters.Add("@OrganisationId", SqlDbType.Int)
                .Value = organisationId;

            await con.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                permissions.Add(new PermissionListDto
                {
                    PermissionId =
                        Convert.ToInt32(reader["PermissionId"]),

                    OrganisationId =
                        reader["OrganisationId"] == DBNull.Value
                            ? null
                            : Convert.ToInt32(reader["OrganisationId"]),

                    PermissionCode =
                        reader["PermissionCode"].ToString() ?? "",

                    ModuleCode =
                        reader["ModuleCode"].ToString() ?? "",

                    PermissionName =
                        reader["PermissionName"].ToString() ?? "",

                    IsActive =
                        Convert.ToBoolean(reader["IsActive"])
                });
            }

            return Ok(new
            {
                success = true,
                message = "Permissions fetched successfully.",
                data = permissions
            });
        }


        private bool TryGetAccess(int organisationId, out string error)
        {
            error = "Access denied.";

            if (organisationId <= 0)
                return false;

            // Read role from JWT
            string? role =
                User.FindFirstValue(ClaimTypes.Role)
                ?? User.FindFirstValue("role")
                ?? User.FindFirstValue("RoleCode");

            // Read organisation from JWT
            string? orgValue =
                User.FindFirstValue("organisation_id")
                ?? User.FindFirstValue("OrganisationId");

            // Super Admin can access all organisations
            if (role == "SUPER_ADMIN")
                return true;

            // Organisation Admin can access own organisation
            if (role == "ORG_ADMIN"
                && int.TryParse(orgValue, out int userOrgId)
                && userOrgId == organisationId)
            {
                return true;
            }

            return false;
        }

    }
}
