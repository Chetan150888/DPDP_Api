
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
    [Route("api/consent")]
    public class ConsentDashboardController : ControllerBase
    {
        private readonly string _connectionString;

        public ConsentDashboardController(
            IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Database connection string missing.");
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard(
            [FromQuery] int organisationId,
            [FromQuery] string viewType = "MY",
            [FromQuery] string? status = null,
            [FromQuery] string? search = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            string? userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            string? roleCode =
                User.FindFirstValue(ClaimTypes.Role)
                ?? User.FindFirstValue("role");

            string? orgValue =
                User.FindFirstValue("organisation_id");

            string? sessionValue =
                User.FindFirstValue("session_id");

            if (!int.TryParse(userIdValue, out int userId)
                || !Guid.TryParse(sessionValue, out _))
            {
                return Unauthorized(ApiResponse(
                    401, false, "Invalid user session."));
            }

            if (organisationId <= 0 ||
                pageNumber < 1 ||
                pageSize < 1 || pageSize > 100)
            {
                return BadRequest(ApiResponse(
                    400, false, "Invalid dashboard parameters."));
            }

            viewType = (viewType ?? "MY")
                .Trim().ToUpperInvariant();

            if (viewType != "MY" &&
                viewType != "TEAM" &&
                viewType != "ALL")
            {
                return BadRequest(ApiResponse(
                    400, false, "Invalid view type."));
            }

            bool isSuperAdmin = roleCode == "SUPER_ADMIN";
            bool isOrgAdmin = roleCode == "ORG_ADMIN";

            if (!isSuperAdmin &&
                (!int.TryParse(orgValue, out int actorOrgId)
                 || actorOrgId != organisationId))
            {
                return StatusCode(403, ApiResponse(
                    403, false, "Organisation access denied."));
            }

            if (viewType != "MY" &&
                !isSuperAdmin && !isOrgAdmin)
            {
                return StatusCode(403, ApiResponse(
                    403, false, "Dashboard view access denied."));
            }

            var data = new ConsentDashboardDto
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            await using var connection =
                new SqlConnection(_connectionString);

            await connection.OpenAsync();

            // 1. Ticket dashboard SP validates module access.
            await using (var command = new SqlCommand(
                "dbo.usp_CMP_ConsentDashboard_Get",
                connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(
                    "@OrganisationId", SqlDbType.Int
                ).Value = organisationId;

                command.Parameters.Add(
                    "@UserId", SqlDbType.Int
                ).Value = userId;

                command.Parameters.Add(
                    "@ViewType", SqlDbType.VarChar, 20
                ).Value = viewType;

                command.Parameters.Add(
                    "@Status", SqlDbType.VarChar, 30
                ).Value = string.IsNullOrWhiteSpace(status)
                    ? DBNull.Value
                    : status.Trim().ToUpperInvariant();

                command.Parameters.Add(
                    "@Search", SqlDbType.NVarChar, 100
                ).Value = string.IsNullOrWhiteSpace(search)
                    ? DBNull.Value
                    : search.Trim();

                command.Parameters.Add(
                    "@PageNumber", SqlDbType.Int
                ).Value = pageNumber;

                command.Parameters.Add(
                    "@PageSize", SqlDbType.Int
                ).Value = pageSize;

                await using var reader =
                    await command.ExecuteReaderAsync();

                // Result 1: Ticket summary
                if (!await reader.ReadAsync())
                {
                    throw new InvalidOperationException(
                        "Ticket summary not returned.");
                }

                data.TicketSummary = new ConsentDashboardSummaryDto
                {
                    TotalCases =
                        Convert.ToInt64(reader["TotalCases"]),

                    Unassigned =
                        Convert.ToInt64(reader["Unassigned"]),

                    InProgress =
                        Convert.ToInt64(reader["InProgress"]),

                    PendingApproval =
                        Convert.ToInt64(reader["PendingApproval"]),

                    Approved =
                        Convert.ToInt64(reader["Approved"]),

                    Rejected =
                        Convert.ToInt64(reader["Rejected"]),

                    Closed =
                        Convert.ToInt64(reader["Closed"])
                };

                // Result 2: Total ticket records
                if (!await reader.NextResultAsync() ||
                    !await reader.ReadAsync())
                {
                    throw new InvalidOperationException(
                        "Total records not returned.");
                }

                data.TotalRecords =
                    Convert.ToInt64(reader["TotalRecords"]);

                // Result 3: Ticket list
                if (!await reader.NextResultAsync())
                {
                    throw new InvalidOperationException(
                        "Ticket list not returned.");
                }

                while (await reader.ReadAsync())
                {
                    data.Tickets.Add(
                        new ConsentDashboardTicketDto
                        {
                            TicketId =
                                Convert.ToInt64(reader["TicketId"]),

                            TicketNo =
                                reader["TicketNo"] == DBNull.Value
                                    ? null
                                    : Convert.ToString(reader["TicketNo"]),

                            OrganisationId =
                                Convert.ToInt32(
                                    reader["OrganisationId"]),

                            ConsentId =
                                Convert.ToInt64(reader["ConsentId"]),

                            TicketType =
                                Convert.ToString(reader["TicketType"])
                                ?? "",

                            TicketStatus =
                                Convert.ToString(reader["TicketStatus"])
                                ?? "",

                            Priority =
                                Convert.ToString(reader["Priority"])
                                ?? "",

                            Subject =
                                Convert.ToString(reader["Subject"])
                                ?? "",

                            AssignedTo =
                                reader["AssignedTo"] == DBNull.Value
                                    ? null
                                    : Convert.ToInt32(reader["AssignedTo"]),

                            CreatedBy =
                                Convert.ToInt32(reader["CreatedBy"]),

                            CreatedDate =
                                Convert.ToDateTime(
                                    reader["CreatedDate"]),

                            ModifiedDate =
                                reader["ModifiedDate"] == DBNull.Value
                                    ? null
                                    : Convert.ToDateTime(
                                        reader["ModifiedDate"])
                        });
                }
            }

            // 2. Original consent summary from ConsentMain
            await using (var command = new SqlCommand(
                "dbo.ConsentDashboard_Summary",
                connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(
                    "@OrganisationId", SqlDbType.Int
                ).Value = organisationId;

                await using var reader =
                    await command.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    throw new InvalidOperationException(
                        "Consent summary not returned.");
                }

                data.ConsentSummary = new ConsentSummaryDto
                {
                    TotalConsents =
                        Convert.ToInt64(reader["TotalConsents"]),

                    ActiveConsents =
                        Convert.ToInt64(reader["ActiveConsents"]),

                    ExpiredConsents =
                        Convert.ToInt64(reader["ExpiredConsents"]),

                    WithdrawnConsents =
                        Convert.ToInt64(reader["WithdrawnConsents"]),

                    PendingConsents =
                        Convert.ToInt64(reader["PendingConsents"]),

                    RejectedConsents =
                        Convert.ToInt64(reader["RejectedConsents"]),

                    ClosedConsents =
                        Convert.ToInt64(reader["ClosedConsents"])
                };
            }

            return Ok(ApiResponse(
                200,
                true,
                "Consent dashboard retrieved successfully.",
                data));
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
