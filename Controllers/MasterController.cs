using DPDP_Api.Data;
using DPDP_Api.DTOs;
using DPDP_Api.Extensions;
using DPDP_Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DPDP_Api.Controllers;

[ApiController]
[Authorize]
[Route("api/master")]
public sealed class MasterController : ControllerBase
{
    private readonly Db _db;

    public MasterController(Db db)
    {
        _db = db;
    }

    [HttpGet("list-state")]
    public async Task<ActionResult<ApiResponse<object>>> ListState(
        string? search = null,
        CancellationToken ct = default)
    {
        var rows = await _db.RowsAsync(
            "dbo.list_state",
            new[]
            {
                _db.P("@Search", search)
            },
            ct);

        return Ok(
            ApiResponse<object>.Ok(
                200,
                "States retrieved successfully.",
                rows,
                HttpContext.TraceIdentifier));
    }

    //[HttpGet("get-state/{stateId:int}")]
    //public async Task<ActionResult<ApiResponse<object>>> GetState(
    //    int stateId,
    //    CancellationToken ct)
    //{
    //    var rows = await _db.RowsAsync(
    //        "dbo.get_state",
    //        new[]
    //        {
    //            _db.P("@StateId", stateId)
    //        },
    //        ct);
    //
    //    if (rows.Count == 0)
    //    {
    //        return NotFound(
    //            ApiResponse<object>.Fail(
    //                404,
    //                "State not found.",
    //                HttpContext.TraceIdentifier));
    //    }
    //
    //    return Ok(
    //        ApiResponse<object>.Ok(
    //            200,
    //            "State retrieved successfully.",
    //            rows[0],
    //            HttpContext.TraceIdentifier));
    //}

    //[Authorize(Roles = "SUPER_ADMIN")]
    //[HttpPost("create-state")]
    //public async Task<ActionResult<ApiResponse<object>>> CreateState(
    //    CreateStateRequest request,
    //    CancellationToken ct)
    //{
    //    var id = await _db.ScalarIntAsync(
    //        "dbo.create_state",
    //        new[]
    //        {
    //            _db.P("@StateCode", request.StateCode.Trim()),
    //            _db.P("@StateName", request.StateName.Trim()),
    //            _db.P("@CreatedBy", User.UserId())
    //        },
    //        ct);
    //
    //    return Ok(
    //        ApiResponse<object>.Ok(
    //            200,
    //            "State created successfully.",
    //            new { stateId = id },
    //            HttpContext.TraceIdentifier));
    //}

    //[Authorize(Roles = "SUPER_ADMIN")]
    //[HttpPut("update-state")]
    //public async Task<ActionResult<ApiResponse<object>>> UpdateState(
    //    UpdateStateRequest request,
    //    CancellationToken ct)
    //{
    //    await _db.ExecuteAsync(
    //        "dbo.update_state",
    //        new[]
    //        {
    //            _db.P("@StateId", request.StateId),
    //            _db.P("@StateCode", request.StateCode.Trim()),
    //            _db.P("@StateName", request.StateName.Trim()),
    //            _db.P("@IsActive", request.IsActive),
    //            _db.P("@ModifiedBy", User.UserId())
    //        },
    //        ct);
    //
    //    return Ok(
    //        ApiResponse<object>.Ok(
    //            200,
    //            "State updated successfully.",
    //            null,
    //            HttpContext.TraceIdentifier));
    //}

    [HttpGet("list-city")]
    //public async Task<ActionResult<ApiResponse<object>>> ListCity(int? stateId = null, string? search = null, CancellationToken ct = default)
    public async Task<ActionResult<ApiResponse<object>>> ListCity(int? stateId = null,string? search = null,CancellationToken ct = default)
    {
        var rows = await _db.RowsAsync("dbo.list_city", new[]
            {
                _db.P("@StateId", stateId),
                _db.P("@Search", search)
            },
            ct);

        return Ok(
            ApiResponse<object>.Ok(
                200,
                "Cities retrieved successfully.",
                rows,
                HttpContext.TraceIdentifier));
    }

    //[HttpGet("get-city/{cityId:int}")]
    //public async Task<ActionResult<ApiResponse<object>>> GetCity(
    //    int cityId,
    //    CancellationToken ct)
    //{
    //    var rows = await _db.RowsAsync(
    //        "dbo.get_city",
    //        new[]
    //        {
    //            _db.P("@CityId", cityId)
    //        },
    //        ct);
    //
    //    if (rows.Count == 0)
    //    {
    //        return NotFound(
    //            ApiResponse<object>.Fail(
    //                404,
    //                "City not found.",
    //                HttpContext.TraceIdentifier));
    //    }
    //
    //    return Ok(
    //        ApiResponse<object>.Ok(
    //            200,
    //            "City retrieved successfully.",
    //            rows[0],
    //            HttpContext.TraceIdentifier));
    //}

    //[Authorize(Roles = "SUPER_ADMIN")]
    //[HttpPost("create-city")]
    //public async Task<ActionResult<ApiResponse<object>>> CreateCity(
    //    CreateCityRequest request,
    //    CancellationToken ct)
    //{
    //    var id = await _db.ScalarIntAsync(
    //        "dbo.create_city",
    //        new[]
    //        {
    //            _db.P("@StateId", request.StateId),
    //            _db.P("@CityCode", request.CityCode.Trim()),
    //            _db.P("@CityName", request.CityName.Trim()),
    //            _db.P("@CreatedBy", User.UserId())
    //        },
    //        ct);
    //
    //    return Ok(
    //        ApiResponse<object>.Ok(
    //            200,
    //            "City created successfully.",
    //            new { cityId = id },
    //            HttpContext.TraceIdentifier));
    //}

    //[Authorize(Roles = "SUPER_ADMIN")]
    //[HttpPut("update-city")]
    //public async Task<ActionResult<ApiResponse<object>>> UpdateCity(
    //    UpdateCityRequest request,
    //    CancellationToken ct)
    //{
    //    await _db.ExecuteAsync(
    //        "dbo.update_city",
    //        new[]
    //        {
    //            _db.P("@CityId", request.CityId),
    //            _db.P("@StateId", request.StateId),
    //            _db.P("@CityCode", request.CityCode.Trim()),
    //            _db.P("@CityName", request.CityName.Trim()),
    //            _db.P("@IsActive", request.IsActive),
    //            _db.P("@ModifiedBy", User.UserId())
    //        },
    //        ct);
    //
    //    return Ok(
    //        ApiResponse<object>.Ok(
    //            200,
    //            "City updated successfully.",
    //            null,
    //            HttpContext.TraceIdentifier));
    //}
}