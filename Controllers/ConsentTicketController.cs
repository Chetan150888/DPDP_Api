
using System;
using System.Data;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using DPDP_Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace DPDP_Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/consent/ticket")]
    public class ConsentTicketController : ControllerBase
    {
        private readonly string _connectionString;

        public ConsentTicketController(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Database connection string missing.");
        }

        // ==========================================
        // 1. CREATE TICKET - EXISTING API
        // ==========================================
        [HttpPost("create")]
        public async Task<IActionResult> Create(
            [FromBody] ConsentTicketCreateDto request)
        {
            if (request == null)
                return BadRequest(ResponseBody(
                    400, false, "Request body is required."));

            string ticketType =
                (request.TicketType ?? "").Trim().ToUpperInvariant();

            string priority =
                (request.Priority ?? "").Trim().ToUpperInvariant();

            if (ticketType != "MODIFY" &&
                ticketType != "WITHDRAW")
                return BadRequest(ResponseBody(
                    400, false, "Invalid ticket type."));

            if (priority != "LOW" &&
                priority != "MEDIUM" &&
                priority != "HIGH")
                return BadRequest(ResponseBody(
                    400, false, "Invalid priority."));

            if (!TryGetActor(request.OrganisationId,
                out int actorUserId, out int errorCode))
                return AccessError(errorCode);

            await using var con =
                new SqlConnection(_connectionString);

            await using var cmd = new SqlCommand(
                "dbo.usp_CMP_ConsentTicket_Create", con);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@OrganisationId", SqlDbType.Int)
                .Value = request.OrganisationId;

            cmd.Parameters.Add("@ConsentId", SqlDbType.BigInt)
                .Value = request.ConsentId;

            cmd.Parameters.Add("@TicketType", SqlDbType.VarChar, 20)
                .Value = ticketType;

            cmd.Parameters.Add("@Priority", SqlDbType.VarChar, 10)
                .Value = priority;

            cmd.Parameters.Add("@Subject", SqlDbType.NVarChar, 250)
                .Value = request.Subject.Trim();

            cmd.Parameters.Add("@Description", SqlDbType.NVarChar, -1)
                .Value = (object?)request.Description ?? DBNull.Value;

            cmd.Parameters.Add("@CreatedBy", SqlDbType.Int)
                .Value = actorUserId;

            await con.OpenAsync();

            await using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                throw new InvalidOperationException(
                    "Ticket creation returned no result.");

            return Ok(ResponseBody(
                200,
                true,
                Convert.ToString(reader["Message"])
                    ?? "Ticket created successfully.",
                new
                {
                    ticketId = Convert.ToInt64(reader["TicketId"]),
                    ticketNo = Convert.ToString(reader["TicketNo"]),
                    ticketStatus =
                        Convert.ToString(reader["TicketStatus"])
                }));
        }

        // ==========================================
        // 2. ASSIGN TICKET - EXISTING API
        // ==========================================
        [HttpPost("assign")]
        public async Task<IActionResult> Assign(
            [FromBody] ConsentTicketAssignDto request)
        {
            if (request == null)
                return BadRequest(ResponseBody(
                    400, false, "Request body is required."));

            if (!TryGetActor(request.OrganisationId,
                out int actorUserId, out int errorCode))
                return AccessError(errorCode);

            await using var con =
                new SqlConnection(_connectionString);

            await using var cmd = new SqlCommand(
                "dbo.usp_CMP_ConsentTicket_Assign", con);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@OrganisationId", SqlDbType.Int)
                .Value = request.OrganisationId;

            cmd.Parameters.Add("@TicketId", SqlDbType.BigInt)
                .Value = request.TicketId;

            cmd.Parameters.Add("@AssignedTo", SqlDbType.Int)
                .Value = request.AssignedTo;

            cmd.Parameters.Add("@AssignedBy", SqlDbType.Int)
                .Value = actorUserId;

            cmd.Parameters.Add("@Remarks", SqlDbType.NVarChar, 1000)
                .Value = (object?)request.Remarks ?? DBNull.Value;

            await con.OpenAsync();

            await using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                throw new InvalidOperationException(
                    "Ticket assignment returned no result.");

            return Ok(ResponseBody(
                200,
                true,
                Convert.ToString(reader["Message"])
                    ?? "Ticket assignment saved successfully.",
                new
                {
                    ticketId = Convert.ToInt64(reader["TicketId"]),
                    assignedTo = Convert.ToInt32(reader["AssignedTo"]),
                    ticketStatus =
                        Convert.ToString(reader["TicketStatus"]),
                    actionType =
                        Convert.ToString(reader["ActionType"])
                }));
        }

        // ==========================================
        // 3. ORGANISATION CONSENT REQUESTS - NEW
        // ==========================================
        [HttpGet("requests")]
        public async Task<IActionResult> GetConsentRequests(
            [FromQuery] int organisationId,
            [FromQuery] string? requestStatus = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            if (!TryGetActor(
                organisationId,
                out int actorUserId,
                out int errorCode))
            {
                return AccessError(errorCode);
            }

            if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest(ResponseBody(
                    400,
                    false,
                    "Invalid pagination parameters."));
            }

            string? status = string.IsNullOrWhiteSpace(requestStatus)
                ? null
                : requestStatus.Trim().ToUpperInvariant();

            await using var con =
                new SqlConnection(_connectionString);

            await using var cmd = new SqlCommand(
                "dbo.usp_CMP_OrgConsentRequests_List", con);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@OrganisationId", SqlDbType.Int)
                .Value = organisationId;

            // Authenticated employee ID from JWT
            cmd.Parameters.Add("@UserId", SqlDbType.Int)
                .Value = actorUserId;

            cmd.Parameters.Add("@RequestStatus", SqlDbType.VarChar, 30)
                .Value = (object?)status ?? DBNull.Value;

            cmd.Parameters.Add("@PageNumber", SqlDbType.Int)
                .Value = pageNumber;

            cmd.Parameters.Add("@PageSize", SqlDbType.Int)
                .Value = pageSize;

            await con.OpenAsync(HttpContext.RequestAborted);

            await using var reader = await cmd.ExecuteReaderAsync(
                HttpContext.RequestAborted);

            long totalRecords = 0;

            // First result set: TotalRecords
            if (await reader.ReadAsync(HttpContext.RequestAborted))
            {
                totalRecords = Convert.ToInt64(
                    reader["TotalRecords"]);
            }

            var requests = new List<object>();

            // Second result set: Requests
            if (await reader.NextResultAsync(
                HttpContext.RequestAborted))
            {
                while (await reader.ReadAsync(
                    HttpContext.RequestAborted))
                {
                    requests.Add(new
                    {
                        changeRequestId =
                            Convert.ToInt64(reader["ChangeRequestId"]),

                        ticketId =
                            Convert.ToInt64(reader["TicketId"]),

                        ticketNo =
                            DbString(reader, "TicketNo"),

                        organisationId =
                            Convert.ToInt32(reader["OrganisationId"]),

                        consentId =
                            Convert.ToInt64(reader["ConsentId"]),

                        requestType =
                            DbString(reader, "RequestType"),

                        requestStatus =
                            DbString(reader, "RequestStatus"),

                        ticketStatus =
                            DbString(reader, "TicketStatus"),

                        priority =
                            DbString(reader, "Priority"),

                        subject =
                            DbString(reader, "Subject"),

                        reason =
                            DbString(reader, "Reason"),

                        proposedChangesJson =
                            DbString(reader, "ProposedChangesJson"),

                        assignedTo =
                            DbNullableInt(reader, "AssignedTo"),

                        assignedBy =
                            DbNullableInt(reader, "AssignedBy"),

                        assignedDate =
                            DbDate(reader, "AssignedDate"),

                        requestedDate =
                            DbDate(reader, "RequestedDate"),

                        decisionBy =
                            DbNullableInt(reader, "DecisionBy"),

                        decisionDate =
                            DbDate(reader, "DecisionDate"),

                        decisionRemarks =
                            DbString(reader, "DecisionRemarks"),

                        appliedDate =
                            DbDate(reader, "AppliedDate")
                    });
                }
            }

            return Ok(ResponseBody(
                200,
                true,
                "Consent requests fetched successfully.",
                new
                {
                    totalRecords,
                    pageNumber,
                    pageSize,
                    requests
                }));
        }

        // ==========================================
        // JWT USER / ORGANISATION VALIDATION
        // ==========================================
        private bool TryGetActor(
            int organisationId,
            out int actorUserId,
            out int errorCode)
        {
            actorUserId = 0;
            errorCode = 401;

            // Customer tokens cannot access employee APIs.
            if (string.Equals(
                User.FindFirstValue("token_type"),
                "CUSTOMER",
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

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

            if (!int.TryParse(userIdValue, out actorUserId)
                || !Guid.TryParse(sessionValue, out _))
                return false;

            errorCode = 403;

            if (organisationId <= 0)
                return false;

            if (roleCode == "SUPER_ADMIN")
                return true;

            return int.TryParse(orgValue, out int actorOrgId)
                   && actorOrgId == organisationId;
        }

        // ==========================================
        // RESPONSE HELPERS
        // ==========================================
        private IActionResult AccessError(int statusCode)
        {
            return StatusCode(
                statusCode,
                ResponseBody(
                    statusCode,
                    false,
                    statusCode == 401
                        ? "Invalid user session."
                        : "Organisation access denied."));
        }

        private object ResponseBody(
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

        private static string? DbString(
            SqlDataReader reader,
            string columnName)
        {
            return reader[columnName] == DBNull.Value
                ? null
                : Convert.ToString(reader[columnName]);
        }

        private static int? DbNullableInt(
            SqlDataReader reader,
            string columnName)
        {
            return reader[columnName] == DBNull.Value
                ? null
                : Convert.ToInt32(reader[columnName]);
        }

        private static string? DbDate(
            SqlDataReader reader,
            string columnName)
        {
            if (reader[columnName] == DBNull.Value)
                return null;

            return Convert.ToDateTime(reader[columnName])
                .ToString("MM/dd/yyyy");
        }








      
        private static async Task<List<Dictionary<string, object?>>> ReadRowsAsync(
                SqlDataReader reader)
        {
            var rows = new List<Dictionary<string, object?>>();

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>();

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] =
                        await reader.IsDBNullAsync(i)
                            ? null
                            : reader.GetValue(i);
                }

                rows.Add(row);
            }

            return rows;
        }




        [HttpGet("requests/{ticketId:long}")]
        public async Task<IActionResult> GetRequestDetails(
            [FromRoute] long ticketId,
            [FromQuery] int organisationId)
        {
            if (ticketId <= 0)
                return BadRequest(ResponseBody(
                    400, false, "Invalid TicketId."));

            if (!TryGetActor(
                organisationId,
                out int actorUserId,
                out int errorCode))
                return AccessError(errorCode);

            await using var con =
                new SqlConnection(_connectionString);

            await using var cmd = new SqlCommand(
                "dbo.usp_CMP_OrgConsentRequest_Details", con);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@OrganisationId", SqlDbType.Int)
                .Value = organisationId;

            cmd.Parameters.Add("@UserId", SqlDbType.Int)
                .Value = actorUserId;

            cmd.Parameters.Add("@TicketId", SqlDbType.BigInt)
                .Value = ticketId;

            await con.OpenAsync(HttpContext.RequestAborted);

            await using var reader = await cmd.ExecuteReaderAsync(
                HttpContext.RequestAborted);

            // Result 1: Ticket + original consent
            var ticketDetails = await ReadRowsAsync(reader);

            if (!await reader.NextResultAsync(HttpContext.RequestAborted))
                throw new InvalidOperationException(
                    "Consent decisions result set missing.");

            // Result 2: Original ACCEPT/REJECT decisions
            var elementDecisions = await ReadRowsAsync(reader);

            if (!await reader.NextResultAsync(HttpContext.RequestAborted))
                throw new InvalidOperationException(
                    "Change requests result set missing.");

            // Result 3: WITHDRAW/MODIFY requests
            var changeRequests = await ReadRowsAsync(reader);

            if (!await reader.NextResultAsync(HttpContext.RequestAborted))
                throw new InvalidOperationException(
                    "Ticket history result set missing.");

            // Result 4: History
            var history = await ReadRowsAsync(reader);

            if (ticketDetails.Count == 0)
                return NotFound(ResponseBody(
                    404, false, "Consent request not found."));

            return Ok(ResponseBody(
                200,
                true,
                "Consent request details fetched successfully.",
                new
                {
                    consent = ticketDetails[0],
                    elementDecisions,
                    changeRequests,
                    history
                }));
        }





        [HttpPost("query")]
        public async Task<IActionResult> AskCustomerQuestion(
            [FromBody] OrgConsentQuestionDto request)
        {
            if (request == null ||
                request.OrganisationId <= 0 ||
                request.TicketId <= 0 ||
                string.IsNullOrWhiteSpace(request.MessageText) ||
                request.MessageText.Trim().Length > 2000)
            {
                return BadRequest(ResponseBody(
                    400, false, "Invalid question details."));
            }

            if (!TryGetActor(
                request.OrganisationId,
                out int actorUserId,
                out int errorCode))
            {
                return AccessError(errorCode);
            }

            await using var con =
                new SqlConnection(_connectionString);

            await using var cmd = new SqlCommand(
                "dbo.usp_CMP_OrgConsentQuestion_Save", con);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@OrganisationId", SqlDbType.Int)
                .Value = request.OrganisationId;

            cmd.Parameters.Add("@TicketId", SqlDbType.BigInt)
                .Value = request.TicketId;

            cmd.Parameters.Add("@UserId", SqlDbType.Int)
                .Value = actorUserId;

            cmd.Parameters.Add("@MessageText", SqlDbType.NVarChar, 2000)
                .Value = request.MessageText.Trim();

            await con.OpenAsync(HttpContext.RequestAborted);

            await using var reader = await cmd.ExecuteReaderAsync(
                HttpContext.RequestAborted);

            if (!await reader.ReadAsync(HttpContext.RequestAborted))
                throw new InvalidOperationException(
                    "Question save returned no result.");

            return Ok(ResponseBody(
                200,
                true,
                "Question sent to customer successfully.",
                new
                {
                    conversationId =
                        Convert.ToInt64(reader["ConversationId"]),
                    ticketId =
                        Convert.ToInt64(reader["TicketId"]),
                    senderType = "ORGANISATION"
                }));
        }


        [HttpPost("decision")]
        public async Task<IActionResult> SaveDecision(
            [FromBody] ConsentTicketDecisionDto request)
        {
            if (request == null ||
                request.OrganisationId <= 0 ||
                request.TicketId <= 0)
            {
                return BadRequest(ResponseBody(
                    400, false, "Invalid decision request."));
            }

            string decision =
                (request.Decision ?? "").Trim().ToUpperInvariant();

            if (decision != "APPROVE" && decision != "REJECT")
            {
                return BadRequest(ResponseBody(
                    400, false,
                    "Decision must be APPROVE or REJECT."));
            }

            if (decision == "REJECT" &&
                string.IsNullOrWhiteSpace(request.Remarks))
            {
                return BadRequest(ResponseBody(
                    400, false, "Rejection remarks are required."));
            }

            if (!TryGetActor(
                request.OrganisationId,
                out int actorUserId,
                out int errorCode))
            {
                return AccessError(errorCode);
            }

            await using var con =
                new SqlConnection(_connectionString);

            await using var cmd = new SqlCommand(
                "dbo.usp_CMP_OrgConsentDecision_Save", con);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@OrganisationId", SqlDbType.Int)
                .Value = request.OrganisationId;

            cmd.Parameters.Add("@TicketId", SqlDbType.BigInt)
                .Value = request.TicketId;

            cmd.Parameters.Add("@UserId", SqlDbType.Int)
                .Value = actorUserId;

            cmd.Parameters.Add("@Decision", SqlDbType.VarChar, 10)
                .Value = decision;

            cmd.Parameters.Add("@Remarks", SqlDbType.NVarChar, 2000)
                .Value = string.IsNullOrWhiteSpace(request.Remarks)
                    ? DBNull.Value
                    : request.Remarks.Trim();

            await con.OpenAsync(HttpContext.RequestAborted);

            await using var reader = await cmd.ExecuteReaderAsync(
                HttpContext.RequestAborted);

            if (!await reader.ReadAsync(HttpContext.RequestAborted))
            {
                throw new InvalidOperationException(
                    "Decision procedure returned no result.");
            }

            return Ok(ResponseBody(
                200,
                true,
                Convert.ToString(reader["Message"])
                    ?? "Decision saved successfully.",
                new
                {
                    ticketId = Convert.ToInt64(reader["TicketId"]),
                    consentId = Convert.ToInt64(reader["ConsentId"]),
                    changeRequestId =
                        Convert.ToInt64(reader["ChangeRequestId"]),
                    requestStatus =
                        Convert.ToString(reader["RequestStatus"]),
                    ticketStatus =
                        Convert.ToString(reader["TicketStatus"])
                }));
        }

    }
}
