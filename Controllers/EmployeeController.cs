using DPDP_Api.Data;
using DPDP_Api.DTOs;
using DPDP_Api.Extensions;
using DPDP_Api.Models;
using DPDP_Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DPDP_Api.Controllers;

[ApiController]
[Authorize(Roles = "ORG_ADMIN")]
[Route("api/employee")]
public sealed class EmployeeController : ControllerBase
{
    private readonly Db _db;
    private readonly PasswordService _passwords;

    public EmployeeController(
        Db db,
        PasswordService passwords)
    {
        _db = db;
        _passwords = passwords;
    }
    [HttpGet("access-roles")]
    public async Task<ActionResult<ApiResponse<object>>> AccessRoles(
    CancellationToken ct)
    {
        var rows = await _db.RowsAsync(
            "dbo.list_access_roles",
            new[]
            {
            _db.P(
                "@RequesterRole",
                User.RoleCode())
            },
            ct);

        return Ok(
            Success(
                "Access roles retrieved successfully.",
                rows));
    }
    private int GetOrganisationId()
    {
        var organisationId = User.OrganisationId();

        if (!organisationId.HasValue ||
            organisationId.Value <= 0)
        {
            throw new UnauthorizedAccessException(
                "Organisation access is unavailable.");
        }

        return organisationId.Value;
    }

