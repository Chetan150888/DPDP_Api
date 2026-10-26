using DPDP_Api.Data;
using DPDP_Api.DTOs;
using DPDP_Api.Extensions;
using DPDP_Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DPDP_Api.Controllers;

[ApiController]
[Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN")]
[Route("api/consent-notice-version")]
public sealed class ConsentNoticeVersionController : ControllerBase
{
    private readonly Db _db;

    public ConsentNoticeVersionController(Db db)
    {
        _db = db;
    }

    private int ScopeOrganisation(int requestedOrganisationId)
    {
        if (User.RoleCode() == "SUPER_ADMIN")
        {
            if (requestedOrganisationId <= 0)
                throw new ArgumentException(
                    "Organisation selection is required.");

            return requestedOrganisationId;
        }

        var organisationId = User.OrganisationId();

        if (!organisationId.HasValue || organisationId.Value <= 0)
            throw new UnauthorizedAccessException(
                "Organisation access is not available for this account.");

        if (requestedOrganisationId > 0 &&
            requestedOrganisationId != organisationId.Value)
            throw new UnauthorizedAccessException("Access denied.");

        return organisationId.Value;
    }

    private ActionResult<ApiResponse<object>> Success(
        string message,
        object? data = null)
    {
        return Ok(ApiResponse<object>.Ok(
            200,
            message,
            data,
            HttpContext.TraceIdentifier));
    }

    [HttpPost("create")]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        CreateConsentNoticeVersionRequest request,
        CancellationToken ct)
    {
        if (request == null)
            throw new ArgumentException("Request body is required.");

        if (request.ConsentNoticeId <= 0)
            throw new ArgumentException("Consent notice is required.");

        if (string.IsNullOrWhiteSpace(request.LanguageCode))
            throw new ArgumentException("Language code is required.");

        if (string.IsNullOrWhiteSpace(request.NoticeTitle))
            throw new ArgumentException("Notice title is required.");

        if (string.IsNullOrWhiteSpace(request.NoticeContent))
            throw new ArgumentException("Notice content is required.");

        if (request.EffectiveFrom.HasValue &&
            request.EffectiveTo.HasValue &&
            request.EffectiveTo < request.EffectiveFrom)
            throw new ArgumentException(
                "Effective to date cannot be earlier than effective from date.");

        var organisationId = ScopeOrganisation(request.OrganisationId);

        var noticeVersionId = await _db.ScalarIntAsync(
            "dbo.create_consent_notice_version",
            new[]
            {
                _db.P("@OrganisationId", organisationId),
                _db.P("@ConsentNoticeId", request.ConsentNoticeId),
                _db.P("@LanguageCode", request.LanguageCode.Trim()),
                _db.P("@NoticeTitle", request.NoticeTitle.Trim()),
                _db.P("@NoticeContent", request.NoticeContent.Trim()),
                _db.P("@EffectiveFrom", request.EffectiveFrom),
                _db.P("@EffectiveTo", request.EffectiveTo),
                _db.P("@CreatedBy", User.UserId())
            },
            ct);

        return Success(
            "Consent notice version created successfully.",
            new { noticeVersionId });
    }

    [HttpGet("list")]
    public async Task<ActionResult<ApiResponse<object>>> List(
        int organisationId,
        int? consentNoticeId = null,
        string? languageCode = null,
        string? status = null,
        CancellationToken ct = default)
    {
        var orgId = ScopeOrganisation(organisationId);

        var rows = await _db.RowsAsync(
            "dbo.list_consent_notice_version",
            new[]
            {
                _db.P("@OrganisationId", orgId),
                _db.P("@ConsentNoticeId", consentNoticeId),
                _db.P("@LanguageCode", languageCode),
                _db.P("@Status", status)
            },
            ct);

        return Success(
            "Consent notice versions retrieved successfully.",
            rows);
    }

    [HttpGet("{noticeVersionId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Get(
        int noticeVersionId,
        int organisationId,
        CancellationToken ct)
    {
        if (noticeVersionId <= 0)
            throw new ArgumentException(
                "Notice version id is required.");

        var orgId = ScopeOrganisation(organisationId);

        var rows = await _db.RowsAsync(
            "dbo.get_consent_notice_version",
            new[]
            {
                _db.P("@OrganisationId", orgId),
                _db.P("@NoticeVersionId", noticeVersionId)
            },
            ct);

        return Success(
            "Consent notice version retrieved successfully.",
            rows);
    }

    [HttpPut("update")]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        UpdateConsentNoticeVersionRequest request,
        CancellationToken ct)
    {
        if (request == null)
            throw new ArgumentException("Request body is required.");

        if (request.NoticeVersionId <= 0)
            throw new ArgumentException(
                "Notice version id is required.");

        if (string.IsNullOrWhiteSpace(request.NoticeTitle))
            throw new ArgumentException("Notice title is required.");

        if (string.IsNullOrWhiteSpace(request.NoticeContent))
            throw new ArgumentException("Notice content is required.");

        if (request.EffectiveFrom.HasValue &&
            request.EffectiveTo.HasValue &&
            request.EffectiveTo < request.EffectiveFrom)
            throw new ArgumentException(
                "Effective to date cannot be earlier than effective from date.");

        var organisationId = ScopeOrganisation(request.OrganisationId);

        var noticeVersionId = await _db.ScalarIntAsync(
            "dbo.update_consent_notice_version",
            new[]
            {
                _db.P("@NoticeVersionId", request.NoticeVersionId),
                _db.P("@OrganisationId", organisationId),
                _db.P("@NoticeTitle", request.NoticeTitle.Trim()),
                _db.P("@NoticeContent", request.NoticeContent.Trim()),
                _db.P("@EffectiveFrom", request.EffectiveFrom),
                _db.P("@EffectiveTo", request.EffectiveTo),
                _db.P("@ModifiedBy", User.UserId())
            },
            ct);

        return Success(
            "Consent notice version updated successfully.",
            new { noticeVersionId });
    }

    [HttpPost("publish/{noticeVersionId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Publish(
        int noticeVersionId,
        int organisationId,
        CancellationToken ct)
    {
        if (noticeVersionId <= 0)
            throw new ArgumentException(
                "Notice version id is required.");

        var orgId = ScopeOrganisation(organisationId);

        var id = await _db.ScalarIntAsync(
            "dbo.publish_consent_notice_version",
            new[]
            {
                _db.P("@NoticeVersionId", noticeVersionId),
                _db.P("@OrganisationId", orgId),
                _db.P("@PublishedBy", User.UserId())
            },
            ct);

        return Success(
            "Consent notice version published successfully.",
            new { noticeVersionId = id });
    }

    [HttpDelete("delete/{noticeVersionId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        int noticeVersionId,
        int organisationId,
        CancellationToken ct)
    {
        if (noticeVersionId <= 0)
            throw new ArgumentException(
                "Notice version id is required.");

        var orgId = ScopeOrganisation(organisationId);

        await _db.ExecuteAsync(
            "dbo.delete_consent_notice_version",
            new[]
            {
                _db.P("@NoticeVersionId", noticeVersionId),
                _db.P("@OrganisationId", orgId),
                _db.P("@ModifiedBy", User.UserId())
            },
            ct);

        return Success(
            "Consent notice draft deleted successfully.");
    }
}