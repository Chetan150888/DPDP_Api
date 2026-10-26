
using System;
using System.Data;
using System.Security.Claims;
using System.Threading.Tasks;
using DPDP_Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace DPDP_Api.Controllers
{
    [ApiController]
    [Route("api/customer/consent")]
    [Authorize]
    public class CustomerConsentRequestController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public CustomerConsentRequestController(
            IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ============================================
        // POST: api/customer/consent/request
        // Create WITHDRAW / MODIFY request
        // ============================================

        [HttpPost("request")]
        public async Task<IActionResult> SaveCustomerRequest(
            [FromBody] CustomerConsentRequestDto request)
        {
            string traceId = HttpContext.TraceIdentifier;

            if (User.FindFirst("token_type")?.Value != "CUSTOMER")
            {
                return StatusCode(403, new
                {
                    statusCode = 403,
                    success = false,
                    message = "Customer access only.",
                    data = (object?)null,
                    errors = Array.Empty<string>(),
                    traceId
                });
            }

            string? email =
                User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.FindFirst("email")?.Value;

            if (string.IsNullOrWhiteSpace(email))
            {
                return Unauthorized(new
                {
                    statusCode = 401,
                    success = false,
                    message = "Invalid customer token.",
                    data = (object?)null,
                    errors = Array.Empty<string>(),
                    traceId
                });
            }

            if (request == null ||
                request.ConsentId <= 0 ||
                string.IsNullOrWhiteSpace(request.RequestType) ||
                string.IsNullOrWhiteSpace(request.Reason))
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    success = false,
                    message = "ConsentId, RequestType and Reason are required.",
                    data = (object?)null,
                    errors = Array.Empty<string>(),
                    traceId
                });
            }

            string requestType =
                request.RequestType.Trim().ToUpperInvariant();

            if (requestType != "WITHDRAW" &&
                requestType != "MODIFY")
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    success = false,
                    message = "RequestType must be WITHDRAW or MODIFY.",
                    data = (object?)null,
                    errors = Array.Empty<string>(),
                    traceId
                });
            }

            string? proposedChangesJson =
                request.ProposedChanges?.GetRawText();

            if (requestType == "MODIFY" &&
                string.IsNullOrWhiteSpace(proposedChangesJson))
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    success = false,
                    message = "ProposedChanges is required for MODIFY.",
                    data = (object?)null,
                    errors = Array.Empty<string>(),
                    traceId
                });
            }

            string connectionString =
                _configuration.GetConnectionString(
                    "DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Database connection is not configured.");

            await using var connection =
                new SqlConnection(connectionString);

            await using var command = new SqlCommand(
                "dbo.usp_CMP_CustomerConsentRequest_Save",
                connection);

            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add(
                "@Email", SqlDbType.NVarChar, 254)
                .Value = email.Trim().ToLowerInvariant();

            command.Parameters.Add(
                "@ConsentId", SqlDbType.BigInt)
                .Value = request.ConsentId;

            command.Parameters.Add(
                "@RequestType", SqlDbType.VarChar, 20)
                .Value = requestType;

            command.Parameters.Add(
                "@Reason", SqlDbType.NVarChar, 2000)
                .Value = request.Reason.Trim();

            command.Parameters.Add(
                "@ProposedChangesJson", SqlDbType.NVarChar, -1)
                .Value = (object?)proposedChangesJson
                    ?? DBNull.Value;

            await connection.OpenAsync();

            await using var reader =
                await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                throw new InvalidOperationException(
                    "Customer request procedure returned no result.");
            }

            bool success = reader.GetBoolean(
                reader.GetOrdinal("Success"));

            string message = reader.GetString(
                reader.GetOrdinal("Message"));

            if (!success)
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    success = false,
                    message,
                    data = (object?)null,
                    errors = Array.Empty<string>(),
                    traceId
                });
            }

            var data = new
            {
                ticketId = reader.GetInt64(
                    reader.GetOrdinal("TicketId")),

                ticketNo = reader.GetString(
                    reader.GetOrdinal("TicketNo")),

                requestType = reader.GetString(
                    reader.GetOrdinal("RequestType")),

                requestStatus = reader.GetString(
                    reader.GetOrdinal("RequestStatus"))
            };

            return Ok(new
            {
                statusCode = 200,
                success = true,
                message,
                data,
                errors = Array.Empty<string>(),
                traceId
            });
        }

        //// ============================================
        //// GET: api/customer/consent/requests
        //// Customer WITHDRAW / MODIFY request list
        //// ============================================

        //[HttpGet("requests")]
        //public async Task<IActionResult> GetMyRequests(
        //    [FromQuery] int pageNumber = 1,
        //    [FromQuery] int pageSize = 20)
        //{
        //    if (pageNumber < 1 ||
        //        pageSize < 1 ||
        //        pageSize > 100)
        //    {
        //        return BadRequest(new
        //        {
        //            statusCode = 400,
        //            success = false,
        //            message = "Invalid pagination parameters.",
        //            data = (object?)null,
        //            errors = new[]
        //            {
        //                "Invalid pageNumber or pageSize."
        //            },
        //            traceId = HttpContext.TraceIdentifier
        //        });
        //    }

        //    string? tokenType =
        //        User.FindFirst("token_type")?.Value;

        //    if (tokenType != "CUSTOMER")
        //    {
        //        return StatusCode(403, new
        //        {
        //            statusCode = 403,
        //            success = false,
        //            message = "Customer access only.",
        //            data = (object?)null,
        //            errors = Array.Empty<string>(),
        //            traceId = HttpContext.TraceIdentifier
        //        });
        //    }

        //    string? email =
        //        User.FindFirst(ClaimTypes.Email)?.Value
        //        ?? User.FindFirst("email")?.Value;

        //    if (string.IsNullOrWhiteSpace(email))
        //    {
        //        return Unauthorized(new
        //        {
        //            statusCode = 401,
        //            success = false,
        //            message = "Invalid customer token.",
        //            data = (object?)null,
        //            errors = Array.Empty<string>(),
        //            traceId = HttpContext.TraceIdentifier
        //        });
        //    }

        //    var result = new CustomerConsentRequestPagedDto
        //    {
        //        PageNumber = pageNumber,
        //        PageSize = pageSize
        //    };

        //    string connectionString =
        //        _configuration.GetConnectionString(
        //            "DefaultConnection")
        //        ?? throw new InvalidOperationException(
        //            "Database connection is not configured.");

        //    await using var connection =
        //        new SqlConnection(connectionString);

        //    await using var command = new SqlCommand(
        //        "dbo.usp_CMP_CustomerConsentRequests_List",
        //        connection);

        //    command.CommandType = CommandType.StoredProcedure;

        //    command.Parameters.Add(
        //        "@Email", SqlDbType.NVarChar, 254)
        //        .Value = email.Trim().ToLowerInvariant();

        //    command.Parameters.Add(
        //        "@PageNumber", SqlDbType.Int)
        //        .Value = pageNumber;

        //    command.Parameters.Add(
        //        "@PageSize", SqlDbType.Int)
        //        .Value = pageSize;

        //    await connection.OpenAsync();

        //    await using var reader =
        //        await command.ExecuteReaderAsync();

        //    // First result set: TotalRecords

        //    if (await reader.ReadAsync())
        //    {
        //        result.TotalRecords = reader.GetInt64(
        //            reader.GetOrdinal("TotalRecords"));
        //    }

        //    // Second result set: Requests

        //    if (await reader.NextResultAsync())
        //    {
        //        while (await reader.ReadAsync())
        //        {
        //            string? GetString(string column)
        //            {
        //                int index = reader.GetOrdinal(column);

        //                return reader.IsDBNull(index)
        //                    ? null
        //                    : reader.GetString(index);
        //            }

        //            string? GetDate(string column)
        //            {
        //                int index = reader.GetOrdinal(column);

        //                return reader.IsDBNull(index)
        //                    ? null
        //                    : reader.GetDateTime(index).ToString(
        //                        "MM/dd/yyyy",
        //                        System.Globalization.CultureInfo
        //                            .InvariantCulture);
        //            }

        //            result.Requests.Add(
        //                new CustomerConsentRequestListDto
        //                {
        //                    ChangeRequestId = reader.GetInt64(
        //                        reader.GetOrdinal("ChangeRequestId")),

        //                    TicketId = reader.GetInt64(
        //                        reader.GetOrdinal("TicketId")),

        //                    TicketNo = GetString("TicketNo"),

        //                    ConsentId = reader.GetInt64(
        //                        reader.GetOrdinal("ConsentId")),

        //                    OrganisationId = reader.GetInt32(
        //                        reader.GetOrdinal("OrganisationId")),

        //                    RequestType = reader.GetString(
        //                        reader.GetOrdinal("RequestType")),

        //                    RequestStatus = reader.GetString(
        //                        reader.GetOrdinal("RequestStatus")),

        //                    TicketStatus = reader.GetString(
        //                        reader.GetOrdinal("TicketStatus")),

        //                    Reason = GetString("Reason"),

        //                    ProposedChangesJson =
        //                        GetString("ProposedChangesJson"),

        //                    RequestedDate =
        //                        GetDate("RequestedDate") ?? "",

        //                    DecisionDate = GetDate("DecisionDate"),

        //                    DecisionRemarks =
        //                        GetString("DecisionRemarks"),

        //                    AppliedDate = GetDate("AppliedDate")
        //                });
        //        }
        //    }

        //    return Ok(new
        //    {
        //        statusCode = 200,
        //        success = true,
        //        message = "Customer requests fetched successfully.",
        //        data = result,
        //        errors = Array.Empty<string>(),
        //        traceId = HttpContext.TraceIdentifier
        //    });
        //}





        [HttpGet("request")]
        public async Task<IActionResult> GetMyConsents(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken cancellationToken = default)
        {
            string traceId = HttpContext.TraceIdentifier;
            if (User.FindFirst("token_type")?.Value != "CUSTOMER")
                return StatusCode(403, new
                {
                    statusCode = 403,
                    success = false,
                    message = "Customer access only.",
                    data = (object?)null,
                    errors = Array.Empty<string>(),
                    traceId
                });

            string? email = User.FindFirst(ClaimTypes.Email)?.Value
                         ?? User.FindFirst("email")?.Value;
            if (string.IsNullOrWhiteSpace(email))
                return Unauthorized(new
                {
                    statusCode = 401,
                    success = false,
                    message = "Invalid customer token.",
                    data = (object?)null,
                    errors = Array.Empty<string>(),
                    traceId
                });

            if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
                return BadRequest(new
                {
                    statusCode = 400,
                    success = false,
                    message = "Invalid pagination parameters.",
                    data = (object?)null,
                    errors = Array.Empty<string>(),
                    traceId
                });

            string connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Database connection is not configured.");

            await using var connection = new SqlConnection(connectionString);
            await using var command = new SqlCommand("dbo.usp_CMP_CustomerMyConsents_Get", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add("@Email", SqlDbType.NVarChar, 254).Value = email.Trim().ToLowerInvariant();
            command.Parameters.Add("@PageNumber", SqlDbType.Int).Value = pageNumber;
            command.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;
            await connection.OpenAsync(cancellationToken);

            // FOR JSON can be split across multiple rows for large results.
            var jsonBuilder = new System.Text.StringBuilder();
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                    if (!reader.IsDBNull(0)) jsonBuilder.Append(reader.GetString(0));
            }
            if (jsonBuilder.Length == 0)
                throw new InvalidOperationException("My Consents procedure returned no data.");

            using var doc = System.Text.Json.JsonDocument.Parse(jsonBuilder.ToString());
            return Ok(new
            {
                statusCode = 200,
                success = true,
                message = "Customer consents fetched successfully.",
                data = doc.RootElement.Clone(),
                errors = Array.Empty<string>(),
                traceId
            });
        }





        