    [HttpPost("create")]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        EmployeeCreateRequest request,
        CancellationToken ct)
    {
        var organisationId = GetOrganisationId();

        var employeeId = await _db.ScalarIntAsync(
            "dbo.create_employee",
            new[]
            {
                _db.P("@OrganisationId", organisationId),
                _db.P("@EmployeeCode", request.EmployeeCode.Trim()),
                _db.P("@EmployeeName", request.EmployeeName.Trim()),
                _db.P("@Email", request.Email.Trim().ToLowerInvariant()),
                _db.P("@MobileNumber", Clean(request.MobileNumber)),
                _db.P("@DepartmentId", request.DepartmentId),
                _db.P("@DesignationId", request.DesignationId),
                _db.P("@LocationId", request.LocationId),
                _db.P("@ReportingManagerId", request.ReportingManagerId),
                _db.P("@DateOfJoining", request.DateOfJoining),
                _db.P("@CreatedBy", User.UserId())
            },
            ct);

        return Ok(
            Success(
                "Employee created successfully.",
                new
                {
                    employeeId
                }));
    }

    [HttpGet("list")]
    public async Task<ActionResult<ApiResponse<object>>> List(
       
        string? search = null,
        CancellationToken ct = default)
    {
        var organisationId = GetOrganisationId();

        var rows = await _db.RowsAsync(
            "dbo.list_employee",
            new[]
            {
               _db.P("@OrganisationId", organisationId),
                _db.P("@Search", Clean(search))
            },
            ct);

        return Ok(
            Success(
                "Employees retrieved successfully.",
                rows));
    }

    [HttpGet("{employeeId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> GetById(
        int employeeId,
       
        CancellationToken ct = default)
    {
        var organisationId = GetOrganisationId();

        var rows = await _db.RowsAsync(
            "dbo.get_employee",
            new[]
            {
                _db.P("@EmployeeId", employeeId),
                _db.P("@OrganisationId", organisationId)
            },
            ct);

        if (rows.Count == 0)
        {
            return NotFound(
                Fail(
                    404,
                    "Employee not found."));
        }

        return Ok(
            Success(
                "Employee retrieved successfully.",
                rows[0]));
    }

    [HttpPut("update")]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        EmployeeUpdateRequest request,
        CancellationToken ct)
    {
        var organisationId = GetOrganisationId();

        await _db.ExecuteAsync(
            "dbo.update_employee",
            new[]
            {
                _db.P("@EmployeeId", request.EmployeeId),
                _db.P("@OrganisationId", organisationId),
                _db.P("@EmployeeCode", request.EmployeeCode.Trim()),
                _db.P("@EmployeeName", request.EmployeeName.Trim()),
                _db.P("@Email", request.Email.Trim().ToLowerInvariant()),
                _db.P("@MobileNumber", Clean(request.MobileNumber)),
                _db.P("@DepartmentId", request.DepartmentId),
                _db.P("@DesignationId", request.DesignationId),
                _db.P("@LocationId", request.LocationId),
                _db.P("@ReportingManagerId", request.ReportingManagerId),
                _db.P("@DateOfJoining", request.DateOfJoining),
                _db.P("@IsActive", request.IsActive),
                _db.P("@ModifiedBy", User.UserId())
            },
            ct);

        return Ok(
            Success(
                "Employee updated successfully.",
                null));
    }

    [HttpDelete("{employeeId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        int employeeId,
      
        CancellationToken ct = default)
    {
        var organisationId = GetOrganisationId();

        await _db.ExecuteAsync(
            "dbo.delete_employee",
            new[]
            {
                _db.P("@EmployeeId", employeeId),
                _db.P("@OrganisationId", organisationId),
                _db.P("@ModifiedBy", User.UserId())
            },
            ct);

        return Ok(
            Success(
                "Employee deleted successfully.",
                null));
    }

    [HttpPost("{employeeId:int}/grant-access")]
    public async Task<ActionResult<ApiResponse<object>>> GrantAccess(
        int employeeId,
        EmployeeGrantAccessRequest request,
        CancellationToken ct)
    {
        var organisationId = GetOrganisationId();

        if (!PasswordService.MeetsPolicy(
                request.TemporaryPassword))
        {
            return BadRequest(
                Fail(
                    400,
                    "Temporary password does not meet the required policy."));
        }

        var password =
            _passwords.Hash(request.TemporaryPassword);

        var userId = await _db.ScalarIntAsync(
            "dbo.grant_employee_access",
            new[]
            {
                _db.P("@EmployeeId", employeeId),
                _db.P("@OrganisationId", organisationId),
                _db.P("@RoleId", request.RoleId),
                _db.P("@PasswordSalt", password.Salt),
                _db.P("@PasswordHash", password.Hash),
                _db.P("@MustChangePassword", true),
                _db.P("@CreatedBy", User.UserId()),
                _db.P("@RequesterRole", User.RoleCode())
            },
            ct);

        return Ok(
            Success(
                "Employee portal access granted successfully.",
                new
                {
                    employeeId,
                    userId,
                    mustChangePassword = true
                }));
    }

    [HttpPut("{employeeId:int}/change-role")]
    public async Task<ActionResult<ApiResponse<object>>> ChangeRole(
        int employeeId,
        EmployeeChangeRoleRequest request,
        CancellationToken ct)
    {
        var organisationId = GetOrganisationId();

        await _db.ExecuteAsync(
            "dbo.update_employee_access_role",
            new[]
            {
                _db.P("@EmployeeId", employeeId),
                _db.P("@OrganisationId", organisationId),
                _db.P("@RoleId", request.RoleId),
                _db.P("@ModifiedBy", User.UserId()),
                _db.P("@RequesterRole", User.RoleCode())
            },
            ct);

        return Ok(
            Success(
                "Employee access role updated successfully.",
                null));
    }

    [HttpPost("{employeeId:int}/revoke-access")]
    public async Task<ActionResult<ApiResponse<object>>> RevokeAccess(
        int employeeId,
        EmployeeRevokeAccessRequest request,
        CancellationToken ct)
    {
        var organisationId = GetOrganisationId();

        await _db.ExecuteAsync(
            "dbo.revoke_employee_access",
            new[]
            {
                _db.P("@EmployeeId", employeeId),
                _db.P("@OrganisationId", organisationId),
                _db.P("@ModifiedBy", User.UserId())
            },
            ct);

        return Ok(
            Success(
                "Employee portal access revoked successfully.",
                null));
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private ApiResponse<object> Success(
        string message,
        object? data)
    {
        return ApiResponse<object>.Ok(
            200,
            message,
            data,
            HttpContext.TraceIdentifier);
    }

    private ApiResponse<object> Fail(
        int code,
        string message)
    {
        return ApiResponse<object>.Fail(
            code,
            message,
            HttpContext.TraceIdentifier);
    }
}
