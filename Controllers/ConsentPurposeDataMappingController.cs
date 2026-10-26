using DPDP_Api.Data;
using DPDP_Api.DTOs;
using DPDP_Api.Extensions;
using DPDP_Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DPDP_Api.Controllers;

[ApiController]
[Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN")]
[Route("api/consent-purpose-data")]
public sealed class ConsentPurposeDataMappingController : ControllerBase
{
    private readonly Db _db;

    public ConsentPurposeDataMappingController(Db db)
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

    [HttpPost("save")]
    public async Task<ActionResult<ApiResponse<object>>> Save(
        SaveConsentPurposeDataMappingRequest request,
        CancellationToken ct)
    {
        if (request == null)
            throw new ArgumentException("Request body is required.");

        if (request.LegalEntityId <= 0)
            throw new ArgumentException("Legal entity is required.");

        if (request.ConsentPurposeId <= 0)
            throw new ArgumentException("Consent purpose is required.");

        if (request.DataCategoryId <= 0)
            throw new ArgumentException("Data category is required.");

        if (request.DataElementId <= 0)
            throw new ArgumentException("Data element is required.");

        var organisationId = ScopeOrganisation(request.OrganisationId);

        var mappingId = await _db.ScalarIntAsync(
            "dbo.save_consent_purpose_data_mapping",
            new[]
            {
                _db.P("@OrganisationId", organisationId),
                _db.P("@LegalEntityId", request.LegalEntityId),
                _db.P("@ConsentPurposeId", request.ConsentPurposeId),
                _db.P("@DataCategoryId", request.DataCategoryId),
                _db.P("@DataElementId", request.DataElementId),
                _db.P("@CreatedBy", User.UserId())
            },
            ct);

        return Success(
            "Consent purpose data mapping saved successfully.",
            new { mappingId });
    }

    [HttpGet("list")]
    public async Task<ActionResult<ApiResponse<object>>> List(
        int organisationId,
        int consentPurposeId,
        int? legalEntityId = null,
        CancellationToken ct = default)
    {
        if (consentPurposeId <= 0)
            throw new ArgumentException("Consent purpose is required.");

        var orgId = ScopeOrganisation(organisationId);

        var rows = await _db.RowsAsync(
            "dbo.list_consent_purpose_data_mapping",
            new[]
            {
                _db.P("@OrganisationId", orgId),
                _db.P("@ConsentPurposeId", consentPurposeId),
                _db.P("@LegalEntityId", legalEntityId)
            },
            ct);

        return Success(
            "Consent purpose data mappings retrieved successfully.",
            rows);
    }

    [HttpDelete("delete/{mappingId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        int mappingId,
        int organisationId,
        CancellationToken ct)
    {
        if (mappingId <= 0)
            throw new ArgumentException("Mapping id is required.");

        var orgId = ScopeOrganisation(organisationId);

        await _db.ExecuteAsync(
            "dbo.delete_consent_purpose_data_mapping",
            new[]
            {
                _db.P("@MappingId", mappingId),
                _db.P("@OrganisationId", orgId),
                _db.P("@ModifiedBy", User.UserId())
            },
            ct);

        return Success(
            "Consent purpose data mapping deleted successfully.");
    }
}