
using DPDP_Api.Data;
using DPDP_Api.DTOs;
using DPDP_Api.Extensions;
using DPDP_Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Cryptography;

namespace DPDP_Api.Controllers
{
    [ApiController]
    [Route("api/consent-public-link")]
    [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN")]
    public class ConsentPublicLinkController : ControllerBase
    {
        private readonly Db _db;
        private readonly IConfiguration _configuration;

        public ConsentPublicLinkController(
            Db db,
            IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create(
            [FromBody] ConsentPublicLinkCreateRequestDtos request)
        {
            int organisationId = User.OrganisationId() ?? 0;
            int userId = User.UserId();

            if (organisationId <= 0 || userId <= 0)
            {
                return StatusCode(403, new
                {
                    statusCode = 403,
                    success = false,
                    message = "Organisation access denied.",
                    traceId = HttpContext.TraceIdentifier
                });
            }

            string frontendUrl =
                _configuration["ConsentSettings:PublicBaseUrl"]
                ?? throw new InvalidOperationException(
                    "Consent public URL is not configured.");

            byte[] tokenBytes =
                RandomNumberGenerator.GetBytes(32);

            string token = Convert
                .ToHexString(tokenBytes)
                .ToLowerInvariant();

            byte[] tokenHash = SHA256.HashData(tokenBytes);

            var parameters = new[]
            {
                _db.P("@OrganisationId", organisationId),
                _db.P("@ConsentFormId", request.ConsentFormId),

                new SqlParameter(
                    "@PublicTokenHash",
                    SqlDbType.VarBinary,
                    32)
                {
                    Value = tokenHash
                },

                _db.P("@CreatedBy", userId)
            };

            var rows = await _db.RowsAsync(
                "dbo.ConsentPublicLink_Create",
                parameters,
                HttpContext.RequestAborted);

            string publicUrl =
                $"{frontendUrl.TrimEnd('/')}/form/{token}";

            return Ok(
                ApiResponse<object>.Ok(
                    200,
                    "Public consent link generated successfully.",
                    new
                    {
                        publicLink = publicUrl,
                        consentFormId = request.ConsentFormId,
                        result = rows
                    },
                    HttpContext.TraceIdentifier));
        }

        [HttpGet("list")]
        public async Task<IActionResult> List()
        {
            int organisationId = User.OrganisationId() ?? 0;

            if (organisationId <= 0)
                return Forbid();

            var rows = await _db.RowsAsync(
                "dbo.ConsentPublicLink_List",
                new[]
                {
                    _db.P("@OrganisationId", organisationId)
                },
                HttpContext.RequestAborted);

            return Ok(
                ApiResponse<object>.Ok(
                    200,
                    "Public consent links retrieved successfully.",
                    rows,
                    HttpContext.TraceIdentifier));
        }

        [HttpPut("deactivate/{publicLinkId:long}")]
        public async Task<IActionResult> Deactivate(
            long publicLinkId)
        {
            int organisationId = User.OrganisationId() ?? 0;
            int userId = User.UserId();

            if (organisationId <= 0 || userId <= 0)
                return Forbid();

            if (publicLinkId <= 0)
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    success = false,
                    message = "Invalid public link ID.",
                    traceId = HttpContext.TraceIdentifier
                });
            }

            var rows = await _db.RowsAsync(
                "dbo.ConsentPublicLink_Deactivate",
                new[]
                {
                    _db.P("@OrganisationId", organisationId),
                    _db.P("@PublicLinkId", publicLinkId),
                    _db.P("@ModifiedBy", userId)
                },
                HttpContext.RequestAborted);

            return Ok(
                ApiResponse<object>.Ok(
                    200,
                    "Public consent link deactivation processed.",
                    rows,
                    HttpContext.TraceIdentifier));
        }
    }
}
