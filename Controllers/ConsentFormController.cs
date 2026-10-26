using DPDP_Api.Data;
using DPDP_Api.DTOs;
using DPDP_Api.Extensions;
using DPDP_Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DPDP_Api.Controllers;

[ApiController]
[Route("api/consent-form")]
[Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN")]
public sealed class ConsentFormController : ControllerBase
{
    private readonly Db _db;

    public ConsentFormController(Db db)
    {
        _db = db;
    }

    [HttpPost("create")]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        [FromBody] CreateConsentFormRequest request,
        CancellationToken ct)
    {
        var organisationId = ScopeOrganisation(request.OrganisationId);

        if (request.LegalEntityId <= 0)
            throw new ArgumentException("Legal entity is required.");

        if (string.IsNullOrWhiteSpace(request.FormName))
            throw new ArgumentException("Form name is required.");

        if (string.IsNullOrWhiteSpace(request.FormUrl))
            throw new ArgumentException("Form URL is required.");

        var formId = await _db.ScalarIntAsync(
            "dbo.create_consent_form",
            new[]
            {
                _db.P("@OrganisationId", organisationId),
                _db.P("@LegalEntityId", request.LegalEntityId),
                _db.P("@FormName", request.FormName.Trim()),
                _db.P("@FormUrl", request.FormUrl.Trim()),
                _db.P("@Tier", request.Tier),
                _db.P("@Remark", request.Remark),
                _db.P("@CreatedBy", User.UserId())
            },
            ct);

        return Success(
            "Consent form created successfully.",
            new { formId });
    }

    [HttpGet("list")]
    public async Task<ActionResult<ApiResponse<object>>> List(
        [FromQuery] int organisationId,
        [FromQuery] int? legalEntityId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        organisationId = ScopeOrganisation(organisationId);

        var rows = await _db.RowsAsync(
            "dbo.list_consent_form",
            new[]
            {
                _db.P("@OrganisationId", organisationId),
                _db.P("@LegalEntityId", legalEntityId),
                _db.P("@Status", status),
                _db.P("@Search", search)
            },
            ct);

        return Success(
            "Consent forms retrieved successfully.",
            rows);
    }

    [HttpGet("{formId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Get(
        int formId,
        [FromQuery] int organisationId,
        CancellationToken ct)
    {
        organisationId = ScopeOrganisation(organisationId);

        if (formId <= 0)
            throw new ArgumentException("Valid form id is required.");

        var rows = await _db.RowsAsync(
            "dbo.get_consent_form",
            new[]
            {
                _db.P("@OrganisationId", organisationId),
                _db.P("@FormId", formId)
            },
            ct);

        return Success(
            "Consent form retrieved successfully.",
            rows);
    }

    [HttpPut("update")]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        [FromBody] UpdateConsentFormRequest request,
        CancellationToken ct)
    {
        var organisationId = ScopeOrganisation(request.OrganisationId);

        if (request.FormId <= 0)
            throw new ArgumentException("Valid form id is required.");

        if (request.LegalEntityId <= 0)
            throw new ArgumentException("Legal entity is required.");

        if (string.IsNullOrWhiteSpace(request.FormName))
            throw new ArgumentException("Form name is required.");

        if (string.IsNullOrWhiteSpace(request.FormUrl))
            throw new ArgumentException("Form URL is required.");

        if (string.IsNullOrWhiteSpace(request.Status))
            throw new ArgumentException("Status is required.");

        var formId = await _db.ScalarIntAsync(
            "dbo.update_consent_form",
            new[]
            {
                _db.P("@FormId", request.FormId),
                _db.P("@OrganisationId", organisationId),
                _db.P("@LegalEntityId", request.LegalEntityId),
                _db.P("@FormName", request.FormName.Trim()),
                _db.P("@FormUrl", request.FormUrl.Trim()),
                _db.P("@Tier", request.Tier),
                _db.P("@Status", request.Status.Trim()),
                _db.P("@Remark", request.Remark),
                _db.P("@ModifiedBy", User.UserId())
            },
            ct);

        return Success(
            "Consent form updated successfully.",
            new { formId });
    }

    [HttpDelete("delete/{formId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        int formId,
        [FromQuery] int organisationId,
        CancellationToken ct)
    {
        organisationId = ScopeOrganisation(organisationId);

        if (formId <= 0)
            throw new ArgumentException("Valid form id is required.");

        await _db.ExecuteAsync(
            "dbo.delete_consent_form",
            new[]
            {
                _db.P("@FormId", formId),
                _db.P("@OrganisationId", organisationId),
                _db.P("@ModifiedBy", User.UserId())
            },
            ct);

        return Success("Consent form deleted successfully.");
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