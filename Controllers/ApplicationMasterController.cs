
using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using DPDP_Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace DPDP_Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/application")]
    public class ApplicationMasterController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ApplicationMasterController(
            IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private SqlConnection CreateConnection()
        {
            string connectionString =
                _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "DefaultConnection is not configured.");

            return new SqlConnection(connectionString);
        }

        private bool TryGetAccess(
            int organisationId,
            out int userId)
        {
            userId = 0;

            var userIdValue =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            var roleCode =
                User.FindFirst("RoleCode")?.Value
                ?? User.FindFirst("role")?.Value;

            var organisationValue =
                User.FindFirst("OrganisationId")?.Value;

            if (!int.TryParse(userIdValue, out userId)
                || userId <= 0)
                return false;

            // Customer tokens cannot access organisation master.
            if (User.FindFirst("token_type")?.Value == "CUSTOMER")
                return false;

            if (roleCode == "SUPER_ADMIN")
                return true;

            return int.TryParse(
                       organisationValue,
                       out int tokenOrganisationId)
                   && tokenOrganisationId == organisationId;
        }

        [HttpPost("save")]
        public async Task<IActionResult> Save(
            [FromBody] ApplicationMasterSaveDto request,
            CancellationToken cancellationToken)
        {
            if (!request.OrganisationId.HasValue ||
                !TryGetAccess(
                    request.OrganisationId.Value,
                    out int userId))
            {
                return StatusCode(403, new
                {
                    statusCode = 403,
                    success = false,
                    message = "Access denied.",
                    data = (object?)null,
                    errors = Array.Empty<string>(),
                    traceId = HttpContext.TraceIdentifier
                });
            }

            await using var connection = CreateConnection();

            await using var command = new SqlCommand(
                "dbo.save_application_master", connection);

            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add(
                "@ApplicationId", SqlDbType.Int).Value =
                (object?)request.ApplicationId ?? DBNull.Value;

            command.Parameters.Add(
                "@OrganisationId", SqlDbType.Int).Value =
                request.OrganisationId.Value;

            command.Parameters.Add(
                "@ApplicationName", SqlDbType.NVarChar, 200).Value =
                request.ApplicationName;

            command.Parameters.Add(
                "@ApplicationDescription", SqlDbType.NVarChar, 500).Value =
                (object?)request.ApplicationDescription ?? DBNull.Value;

            command.Parameters.Add(
                "@IsActive", SqlDbType.Bit).Value =
                (object?)request.IsActive ?? DBNull.Value;

            command.Parameters.Add(
                "@CreatedBy", SqlDbType.Int).Value = userId;

            command.Parameters.Add(
                "@ModifiedBy", SqlDbType.Int).Value = userId;

            await connection.OpenAsync(cancellationToken);

            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException(
                    "Application save returned no result.");

            int applicationId =
                Convert.ToInt32(reader["ApplicationId"]);

            bool success =
                Convert.ToBoolean(reader["Success"]);

            string message =
                Convert.ToString(reader["Message"]) ?? "";

            return Ok(new
            {
                statusCode = 200,
                success,
                message,
                data = new { applicationId },
                errors = Array.Empty<string>(),
                traceId = HttpContext.TraceIdentifier
            });
        }

        [HttpGet("list")]
        public async Task<IActionResult> List(
            [FromQuery] int organisationId,
            [FromQuery] int? applicationId,
            [FromQuery] bool? isActive,
            CancellationToken cancellationToken)
        {
            if (!TryGetAccess(organisationId, out _))
                return Forbid();

            var applications = await GetApplicationsAsync(
                organisationId,
                applicationId,
                isActive,
                cancellationToken);

            return Ok(new
            {
                statusCode = 200,
                success = true,
                message = "Applications fetched successfully.",
                data = applications,
                errors = Array.Empty<string>(),
                traceId = HttpContext.TraceIdentifier
            });
        }

        [HttpGet("{applicationId:int}")]
        public async Task<IActionResult> GetById(
            int applicationId,
            [FromQuery] int organisationId,
            CancellationToken cancellationToken)
        {
            if (!TryGetAccess(organisationId, out _))
                return Forbid();

            if (applicationId <= 0)
                return BadRequest(new
                {
                    message = "Valid application id is required."
                });

            var applications = await GetApplicationsAsync(
                organisationId,
                applicationId,
                null,
                cancellationToken);

            if (applications.Count == 0)
                return NotFound(new
                {
                    statusCode = 404,
                    success = false,
                    message = "Application not found.",
                    data = (object?)null,
                    errors = Array.Empty<string>(),
                    traceId = HttpContext.TraceIdentifier
                });

            return Ok(new
            {
                statusCode = 200,
                success = true,
                message = "Application fetched successfully.",
                data = applications[0],
                errors = Array.Empty<string>(),
                traceId = HttpContext.TraceIdentifier
            });
        }

        private async Task<List<ApplicationMasterListDto>>
            GetApplicationsAsync(
                int organisationId,
                int? applicationId,
                bool? isActive,
                CancellationToken cancellationToken)
        {
            var applications =
                new List<ApplicationMasterListDto>();

            await using var connection = CreateConnection();

            await using var command = new SqlCommand(
                "dbo.list_application_master", connection);

            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add(
                "@OrganisationId", SqlDbType.Int).Value =
                organisationId;

            command.Parameters.Add(
                "@ApplicationId", SqlDbType.Int).Value =
                (object?)applicationId ?? DBNull.Value;

            command.Parameters.Add(
                "@IsActive", SqlDbType.Bit).Value =
                (object?)isActive ?? DBNull.Value;

            await connection.OpenAsync(cancellationToken);

            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                applications.Add(new ApplicationMasterListDto
                {
                    ApplicationId =
                        Convert.ToInt32(reader["ApplicationId"]),

                    OrganisationId =
                        Convert.ToInt32(reader["OrganisationId"]),

                    ApplicationName =
                        Convert.ToString(reader["ApplicationName"]) ?? "",

                    ApplicationDescription =
                        reader["ApplicationDescription"] == DBNull.Value
                            ? null
                            : Convert.ToString(reader["ApplicationDescription"]),

                    IsActive =
                        Convert.ToBoolean(reader["IsActive"]),

                    IsDeleted =
                        Convert.ToBoolean(reader["IsDeleted"]),

                    CreatedBy =
                        Convert.ToInt32(reader["CreatedBy"]),

                    CreatedDate =
                        Convert.ToDateTime(reader["CreatedDate"])
                            .ToString("MM/dd/yyyy"),

                    ModifiedBy =
                        reader["ModifiedBy"] == DBNull.Value
                            ? null
                            : Convert.ToInt32(reader["ModifiedBy"]),

                    ModifiedDate =
                        reader["ModifiedDate"] == DBNull.Value
                            ? null
                            : Convert.ToDateTime(reader["ModifiedDate"])
                                .ToString("MM/dd/yyyy")
                });
            }

            return applications;
        }
    }
}
