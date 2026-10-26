using DPDP_Api.Data;
using DPDP_Api.DTOs;
using DPDP_Api.Extensions;
using DPDP_Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace DPDP_Api.Controllers;

[ApiController]
[Authorize(Roles = "SUPER_ADMIN")]
[Route("api/organisation")]
public sealed class OrganisationController : ControllerBase
{
    private readonly Db _db;

    public OrganisationController(Db db)
    {
        _db = db;
    }

    [HttpPost("create-organisation")]
    public async Task<ActionResult<ApiResponse<object>>> CreateOrganisation(
        CreateOrganisationRequest request,
        CancellationToken ct)
    {
        var parameters = GetCreateParameters(
            request,
            User.UserId());

        var organisationId = await _db.ScalarIntAsync(
            "dbo.create_organisation",
            parameters,
            ct);

        return Ok(
            ApiResponse<object>.Ok(
                200,
                "Organisation created successfully.",
                new
                {
                    organisationId
                },
                HttpContext.TraceIdentifier));
    }

    [HttpGet("list-organisation")]
    public async Task<ActionResult<ApiResponse<object>>> ListOrganisation( string? search = null, int pageNumber = 1,int pageSize = 20, CancellationToken ct = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var parameters = new[]
        {
            _db.P("@Search", search),
            _db.P("@PageNumber", pageNumber),
            _db.P("@PageSize", pageSize)
        };

        var rows = await _db.RowsAsync(
            "dbo.list_organisation",
            parameters,
            ct);

        return Ok(
            ApiResponse<object>.Ok(
                200,
                "Organisations retrieved successfully.",
                rows,
                HttpContext.TraceIdentifier));
    }

    [HttpGet("get-organisation/{organisationId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> GetOrganisation(
        int organisationId,
        CancellationToken ct)
    {
        var rows = await _db.RowsAsync(
            "dbo.get_organisation",
            new[]
            {
                _db.P("@OrganisationId", organisationId)
            },
            ct);

        if (rows.Count == 0)
        {
            return NotFound(
                ApiResponse<object>.Fail(
                    404,
                    "Organisation not found.",
                    HttpContext.TraceIdentifier));
        }

        return Ok(
            ApiResponse<object>.Ok(
                200,
                "Organisation retrieved successfully.",
                rows[0],
                HttpContext.TraceIdentifier));
    }

    [HttpPut("update-organisation")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateOrganisation(
        UpdateOrganisationRequest request,
        CancellationToken ct)
    {
        var parameters = new[]
        {
            _db.P("@OrganisationId", request.OrganisationId),
            _db.P("@OrganisationCode", request.OrganisationCode.Trim()),
            _db.P("@OrganisationName", request.OrganisationName.Trim()),
            _db.P("@OrganisationType", request.OrganisationType),
            _db.P("@Industry", request.Industry),
            _db.P("@RegisteredAddress", request.RegisteredAddress),
            _db.P("@StateId", request.StateId),
            _db.P("@CityId", request.CityId),
            _db.P("@PinCode", request.PinCode),
            _db.P("@ContactEmail", request.ContactEmail),
            _db.P("@ContactNumber", request.ContactNumber),
            _db.P("@IsActive", request.IsActive),
            _db.P("@ModifiedBy", User.UserId()),
            _db.P("@StartDate", request.StartDate),
            _db.P("@EndDate", request.EndDate),
        };

        await _db.ExecuteAsync(
            "dbo.update_organisation",
            parameters,
            ct);

        return Ok(
            ApiResponse<object>.Ok(
                200,
                "Organisation updated successfully.",
                null,
                HttpContext.TraceIdentifier));
    }

    private SqlParameter[] GetCreateParameters(
        CreateOrganisationRequest request,
        int userId)
    {
        return new[]
        {
            _db.P("@OrganisationCode", request.OrganisationCode.Trim()),
            _db.P("@OrganisationName", request.OrganisationName.Trim()),
            _db.P("@OrganisationType", request.OrganisationType),
            _db.P("@Industry", request.Industry),
            _db.P("@RegisteredAddress", request.RegisteredAddress),
            _db.P("@StateId", request.StateId),
            _db.P("@CityId", request.CityId),
            _db.P("@PinCode", request.PinCode),
            _db.P("@ContactEmail", request.ContactEmail),
            _db.P("@ContactNumber", request.ContactNumber),
            _db.P("@CreatedBy", userId),
             _db.P("@StartDate", request.StartDate),
            _db.P("@EndDate", request.EndDate),
        };
    }
}