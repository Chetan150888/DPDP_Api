using DPDP_Api.Data;
using DPDP_Api.DTOs;
using DPDP_Api.Extensions;
using DPDP_Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DPDP_Api.Controllers;

[ApiController]
[Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN")]
[Route("api/consent-purpose")]
public sealed class ConsentPurposeController : ControllerBase
{
    private readonly Db _db;

    public ConsentPurposeController(Db db)
    {
        _db = db;
    }

    private int ScopeOrganisation(int requestedOrganisationId)
    {
        if (User.RoleCode() == "SUPER_ADMIN")
        {
            if (requestedOrganisationId <= 0)
                throw new ArgumentException("Organisation selection is required.");

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
        CreateConsentPurposeRequest request,
        CancellationToken ct)
    {
        if (request == null)
            throw new ArgumentException("Request body is required.");

        if (request.LegalEntityId <= 0)
            throw new ArgumentException("Legal entity is required.");

        if (string.IsNullOrWhiteSpace(request.PurposeCode))
            throw new ArgumentException("Purpose code is required.");

        if (string.IsNullOrWhiteSpace(request.PurposeName))
            throw new ArgumentException("Purpose name is required.");

        if (string.IsNullOrWhiteSpace(request.ProcessingBasis))
            throw new ArgumentException("Processing basis is required.");

        var organisationId = ScopeOrganisation(request.OrganisationId);

        var consentPurposeId = await _db.ScalarIntAsync(
            "dbo.create_consent_purpose",
            new[]
            {
                _db.P("@OrganisationId", organisationId),
                _db.P("@LegalEntityId", request.LegalEntityId),
                _db.P("@PurposeCode", request.PurposeCode.Trim()),
                _db.P("@PurposeName", request.PurposeName.Trim()),
                _db.P("@Description", request.Description),
                _db.P("@ProcessingBasis", request.ProcessingBasis.Trim()),
                _db.P("@IsConsentRequired", request.IsConsentRequired),
                _db.P("@CreatedBy", User.UserId())
            },
            ct);

        return Success(
            "Consent purpose created successfully.",
            new { consentPurposeId });
    }

    [HttpGet("list")]
    public async Task<ActionResult<ApiResponse<object>>> List(
        int organisationId,
        int? legalEntityId = null,
        string? search = null,
        bool? isActive = null,
        CancellationToken ct = default)
    {
        var orgId = ScopeOrganisation(organisationId);

        var rows = await _db.RowsAsync(
            "dbo.list_consent_purpose",
            new[]
            {
                _db.P("@OrganisationId", orgId),
                _db.P("@LegalEntityId", legalEntityId),
                _db.P("@Search", search),
                _db.P("@IsActive", isActive)
            },
            ct);

        return Success("Consent purposes retrieved successfully.", rows);
    }

    [HttpPut("update")]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        UpdateConsentPurposeRequest request,
        CancellationToken ct)
    {
        if (request == null)
            throw new ArgumentException("Request body is required.");

        if (request.ConsentPurposeId <= 0)
            throw new ArgumentException("Consent purpose id is required.");

        if (request.LegalEntityId <= 0)
            throw new ArgumentException("Legal entity is required.");

        if (string.IsNullOrWhiteSpace(request.PurposeCode))
            throw new ArgumentException("Purpose code is required.");

        if (string.IsNullOrWhiteSpace(request.PurposeName))
            throw new ArgumentException("Purpose name is required.");

        if (string.IsNullOrWhiteSpace(request.ProcessingBasis))
            throw new ArgumentException("Processing basis is required.");

        var organisationId = ScopeOrganisation(request.OrganisationId);

        await _db.ExecuteAsync(
            "dbo.update_consent_purpose",
            new[]
            {
                _db.P("@ConsentPurposeId", request.ConsentPurposeId),
                _db.P("@OrganisationId", organisationId),
                _db.P("@LegalEntityId", request.LegalEntityId),
                _db.P("@PurposeCode", request.PurposeCode.Trim()),
                _db.P("@PurposeName", request.PurposeName.Trim()),
                _db.P("@Description", request.Description),
                _db.P("@ProcessingBasis", request.ProcessingBasis.Trim()),
                _db.P("@IsConsentRequired", request.IsConsentRequired),
                _db.P("@IsActive", request.IsActive),
                _db.P("@ModifiedBy", User.UserId())
            },
            ct);

        return Success("Consent purpose updated successfully.");
    }

    [HttpDelete("delete/{consentPurposeId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        int consentPurposeId,
        int organisationId,
        CancellationToken ct)
    {
        if (consentPurposeId <= 0)
            throw new ArgumentException("Consent purpose id is required.");

        var orgId = ScopeOrganisation(organisationId);

        await _db.ExecuteAsync(
            "dbo.delete_consent_purpose",
            new[]
            {
                _db.P("@ConsentPurposeId", consentPurposeId),
                _db.P("@OrganisationId", orgId),
                _db.P("@ModifiedBy", User.UserId())
            },
            ct);

        return Success("Consent purpose deleted successfully.");
    }
}