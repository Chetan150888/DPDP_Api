using DPDP_Api.Data;
using DPDP_Api.DTOs;
using DPDP_Api.Extensions;
using DPDP_Api.Models;
using DPDP_Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DPDP_Api.Controllers;

[ApiController]
[Authorize(Roles = "SUPER_ADMIN")]
[Route("api/user")]
public sealed class UserController : ControllerBase
{
    private readonly Db _db;
    private readonly PasswordService _passwords;

    public UserController(
        Db db,
        PasswordService passwords)
    {
        _db = db;
        _passwords = passwords;
    }

    [HttpPost("create-org-admin")]
    public async Task<ActionResult<ApiResponse<object>>> CreateOrgAdmin(
        CreateOrgAdminRequest request,
        CancellationToken ct)
    {
        if (!PasswordService.MeetsPolicy(request.Password))
        {
            return BadRequest(
                ApiResponse<object>.Fail(
                    400,
                    "Password must be at least 12 characters and include uppercase, lowercase, number and special character.",
                    HttpContext.TraceIdentifier));
        }

        var (salt, hash) = _passwords.Hash(request.Password);

        var userId = await _db.ScalarIntAsync(
            "dbo.create_org_admin",
            new[]
            {
                _db.P("@OrganisationId", request.OrganisationId),
                _db.P("@UserName", request.UserName),
                _db.P("@DisplayName", request.DisplayName),
                _db.P("@Email", request.Email),
                _db.P("@MobileNumber", request.MobileNumber),
                _db.P("@EmployeeCode", request.EmployeeCode),
                _db.P("@DepartmentId", request.DepartmentId),
                _db.P("@DesignationId", request.DesignationId),
                _db.P("@LocationId", request.LocationId),
                _db.P("@PasswordSalt", salt),
                _db.P("@PasswordHash", hash),
                _db.P("@CreatedBy", User.UserId())
            },
            ct);

        return Ok(
            ApiResponse<object>.Ok(
                200,
                "Organisation administrator created successfully.",
                new
                {
                    userId
                },
                HttpContext.TraceIdentifier));
    }

    [HttpGet("list-org-admin")]
    public async Task<ActionResult<ApiResponse<object>>> ListOrgAdmin(
        int? organisationId = null,
        string? search = null,
        CancellationToken ct = default)
    {
        var rows = await _db.RowsAsync(
            "dbo.list_org_admin",
            new[]
            {
                _db.P("@OrganisationId", organisationId),
                _db.P("@Search", search)
            },
            ct);

        return Ok(
            ApiResponse<object>.Ok(
                200,
                "Organisation administrators retrieved successfully.",
                rows,
                HttpContext.TraceIdentifier));
    }

    [HttpPut("update-org-admin")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateOrgAdmin(
        UpdateOrgAdminRequest request,
        CancellationToken ct)
    {
        await _db.ExecuteAsync(
            "dbo.update_org_admin",
            new[]
            {
                _db.P("@UserId", request.UserId),
                _db.P("@DisplayName", request.DisplayName),
                _db.P("@Email", request.Email),
                _db.P("@MobileNumber", request.MobileNumber),
                _db.P("@DepartmentId", request.DepartmentId),
                _db.P("@DesignationId", request.DesignationId),
                _db.P("@LocationId", request.LocationId),
                _db.P("@IsActive", request.IsActive),
                _db.P("@ModifiedBy", User.UserId())
            },
            ct);

        return Ok(
            ApiResponse<object>.Ok(
                200,
                "Organisation administrator updated successfully.",
                null,
                HttpContext.TraceIdentifier));
    }
}