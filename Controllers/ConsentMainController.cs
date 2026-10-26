
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DPDP_Api.Data;
using DPDP_Api.DTOs;
using DPDP_Api.Extensions;
using DPDP_Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace DPDP_Api.Controllers
{
    [ApiController]
    [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN")]
    [Route("api/consent")]
    public class ConsentMainController : ControllerBase
    {
        private readonly Db _db;

        public ConsentMainController(Db db)
        {
            _db = db;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create(
            [FromBody] ConsentMainCreateRequest request,
            CancellationToken ct)
        {
            int organisationId = User.OrganisationId() ?? 0;
            int userId = User.UserId();

            if (organisationId <= 0 || userId <= 0)
            {
                return StatusCode(403, new
                {
                    statusCode = 403,
                    success = false,
                    message = "Valid organisation and user context required.",
                    traceId = HttpContext.TraceIdentifier
                });
            }

            string? validationError = ValidateRequest(request);

            if (validationError != null)
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    success = false,
                    message = validationError,
                    traceId = HttpContext.TraceIdentifier
                });
            }

            var parameters = new List<SqlParameter>(
                GetSaveParameters(request, organisationId))
            {
                _db.P("@CreatedBy", userId)
            };

            var rows = await _db.RowsAsync(
                "dbo.ConsentMain_Insert",
                parameters,
                ct);

            return Ok(ApiResponse<object>.Ok(
                200,
                "Consent created successfully.",
                rows.FirstOrDefault(),
                HttpContext.TraceIdentifier));
        }

        [HttpPut("update")]
        public async Task<IActionResult> Update(
            [FromBody] ConsentMainUpdateRequest request,
            CancellationToken ct)
        {
            int organisationId = User.OrganisationId() ?? 0;
            int userId = User.UserId();

            if (organisationId <= 0 || userId <= 0)
            {
                return StatusCode(403, new
                {
                    statusCode = 403,
                    success = false,
                    message = "Valid organisation and user context required.",
                    traceId = HttpContext.TraceIdentifier
                });
            }

            if (request.ConsentId <= 0 ||
                request.ExpectedVersion <= 0)
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    success = false,
                    message = "Valid ConsentId and ExpectedVersion required.",
                    traceId = HttpContext.TraceIdentifier
                });
            }

            string? validationError = ValidateRequest(request);

            if (validationError != null)
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    success = false,
                    message = validationError,
                    traceId = HttpContext.TraceIdentifier
                });
            }

            var parameters = new List<SqlParameter>(
                GetSaveParameters(request, organisationId))
            {
                _db.P("@ConsentId", request.ConsentId),
                _db.P("@ExpectedVersion", request.ExpectedVersion),
                _db.P("@ModifiedBy", userId)
            };

            var rows = await _db.RowsAsync(
                "dbo.ConsentMain_Update",
                parameters,
                ct);

            return Ok(ApiResponse<object>.Ok(
                200,
                "Consent updated successfully.",
                rows.FirstOrDefault(),
                HttpContext.TraceIdentifier));
        }

        [HttpDelete("delete/{consentId:long}")]
        public async Task<IActionResult> Delete(
            long consentId,
            [FromQuery] int expectedVersion,
            CancellationToken ct)
        {
            int organisationId = User.OrganisationId() ?? 0;
            int userId = User.UserId();

            if (organisationId <= 0 || userId <= 0)
            {
                return StatusCode(403, new
                {
                    statusCode = 403,
                    success = false,
                    message = "Valid organisation and user context required.",
                    traceId = HttpContext.TraceIdentifier
                });
            }

            if (consentId <= 0 || expectedVersion <= 0)
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    success = false,
                    message = "Valid ConsentId and ExpectedVersion required.",
                    traceId = HttpContext.TraceIdentifier
                });
            }

            var parameters = new[]
            {
                _db.P("@ConsentId", consentId),
                _db.P("@OrganisationId", organisationId),
                _db.P("@ExpectedVersion", expectedVersion),
                _db.P("@ModifiedBy", userId)
            };

            var rows = await _db.RowsAsync(
                "dbo.ConsentMain_Delete",
                parameters,
                ct);

            return Ok(ApiResponse<object>.Ok(
                200,
                "Consent deleted successfully.",
                rows.FirstOrDefault(),
                HttpContext.TraceIdentifier));
        }

        [HttpGet("{consentId:long}")]
        public async Task<IActionResult> GetById(
            long consentId,
            CancellationToken ct)
        {
            int organisationId = User.OrganisationId() ?? 0;

            if (organisationId <= 0)
            {
                return StatusCode(403, new
                {
                    statusCode = 403,
                    success = false,
                    message = "Organisation context required.",
                    traceId = HttpContext.TraceIdentifier
                });
            }

            var parameters = new[]
            {
                _db.P("@ConsentId", consentId),
                _db.P("@OrganisationId", organisationId)
            };

            var rows = await _db.RowsAsync(
                "dbo.ConsentMain_GetById",
                parameters,
                ct);

            if (rows.Count == 0)
            {
                return NotFound(new
                {
                    statusCode = 404,
                    success = false,
                    message = "Consent not found.",
                    traceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(ApiResponse<object>.Ok(
                200,
                "Consent retrieved successfully.",
                rows[0],
                HttpContext.TraceIdentifier));
        }

        [HttpGet("list")]
        public async Task<IActionResult> List(
            [FromQuery] int? consentFormId,
            [FromQuery] string? status,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            int organisationId = User.OrganisationId() ?? 0;

            if (organisationId <= 0)
            {
                return StatusCode(403, new
                {
                    statusCode = 403,
                    success = false,
                    message = "Organisation context required.",
                    traceId = HttpContext.TraceIdentifier
                });
            }

            if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    success = false,
                    message = "Invalid pagination.",
                    traceId = HttpContext.TraceIdentifier
                });
            }

            var parameters = new[]
            {
                _db.P("@OrganisationId", organisationId),
                _db.P("@ConsentFormId", consentFormId),
                _db.P("@Status", status),
                _db.P("@PageNumber", pageNumber),
                _db.P("@PageSize", pageSize)
            };

            var rows = await _db.RowsAsync(
                "dbo.ConsentMain_List",
                parameters,
                ct);

            return Ok(ApiResponse<object>.Ok(
                200,
                "Consent list retrieved successfully.",
                rows,
                HttpContext.TraceIdentifier));
        }

        [HttpGet("history/{consentId:long}")]
        public async Task<IActionResult> History(
            long consentId,
            CancellationToken ct)
        {
            int organisationId = User.OrganisationId() ?? 0;

            if (organisationId <= 0)
            {
                return StatusCode(403, new
                {
                    statusCode = 403,
                    success = false,
                    message = "Organisation context required.",
                    traceId = HttpContext.TraceIdentifier
                });
            }

            var parameters = new[]
            {
                _db.P("@ConsentId", consentId),
                _db.P("@OrganisationId", organisationId)
            };

            var rows = await _db.RowsAsync(
                "dbo.ConsentMain_VersionHistory",
                parameters,
                ct);

            return Ok(ApiResponse<object>.Ok(
                200,
                "Consent history retrieved successfully.",
                rows,
                HttpContext.TraceIdentifier));
        }

        private SqlParameter[] GetSaveParameters(
            ConsentMainSaveRequest request,
            int organisationId)
        {
            string? ipAddress =
                HttpContext.Connection.RemoteIpAddress?.ToString();

            return new[]
            {
                _db.P("@Name", request.Name.Trim()),
                _db.P("@Mobile", request.Mobile),
                _db.P("@Email", request.Email),
                _db.P("@ConsentFormId", request.ConsentFormId),

                _db.P("@StartDate",
                    request.StartDate.HasValue
                        ? (object)request.StartDate.Value.Date
                        : DBNull.Value),

                _db.P("@EndDate",
                    request.EndDate.HasValue
                        ? (object)request.EndDate.Value.Date
                        : DBNull.Value),

                _db.P("@Status", request.Status),
                _db.P("@OrganisationId", organisationId),
                _db.P("@LegalEntityId", request.LegalEntityId),
                _db.P("@IPAddress", ipAddress),
                _db.P("@MacId", DBNull.Value)
            };
        }

        private string? ValidateRequest(
            ConsentMainSaveRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return "Name is required.";
            }

            if (request.ConsentFormId <= 0)
            {
                return "Valid ConsentFormId is required.";
            }

            if (string.IsNullOrWhiteSpace(request.Mobile) &&
                string.IsNullOrWhiteSpace(request.Email))
            {
                return "Email or Mobile is required.";
            }

            if (request.StartDate.HasValue &&
                request.EndDate.HasValue &&
                request.EndDate.Value.Date < request.StartDate.Value.Date)
            {
                return "EndDate cannot precede StartDate.";
            }

            string[] allowedStatuses =
            {
                "PENDING",
                "GRANTED",
                "REJECTED",
                "PARTIALLY_GRANTED",
                "WITHDRAWN",
                "EXPIRED"
            };

            if (!allowedStatuses.Contains(
                request.Status,
                StringComparer.Ordinal))
            {
                return "Invalid consent status.";
            }

            return null;
        }
    }
}
