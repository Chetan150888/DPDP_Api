using DPDP_Api.Data;
using DPDP_Api.DTOs;
using DPDP_Api.Extensions;
using DPDP_Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DPDP_Api.Controllers;

[ApiController]
[Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN")]
[Route("api/consent-notice")]
public sealed class ConsentNoticeController : ControllerBase
{
    private readonly Db _db;

    public ConsentNoticeController(Db db)
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
        CreateConsentNoticeRequest request,
        CancellationToken ct)
    {
        if (request == null)
            throw new ArgumentException("Request body is required.");

        if (request.LegalEntityId <= 0)
            throw new ArgumentException("Legal entity is required.");

        if (request.ConsentPurposeId <= 0)
            throw new ArgumentException("Consent purpose is required.");

        if (string.IsNullOrWhiteSpace(request.NoticeCode))
            throw new ArgumentException("Notice code is required.");

        if (string.IsNullOrWhiteSpace(request.NoticeName))
            throw new ArgumentException("Notice name is required.");

        var organisationId = ScopeOrganisation(request.OrganisationId);

        var consentNoticeId = await _db.ScalarIntAsync(
            "dbo.create_consent_notice",
            new[]
            {
                _db.P("@OrganisationId", organisationId),
                _db.P("@LegalEntityId", request.LegalEntityId),
                _db.P("@ConsentPurposeId", request.ConsentPurposeId),
                _db.P("@NoticeCode", request.NoticeCode.Trim()),
                _db.P("@NoticeName", request.NoticeName.Trim()),
                _db.P("@Description", request.Description),
                _db.P("@CreatedBy", User.UserId())
            },
            ct);

        return Success(
            "Consent notice created successfully.",
            new { consentNoticeId });
    }

    [HttpGet("list")]
    public async Task<ActionResult<ApiResponse<object>>> List(
        int organisationId,
        int? legalEntityId = null,
        int? consentPurposeId = null,
        string? search = null,
        bool? isActive = null,
        CancellationToken ct = default)
    {
        var orgId = ScopeOrganisation(organisationId);

        var rows = await _db.RowsAsync(
            "dbo.list_consent_notice",
            new[]
            {
                _db.P("@OrganisationId", orgId),
                _db.P("@LegalEntityId", legalEntityId),
                _db.P("@ConsentPurposeId", consentPurposeId),
                _db.P("@Search", search),
                _db.P("@IsActive", isActive)
            },
            ct);

        return Success(
            "Consent notices retrieved successfully.",
            rows);
    }

    [HttpPut("update")]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        UpdateConsentNoticeRequest request,
        CancellationToken ct)
    {
        if (request == null)
            throw new ArgumentException("Request body is required.");

        if (request.ConsentNoticeId <= 0)
            throw new ArgumentException("Consent notice id is required.");

        if (request.LegalEntityId <= 0)
            throw new ArgumentException("Legal entity is required.");

        if (request.ConsentPurposeId <= 0)
            throw new ArgumentException("Consent purpose is required.");

        if (string.IsNullOrWhiteSpace(request.NoticeCode))
            throw new ArgumentException("Notice code is required.");

        if (string.IsNullOrWhiteSpace(request.NoticeName))
            throw new ArgumentException("Notice name is required.");

        var organisationId = ScopeOrganisation(request.OrganisationId);

        var consentNoticeId = await _db.ScalarIntAsync(
            "dbo.update_consent_notice",
            new[]
            {
                _db.P("@ConsentNoticeId", request.ConsentNoticeId),
                _db.P("@OrganisationId", organisationId),
                _db.P("@LegalEntityId", request.LegalEntityId),
                _db.P("@ConsentPurposeId", request.ConsentPurposeId),
                _db.P("@NoticeCode", request.NoticeCode.Trim()),
                _db.P("@NoticeName", request.NoticeName.Trim()),
                _db.P("@Description", request.Description),
                _db.P("@IsActive", request.IsActive),
                _db.P("@ModifiedBy", User.UserId())
            },
            ct);

        return Success(
            "Consent notice updated successfully.",
            new { consentNoticeId });
    }

    [HttpDelete("delete/{consentNoticeId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        int consentNoticeId,
        int organisationId,
        CancellationToken ct)
    {
        if (consentNoticeId <= 0)
            throw new ArgumentException("Consent notice id is required.");

        var orgId = ScopeOrganisation(organisationId);

        await _db.ExecuteAsync(
            "dbo.delete_consent_notice",
            new[]
            {
                _db.P("@ConsentNoticeId", consentNoticeId),
                _db.P("@OrganisationId", orgId),
                _db.P("@ModifiedBy", User.UserId())
            },
            ct);

        return Success("Consent notice deleted successfully.");
    }
}