[HttpPost("reply")]
public async Task<IActionResult> ReplyToOrganisation(
    [FromBody] CustomerConsentReplyDto request)
{
    if (request == null ||
        request.TicketId <= 0 ||
        string.IsNullOrWhiteSpace(request.MessageText) ||
        request.MessageText.Trim().Length > 2000)
    {
        return BadRequest(new
        {
            statusCode = 400,
            success = false,
            message = "Invalid reply details.",
            data = (object?)null,
            errors = Array.Empty<string>(),
            traceId = HttpContext.TraceIdentifier
        });
    }

    string? tokenType =
        User.FindFirstValue("token_type");

    string? customerEmail =
        User.FindFirstValue("email");

    if (!string.Equals(
            tokenType,
            "CUSTOMER",
            StringComparison.OrdinalIgnoreCase)
        || string.IsNullOrWhiteSpace(customerEmail))
    {
        return Unauthorized(new
        {
            statusCode = 401,
            success = false,
            message = "Invalid customer session.",
            data = (object?)null,
            errors = Array.Empty<string>(),
            traceId = HttpContext.TraceIdentifier
        });
    }

            string connectionString =
            _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Database connection is not configured.");

            await using var con =
                new SqlConnection(connectionString);

            await using var cmd = new SqlCommand(
        "dbo.usp_CMP_CustomerConsentReply_Save", con);

    cmd.CommandType = CommandType.StoredProcedure;

    cmd.Parameters.Add("@TicketId", SqlDbType.BigInt)
        .Value = request.TicketId;

    cmd.Parameters.Add("@CustomerEmail", SqlDbType.NVarChar, 254)
        .Value = customerEmail;

    cmd.Parameters.Add("@MessageText", SqlDbType.NVarChar, 2000)
        .Value = request.MessageText.Trim();

    await con.OpenAsync(HttpContext.RequestAborted);

    await using var reader = await cmd.ExecuteReaderAsync(
        HttpContext.RequestAborted);

    if (!await reader.ReadAsync(HttpContext.RequestAborted))
        throw new InvalidOperationException(
            "Customer reply returned no result.");

    return Ok(new
    {
        statusCode = 200,
        success = true,
        message = "Reply sent successfully.",
        data = new
        {
            conversationId =
                Convert.ToInt64(reader["ConversationId"]),
            ticketId =
                Convert.ToInt64(reader["TicketId"]),
            senderType = "CUSTOMER"
        },
        errors = Array.Empty<string>(),
        traceId = HttpContext.TraceIdentifier
    });
}









      

    }
}
