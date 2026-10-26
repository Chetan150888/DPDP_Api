
using DPDP_Api.Data;
using DPDP_Api.DTOs;
using DPDP_Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Cryptography;
using System.Text.Json;

namespace DPDP_Api.Controllers
{
    [ApiController]
    [Route("api/public/consent")]
    [AllowAnonymous]
    public class PublicConsentController : ControllerBase
    {
        private readonly Db _db;

        public PublicConsentController(Db db)
        {
            _db = db;
        }

        // GET: api/public/consent/{token}
        [HttpGet("{token}")]
        public async Task<IActionResult> GetDetails(
            [FromRoute] string token,
            CancellationToken ct)
        {
            if (!TryGetTokenHash(token, out byte[] tokenHash))
                return InvalidLink();

            var parameters = new[]
            {
                new SqlParameter(
                    "@PublicTokenHash",
                    SqlDbType.VarBinary,
                    32)
                {
                    Value = tokenHash
                }
            };

            var rows = await _db.RowsAsync(
                "dbo.ConsentPublic_GetDetails",
                parameters,
                ct);

            if (rows.Count == 0)
                return InvalidLink();

            return Ok(
                ApiResponse<object>.Ok(
                    200,
                    "Public consent details retrieved successfully.",
                    rows,
                    HttpContext.TraceIdentifier));
        }

        // POST: api/public/consent/{token}/submit
        [HttpPost("{token}/submit")]
        public async Task<IActionResult> Submit(
            [FromRoute] string token,
            [FromBody] PublicConsentSubmitRequestDtos request,
            CancellationToken ct)
        {
            if (!TryGetTokenHash(token, out byte[] tokenHash))
                return InvalidLink();

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    success = false,
                    message = "Name is required.",
                    traceId = HttpContext.TraceIdentifier
                });
            }

            if (string.IsNullOrWhiteSpace(request.Mobile) &&
                string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    success = false,
                    message = "Mobile or email is required.",
                    traceId = HttpContext.TraceIdentifier
                });
            }

            if (request.Decisions == null ||
                request.Decisions.Count == 0)
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    success = false,
                    message = "Purpose decisions are required.",
                    traceId = HttpContext.TraceIdentifier
                });
            }

            string decisionsJson =
                JsonSerializer.Serialize(
                    request.Decisions.Select(d => new
                    {
                        consentPurposeId = d.ConsentPurposeId,
                        noticeVersionId = d.NoticeVersionId,
                        decision = d.Decision
                    }));

            string? ipAddress =
                HttpContext.Connection.RemoteIpAddress?.ToString();

            string? userAgent =
                Request.Headers.UserAgent.ToString();

            if (userAgent != null && userAgent.Length > 500)
                userAgent = userAgent.Substring(0, 500);

            var parameters = new[]
            {
                new SqlParameter(
                    "@PublicTokenHash",
                    SqlDbType.VarBinary,
                    32)
                {
                    Value = tokenHash
                },

                _db.P("@Name", request.Name.Trim()),
                _db.P("@Mobile", request.Mobile),
                _db.P("@Email", request.Email),
                _db.P("@DecisionsJson", decisionsJson),
                _db.P("@IPAddress", ipAddress),
                _db.P("@UserAgent", userAgent)
            };

            var rows = await _db.RowsAsync(
                "dbo.ConsentPublic_Submit",
                parameters,
                ct);

            return Ok(
                ApiResponse<object>.Ok(
                    200,
                    "Consent submitted successfully.",
                    rows,
                    HttpContext.TraceIdentifier));
        }

        private static bool TryGetTokenHash(
            string token,
            out byte[] tokenHash)
        {
            tokenHash = Array.Empty<byte>();

            if (string.IsNullOrWhiteSpace(token) ||
                token.Length != 64)
            {
                return false;
            }

            try
            {
                byte[] tokenBytes =
                    Convert.FromHexString(token);

                tokenHash = SHA256.HashData(tokenBytes);

                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private IActionResult InvalidLink()
        {
            return NotFound(new
            {
                statusCode = 404,
                success = false,
                message = "Consent link is invalid or unavailable.",
                data = (object?)null,
                errors = Array.Empty<string>(),
                traceId = HttpContext.TraceIdentifier
            });
        }
    }
}
