using DPDP_Api.Data;
using DPDP_Api.DTOs;
using DPDP_Api.Extensions;
using DPDP_Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DPDP_Api.Controllers;

[ApiController]
[Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN")]
[Route("api/organisation-master")]
public sealed class OrganisationMastersController : ControllerBase
{
    private readonly Db _db;

    public OrganisationMastersController(Db db)
    {
        _db = db;
    }
    // Resolve organisation access from the authenticated user's claims.
    private int ScopeOrganisation(int? requestedOrganisationId)
    {
        if (User.IsInRole("SUPER_ADMIN"))
        {
            if (!requestedOrganisationId.HasValue || requestedOrganisationId.Value <= 0)
                throw new ArgumentException("Organisation selection is required.");

            return requestedOrganisationId.Value;
        }

        var ownOrganisationId = User.OrganisationId();

        if (!ownOrganisationId.HasValue || ownOrganisationId.Value <= 0)
            throw new UnauthorizedAccessException("Organisation access is unavailable.");

        if (requestedOrganisationId.HasValue &&
            requestedOrganisationId.Value > 0 &&
            requestedOrganisationId.Value != ownOrganisationId.Value)
            throw new UnauthorizedAccessException("Access denied.");

        return ownOrganisationId.Value;
    }

    private ActionResult<ApiResponse<object>> Success(string message, object? data = null) => Ok(ApiResponse<object>.Ok(200, message, data, HttpContext.TraceIdentifier));

    [HttpPost("create-department")]
    public async Task<ActionResult<ApiResponse<object>>> CreateDepartment(DepartmentRequest r, CancellationToken ct)
    {
        var org = ScopeOrganisation(r.OrganisationId);
        var id = await _db.ScalarIntAsync("dbo.create_department", new[] { _db.P("@OrganisationId", org), _db.P("@BusinessUnitId", r.BusinessUnitId), _db.P("@ParentDepartmentId", r.ParentDepartmentId), _db.P("@DepartmentCode", r.DepartmentCode.Trim()), _db.P("@DepartmentName", r.DepartmentName.Trim()), _db.P("@Description", r.Description), _db.P("@CreatedBy", User.UserId()) }, ct);
        return Success("Department created successfully.", new { departmentId = id });
    }

    [HttpGet("list-department")]
    public async Task<ActionResult<ApiResponse<object>>> ListDepartment(int? organisationId = null, string? search = null, CancellationToken ct = default) => Success("Departments retrieved successfully.", await _db.RowsAsync("dbo.list_department", new[] { _db.P("@OrganisationId", ScopeOrganisation(organisationId)), _db.P("@Search", search) }, ct));

    [HttpPut("update-department")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateDepartment(UpdateDepartmentRequest r, CancellationToken ct) { var org = ScopeOrganisation(r.OrganisationId); await _db.ExecuteAsync("dbo.update_department", new[] { _db.P("@DepartmentId", r.DepartmentId), _db.P("@OrganisationId", org), _db.P("@BusinessUnitId", r.BusinessUnitId), _db.P("@ParentDepartmentId", r.ParentDepartmentId), _db.P("@DepartmentCode", r.DepartmentCode.Trim()), _db.P("@DepartmentName", r.DepartmentName.Trim()), _db.P("@Description", r.Description), _db.P("@IsActive", r.IsActive), _db.P("@ModifiedBy", User.UserId()) }, ct); return Success("Department updated successfully."); }

    [HttpDelete("delete-department/{departmentId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteDepartment(int departmentId, int? organisationId = null, CancellationToken ct = default) { await _db.ExecuteAsync("dbo.delete_department", new[] { _db.P("@DepartmentId", departmentId), _db.P("@OrganisationId", ScopeOrganisation(organisationId)), _db.P("@ModifiedBy", User.UserId()) }, ct); return Success("Department deleted successfully."); }

    [HttpPost("create-designation")]
    public async Task<ActionResult<ApiResponse<object>>> CreateDesignation(DesignationRequest r, CancellationToken ct) { var org = ScopeOrganisation(r.OrganisationId); var id = await _db.ScalarIntAsync("dbo.create_designation", new[] { _db.P("@OrganisationId", org), _db.P("@DesignationCode", r.DesignationCode.Trim()), _db.P("@DesignationName", r.DesignationName.Trim()), _db.P("@Description", r.Description), _db.P("@CreatedBy", User.UserId()) }, ct); return Success("Designation created successfully.", new { designationId = id }); }

    [HttpGet("list-designation")]
    public async Task<ActionResult<ApiResponse<object>>> ListDesignation(int? organisationId = null, string? search = null, CancellationToken ct = default) => Success("Designations retrieved successfully.", await _db.RowsAsync("dbo.list_designation", new[] { _db.P("@OrganisationId", ScopeOrganisation(organisationId)), _db.P("@Search", search) }, ct));

    [HttpPut("update-designation")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateDesignation(UpdateDesignationRequest r, CancellationToken ct) { var org = ScopeOrganisation(r.OrganisationId); await _db.ExecuteAsync("dbo.update_designation", new[] { _db.P("@DesignationId", r.DesignationId), _db.P("@OrganisationId", org), _db.P("@DesignationCode", r.DesignationCode.Trim()), _db.P("@DesignationName", r.DesignationName.Trim()), _db.P("@Description", r.Description), _db.P("@IsActive", r.IsActive), _db.P("@ModifiedBy", User.UserId()) }, ct); return Success("Designation updated successfully."); }

    [HttpDelete("delete-designation/{designationId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteDesignation(int designationId, int? organisationId = null, CancellationToken ct = default) { await _db.ExecuteAsync("dbo.delete_designation", new[] { _db.P("@DesignationId", designationId), _db.P("@OrganisationId", ScopeOrganisation(organisationId)), _db.P("@ModifiedBy", User.UserId()) }, ct); return Success("Designation deleted successfully."); }

    [HttpPost("create-location")]
    public async Task<ActionResult<ApiResponse<object>>> CreateLocation(LocationRequest r, CancellationToken ct) { var org = ScopeOrganisation(r.OrganisationId); var id = await _db.ScalarIntAsync("dbo.create_location", new[] { _db.P("@OrganisationId", org), _db.P("@ParentLocationId", r.ParentLocationId), _db.P("@LocationCode", r.LocationCode.Trim()), _db.P("@LocationName", r.LocationName.Trim()), _db.P("@LocationType", r.LocationType), _db.P("@AddressLine1", r.AddressLine1), _db.P("@AddressLine2", r.AddressLine2), _db.P("@StateId", r.StateId), _db.P("@CityId", r.CityId), _db.P("@PinCode", r.PinCode), _db.P("@IsProcessingLocation", r.IsProcessingLocation), _db.P("@CreatedBy", User.UserId()) }, ct); return Success("Location created successfully.", new { locationId = id }); }

    [HttpGet("list-location")]
    public async Task<ActionResult<ApiResponse<object>>> ListLocation(int? organisationId = null, string? search = null, string? locationType = null, CancellationToken ct = default) => Success("Locations retrieved successfully.", await _db.RowsAsync("dbo.list_location", new[] { _db.P("@OrganisationId", ScopeOrganisation(organisationId)), _db.P("@Search", search), _db.P("@LocationType", locationType) }, ct));

    [HttpPut("update-location")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateLocation(UpdateLocationRequest r, CancellationToken ct) { var org = ScopeOrganisation(r.OrganisationId); await _db.ExecuteAsync("dbo.update_location", new[] { _db.P("@LocationId", r.LocationId), _db.P("@OrganisationId", org), _db.P("@ParentLocationId", r.ParentLocationId), _db.P("@LocationCode", r.LocationCode.Trim()), _db.P("@LocationName", r.LocationName.Trim()), _db.P("@LocationType", r.LocationType), _db.P("@AddressLine1", r.AddressLine1), _db.P("@AddressLine2", r.AddressLine2), _db.P("@StateId", r.StateId), _db.P("@CityId", r.CityId), _db.P("@PinCode", r.PinCode), _db.P("@IsProcessingLocation", r.IsProcessingLocation), _db.P("@IsActive", r.IsActive), _db.P("@ModifiedBy", User.UserId()) }, ct); return Success("Location updated successfully."); }

    [HttpDelete("delete-location/{locationId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteLocation(int locationId, int? organisationId = null, CancellationToken ct = default) { await _db.ExecuteAsync("dbo.delete_location", new[] { _db.P("@LocationId", locationId), _db.P("@OrganisationId", ScopeOrganisation(organisationId)), _db.P("@ModifiedBy", User.UserId()) }, ct); return Success("Location deleted successfully."); }

    [HttpPost("create-business-unit")]
    public async Task<ActionResult<ApiResponse<object>>> CreateBusinessUnit(BusinessUnitRequest r, CancellationToken ct)
    {
        var org = ScopeOrganisation(r.OrganisationId);
        var id = await _db.ScalarIntAsync("dbo.create_business_unit", new[]{_db.P("@OrganisationId",org)
            ,_db.P("@BusinessUnitCode",r.BusinessUnitCode.Trim()),_db.P("@BusinessUnitName",r.BusinessUnitName.Trim()),_db.P("@Description",r.Description),_db.P("@CreatedBy",User.UserId())}, ct); return Success("Business unit created successfully.", new { businessUnitId = id });
    }

    [HttpGet("list-business-unit")]
    public async Task<ActionResult<ApiResponse<object>>> ListBusinessUnit(int? organisationId = null, string? search = null, CancellationToken ct = default) => Success("Business units retrieved successfully.", await _db.RowsAsync("dbo.list_business_unit", new[] { _db.P("@OrganisationId", ScopeOrganisation(organisationId)), _db.P("@Search", search) }, ct));

    [HttpPut("update-business-unit")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateBusinessUnit(UpdateBusinessUnitRequest r, CancellationToken ct) { var org = ScopeOrganisation(r.OrganisationId); await _db.ExecuteAsync("dbo.update_business_unit", new[] { _db.P("@BusinessUnitId", r.BusinessUnitId), _db.P("@OrganisationId", org), _db.P("@BusinessUnitCode", r.BusinessUnitCode.Trim()), _db.P("@BusinessUnitName", r.BusinessUnitName.Trim()), _db.P("@Description", r.Description), _db.P("@IsActive", r.IsActive), _db.P("@ModifiedBy", User.UserId()) }, ct); return Success("Business unit updated successfully."); }

    [HttpDelete("delete-business-unit/{businessUnitId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteBusinessUnit(int businessUnitId, int? organisationId = null, CancellationToken ct = default) { await _db.ExecuteAsync("dbo.delete_business_unit", new[] { _db.P("@BusinessUnitId", businessUnitId), _db.P("@OrganisationId", ScopeOrganisation(organisationId)), _db.P("@ModifiedBy", User.UserId()) }, ct); return Success("Business unit deleted successfully."); }

    [HttpPost("create-legal-entity")]
    public async Task<ActionResult<ApiResponse<object>>> CreateLegalEntity(LegalEntityRequest r, CancellationToken ct) { var org = ScopeOrganisation(r.OrganisationId); var id = await _db.ScalarIntAsync("dbo.create_legal_entity", new[] { _db.P("@OrganisationId", org), _db.P("@LegalEntityCode", r.LegalEntityCode.Trim()), _db.P("@LegalEntityName", r.LegalEntityName.Trim()), _db.P("@EntityType", r.EntityType), _db.P("@CIN", r.CIN), _db.P("@PAN", r.PAN), _db.P("@RegisteredAddress", r.RegisteredAddress), _db.P("@StateId", r.StateId), _db.P("@CityId", r.CityId), _db.P("@PinCode", r.PinCode), _db.P("@CreatedBy", User.UserId()) }, ct); return Success("Legal entity created successfully.", new { legalEntityId = id }); }

    [HttpGet("list-legal-entity")]
    public async Task<ActionResult<ApiResponse<object>>> ListLegalEntity(int? organisationId = null, string? search = null, CancellationToken ct = default) => Success("Legal entities retrieved successfully.", await _db.RowsAsync("dbo.list_legal_entity", new[] { _db.P("@OrganisationId", ScopeOrganisation(organisationId)), _db.P("@Search", search) }, ct));

    [HttpPut("update-legal-entity")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateLegalEntity(UpdateLegalEntityRequest r, CancellationToken ct) { var org = ScopeOrganisation(r.OrganisationId); await _db.ExecuteAsync("dbo.update_legal_entity", new[] { _db.P("@LegalEntityId", r.LegalEntityId), _db.P("@OrganisationId", org), _db.P("@LegalEntityCode", r.LegalEntityCode.Trim()), _db.P("@LegalEntityName", r.LegalEntityName.Trim()), _db.P("@EntityType", r.EntityType), _db.P("@CIN", r.CIN), _db.P("@PAN", r.PAN), _db.P("@RegisteredAddress", r.RegisteredAddress), _db.P("@StateId", r.StateId), _db.P("@CityId", r.CityId), _db.P("@PinCode", r.PinCode), _db.P("@IsActive", r.IsActive), _db.P("@ModifiedBy", User.UserId()) }, ct); return Success("Legal entity updated successfully."); }

    [HttpDelete("delete-legal-entity/{legalEntityId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteLegalEntity(int legalEntityId, int? organisationId = null, CancellationToken ct = default) { await _db.ExecuteAsync("dbo.delete_legal_entity", new[] { _db.P("@LegalEntityId", legalEntityId), _db.P("@OrganisationId", ScopeOrganisation(organisationId)), _db.P("@ModifiedBy", User.UserId()) }, ct); return Success("Legal entity deleted successfully."); }

    [HttpPost("create-subsidiary")]
    public async Task<ActionResult<ApiResponse<object>>> CreateSubsidiary(SubsidiaryRequest r, CancellationToken ct) { var org = ScopeOrganisation(r.OrganisationId); var id = await _db.ScalarIntAsync("dbo.create_subsidiary", new[] { _db.P("@OrganisationId", org), _db.P("@LegalEntityId", r.LegalEntityId), _db.P("@SubsidiaryCode", r.SubsidiaryCode.Trim()), _db.P("@SubsidiaryName", r.SubsidiaryName.Trim()), _db.P("@CountryCode", r.CountryCode), _db.P("@OwnershipPercent", r.OwnershipPercent), _db.P("@CreatedBy", User.UserId()) }, ct); return Success("Subsidiary created successfully.", new { subsidiaryId = id }); }

    [HttpGet("list-subsidiary")]
    public async Task<ActionResult<ApiResponse<object>>> ListSubsidiary(int? organisationId = null, string? search = null, CancellationToken ct = default) => Success("Subsidiaries retrieved successfully.", await _db.RowsAsync("dbo.list_subsidiary", new[] { _db.P("@OrganisationId", ScopeOrganisation(organisationId)), _db.P("@Search", search) }, ct));

    [HttpPut("update-subsidiary")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateSubsidiary(UpdateSubsidiaryRequest r, CancellationToken ct) { var org = ScopeOrganisation(r.OrganisationId); await _db.ExecuteAsync("dbo.update_subsidiary", new[] { _db.P("@SubsidiaryId", r.SubsidiaryId), _db.P("@OrganisationId", org), _db.P("@LegalEntityId", r.LegalEntityId), _db.P("@SubsidiaryCode", r.SubsidiaryCode.Trim()), _db.P("@SubsidiaryName", r.SubsidiaryName.Trim()), _db.P("@CountryCode", r.CountryCode), _db.P("@OwnershipPercent", r.OwnershipPercent), _db.P("@IsActive", r.IsActive), _db.P("@ModifiedBy", User.UserId()) }, ct); return Success("Subsidiary updated successfully."); }

    [HttpDelete("delete-subsidiary/{subsidiaryId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteSubsidiary(int subsidiaryId, int? organisationId = null, CancellationToken ct = default) { await _db.ExecuteAsync("dbo.delete_subsidiary", new[] { _db.P("@SubsidiaryId", subsidiaryId), _db.P("@OrganisationId", ScopeOrganisation(organisationId)), _db.P("@ModifiedBy", User.UserId()) }, ct); return Success("Subsidiary deleted successfully."); }
}
