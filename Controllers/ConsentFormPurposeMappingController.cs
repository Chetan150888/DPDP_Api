using DPDP_Api.Data;
using DPDP_Api.DTOs;
using DPDP_Api.Extensions;
using DPDP_Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DPDP_Api.Controllers;

[ApiController]
[Route("api/consent-form-purpose")]
[Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN")]
public sealed class ConsentFormPurposeMappingController : ControllerBase
{
    private readonly Db _db;

    public ConsentFormPurposeMappingController(Db db)
    {
        _db = db;
    }

    [HttpPost("save")]
    public async Task<ActionResult<ApiResponse<object>>> Save(
        [FromBody] SaveConsentFormPurposeMappingRequest request,
        CancellationToken ct)
    {
        var organisationId = ScopeOrganisation(request.OrganisationId);

        if (request.LegalEntityId <= 0)
            throw new ArgumentException("Legal entity is required.");

        if (request.FormId <= 0)
            throw new ArgumentException("Form is required.");

        if (request.ConsentPurposeId <= 0)
            throw new ArgumentException("Consent purpose is required.");

        if (request.DataCategoryId <= 0)
            throw new ArgumentException("Data category is required.");

        if (request.DataElementId <= 0)
            throw new ArgumentException("Data element is required.");

        var mappingId = await _db.ScalarIntAsync(
            "dbo.save_consent_form_purpose_mapping",
            new[]
            {
                _db.P("@OrganisationId", organisationId),
                _db.P("@LegalEntityId", request.LegalEntityId),
                _db.P("@FormId", request.FormId),
                _db.P("@ConsentPurposeId", request.ConsentPurposeId),
                _db.P("@DataCategoryId", request.DataCategoryId),
                _db.P("@DataElementId", request.DataElementId),
                _db.P("@CreatedBy", User.UserId())
            },
            ct);

        return Success(
            "Consent form purpose mapping saved successfully.",
            new { mappingId });
    }

    [HttpGet("list")]
    public async Task<ActionResult<ApiResponse<object>>> List(
        [FromQuery] int organisationId,
        [FromQuery] int formId,
        CancellationToken ct = default)
    {
        organisationId = ScopeOrganisation(organisationId);

        if (formId <= 0)
            throw new ArgumentException("Valid form id is required.");

        var rows = await _db.RowsAsync(
            "dbo.list_consent_form_purpose_mapping",
            new[]
            {
                _db.P("@OrganisationId", organisationId),
                _db.P("@FormId", formId)
            },
            ct);

        return Success(
            "Consent form purpose mappings retrieved successfully.",
            rows);
    }

    [HttpDelete("delete/{mappingId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        int mappingId,
        [FromQuery] int organisationId,
        CancellationToken ct)
    {
        organisationId = ScopeOrganisation(organisationId);

        if (mappingId <= 0)
            throw new ArgumentException("Valid mapping id is required.");

        await _db.ExecuteAsync(
            "dbo.delete_consent_form_purpose_mapping",
            new[]
            {
                _db.P("@MappingId", mappingId),
                _db.P("@OrganisationId", organisationId),
                _db.P("@ModifiedBy", User.UserId())
            },
            ct);

        return Success(
            "Consent form purpose mapping deleted successfully.");
    }

    [HttpDelete("delete-all/{formId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAll(
        int formId,
        [FromQuery] int organisationId,
        CancellationToken ct)
    {
        organisationId = ScopeOrganisation(organisationId);

        if (formId <= 0)
            throw new ArgumentException("Valid form id is required.");

        await _db.ExecuteAsync(
            "dbo.delete_consent_form_all_mapping",
            new[]
            {
                _db.P("@FormId", formId),
                _db.P("@OrganisationId", organisationId),
                _db.P("@ModifiedBy", User.UserId())
            },
            ct);

        return Success(
            "All consent form mappings deleted successfully.");
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

        if (!organisationId.HasValue ||
            organisationId.Value <= 0)
        {
            throw new UnauthorizedAccessException(
                "Organisation access is not available for this account.");
        }

        if (requestedOrganisationId > 0 &&
            requestedOrganisationId != organisationId.Value)
        {
            throw new UnauthorizedAccessException("Access denied.");
        }

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
}