
using DPDP_Api.Data;
using DPDP_Api.DTOs;
using DPDP_Api.Models;
using Microsoft.AspNetCore.Authorization;

//using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DPDP_Api.Controllers
{
    [ApiController]
    [Route("api/consent-form-details")]
    // [AllowAnonymous]
    public class ConsentFormDetailsController : ControllerBase
    {
        private readonly Db _db;

        public ConsentFormDetailsController(Db db)
        {
            _db = db;
        }

        // ==========================================
        // GET CONSENT FORM DETAILS
        // GET: api/consent-form-details/101
        // ==========================================

        [HttpGet("{formId:int}")]
        public async Task<IActionResult> GetDetails(
            int formId, int OrganisationId,
            CancellationToken ct)
        {
            if (formId <= 0)
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    success = false,
                    message = "Valid FormId is required.",
                    data = (object?)null,
                    errors = Array.Empty<string>(),
                    traceId = HttpContext.TraceIdentifier
                });
            }

            var rows = await _db.RowsAsync(
                "dbo.ConsentFormDetails_Get",
                new[]
                {
                    _db.P("@FormId", formId)  ,
                    _db.P("@OrganisationId", OrganisationId)
                },
                ct
            );

            if (rows == null || rows.Count == 0)
            {
                return NotFound(new
                {
                    statusCode = 404,
                    success = false,
                    message = "Consent form not found or unavailable.",
                    data = (object?)null,
                    errors = Array.Empty<string>(),
                    traceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(
                ApiResponse<object>.Ok(
                    200,
                    "Consent form details retrieved successfully.",
                    rows,
                    HttpContext.TraceIdentifier
                )
            );
        }

        // ==========================================
        // SUBMIT CONSENT FORM
        // POST: api/consent-form-details/submit
        // ==========================================

        [HttpPost("submit")]
        public async Task<IActionResult> Submit(
            [FromBody] ConsentFormDetailsSubmitRequest request,
            CancellationToken ct)
        {
            // Validate request
            if (request == null)
            {
                return ValidationError(
                    "Request body is required.");
            }

            if (request.ConsentFormId <= 0)
            {
                return ValidationError(
                    "Valid ConsentFormId is required.");
            }

            if (request.SubmissionKey == Guid.Empty)
            {
                return ValidationError(
                    "Valid SubmissionKey is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return ValidationError(
                    "Customer name is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Mobile) &&
                string.IsNullOrWhiteSpace(request.Email))
            {
                return ValidationError(
                    "Mobile or Email is required.");
            }

            if (request.Decisions == null ||
                !request.Decisions.Any())
            {
                return ValidationError(
                    "At least one consent decision is required.");
            }

            // Validate each data element decision
            foreach (var item in request.Decisions)
            {
                if (item.ConsentPurposeId <= 0 ||
                    item.DataCategoryId <= 0 ||
                    item.DataElementId <= 0 ||
                    item.NoticeVersionId <= 0)
                {
                    return ValidationError(
                        "Invalid Purpose, Category, Element or NoticeVersion ID.");
                }

                string decision =
                    item.Decision?.Trim().ToUpperInvariant()
                    ?? string.Empty;

                if (decision != "ACCEPT" &&
                    decision != "REJECT")
                {
                    return ValidationError(
                        "Decision must be ACCEPT or REJECT.");
                }
            }

            // Prepare decisions JSON for SQL stored procedure
            string decisionsJson = JsonSerializer.Serialize(
                request.Decisions.Select(item => new
                {
                    consentPurposeId = item.ConsentPurposeId,
                    dataCategoryId = item.DataCategoryId,
                    dataElementId = item.DataElementId,
                    noticeVersionId = item.NoticeVersionId,
                    decision = item.Decision.Trim().ToUpperInvariant()
                })
            );

            // Customer request information
            string? ipAddress =
                HttpContext.Connection.RemoteIpAddress?.ToString();

            string? userAgent =
                Request.Headers.UserAgent.ToString();

            if (!string.IsNullOrEmpty(userAgent) &&
                userAgent.Length > 500)
            {
                userAgent = userAgent.Substring(0, 500);
            }

            // Execute stored procedure
            var rows = await _db.RowsAsync(
                "dbo.ConsentFormDetails_Submit",
                new[]
                {  _db.P("@OrganisationId", request.OrganisationId),
                    _db.P("@FormId", request.ConsentFormId),
                    _db.P("@Name", request.Name.Trim()),
                    _db.P("@Mobile", request.Mobile?.Trim()),
                    _db.P("@Email", request.Email?.Trim()),
                    _db.P("@DecisionsJson", decisionsJson),
                    _db.P("@SubmissionKey", request.SubmissionKey),
                    _db.P("@IPAddress", ipAddress),
                    _db.P("@UserAgent", userAgent)
                },
                ct
            );

            return Ok(
                ApiResponse<object>.Ok(
                    200,
                    "Consent submitted successfully.",
                    rows,
                    HttpContext.TraceIdentifier
                )
            );
        }

        // ==========================================
        // COMMON VALIDATION RESPONSE
        // ==========================================

        private IActionResult ValidationError(string message)
        {
            return BadRequest(new
            {
                statusCode = 400,
                success = false,
                message = message,
                data = (object?)null,
                errors = Array.Empty<string>(),
                traceId = HttpContext.TraceIdentifier
            });
        }





        // ==========================================
        // GET CUSTOMER SUBMITTED CONSENT
        // GET: api/consent-form-details/submission/29
        // ==========================================

        [HttpGet("submission/{consentId:int}")]
       // [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN")]
        public async Task<IActionResult> GetSubmission(
            int consentId,
            int organisationId,
            CancellationToken ct)
        {
            if (consentId <= 0 || organisationId <= 0)
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    success = false,
                    message = "Valid ConsentId and OrganisationId are required.",
                    data = (object?)null,
                    errors = Array.Empty<string>(),
                    traceId = HttpContext.TraceIdentifier
                });
            }

            var rows = await _db.RowsAsync(
                "dbo.ConsentFormDetails_GetSubmission",
                new[]
                {
                    _db.P("@ConsentId", consentId),
                    _db.P("@OrganisationId", organisationId)
                },
                ct
            );

            if (rows.Count == 0)
            {
                return NotFound(new
                {
                    statusCode = 404,
                    success = false,
                    message = "Submitted consent not found.",
                    data = (object?)null,
                    errors = Array.Empty<string>(),
                    traceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(
                ApiResponse<object>.Ok(
                    200,
                    "Submitted consent details retrieved successfully.",
                    rows,
                    HttpContext.TraceIdentifier
                )
            );
        }


    }

    }
