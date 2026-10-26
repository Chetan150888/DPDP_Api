using DPDP_Api.Data;
using DPDP_Api.DTOs;
using DPDP_Api.Extensions;
using DPDP_Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DPDP_Api.Controllers;

[ApiController]
[Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN")]
[Route("api/data-element-category")]
public sealed class DataElementCategoryController : ControllerBase
{
    private readonly Db _db;
    public DataElementCategoryController(Db db) => _db = db;

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

        if (requestedOrganisationId.HasValue && requestedOrganisationId.Value > 0 &&
            requestedOrganisationId.Value != ownOrganisationId.Value)
            throw new UnauthorizedAccessException("Access denied.");

        return ownOrganisationId.Value;
    }

    private ActionResult<ApiResponse<object>> Success(string message, object? data = null)
        => Ok(ApiResponse<object>.Ok(200, message, data, HttpContext.TraceIdentifier));

    [HttpPost("create")]
    public async Task<ActionResult<ApiResponse<object>>> Create(DataElementCategoryRequest r, CancellationToken ct)
    {
        var org = ScopeOrganisation(r.OrganisationId);
        var id = await _db.ScalarIntAsync("dbo.create_data_element_category", new[] {
            _db.P("@OrganisationId",org), _db.P("@CategoryCode",r.CategoryCode.Trim()),
            _db.P("@CategoryName",r.CategoryName.Trim()), _db.P("@Description",r.Description?.Trim()),
            _db.P("@CreatedBy",User.UserId()) }, ct);
        return Success("Data element category created successfully.", new { categoryId = id });
    }

    [HttpGet("list")]
    public async Task<ActionResult<ApiResponse<object>>> List(int? organisationId=null,string? search=null,CancellationToken ct=default)
        => Success("Data element categories retrieved successfully.",
            await _db.RowsAsync("dbo.list_data_element_category", new[] {
                _db.P("@OrganisationId",ScopeOrganisation(organisationId)), _db.P("@Search",search?.Trim()) }, ct));

    [HttpPut("update")]
    public async Task<ActionResult<ApiResponse<object>>> Update(UpdateDataElementCategoryRequest r,CancellationToken ct)
    {
        var org=ScopeOrganisation(r.OrganisationId);
        await _db.ExecuteAsync("dbo.update_data_element_category",new[] {
            _db.P("@CategoryId",r.CategoryId),_db.P("@OrganisationId",org),
            _db.P("@CategoryCode",r.CategoryCode.Trim()),_db.P("@CategoryName",r.CategoryName.Trim()),
            _db.P("@Description",r.Description?.Trim()),_db.P("@IsActive",r.IsActive),
            _db.P("@ModifiedBy",User.UserId())},ct);
        return Success("Data element category updated successfully.");
    }

    [HttpDelete("delete/{categoryId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int categoryId,int? organisationId=null,CancellationToken ct=default)
    {
        await _db.ExecuteAsync("dbo.delete_data_element_category",new[] {
            _db.P("@CategoryId",categoryId),_db.P("@OrganisationId",ScopeOrganisation(organisationId)),
            _db.P("@ModifiedBy",User.UserId())},ct);
        return Success("Data element category deleted successfully.");
    }
}
