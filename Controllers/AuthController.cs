using DPDP_Api.Data;
using DPDP_Api.DTOs;
using DPDP_Api.Extensions;
using DPDP_Api.Models;
using DPDP_Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DPDP_Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly Db _db;
    private readonly PasswordService _passwords;
    private readonly TokenService _tokens;
    private readonly IConfiguration _configuration;

    public AuthController(
        Db db,
        PasswordService passwords,
        TokenService tokens,
        IConfiguration configuration)
    {
        _db = db;
        _passwords = passwords;
        _tokens = tokens;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<object>>> Login(
        LoginRequest request,
        CancellationToken ct)
    {
        var rows = await _db.RowsAsync(
            "dbo.login_user",
            new[]
            {
                _db.P("@Email", request.Email.Trim().ToLowerInvariant())
            },
            ct);

        if (rows.Count == 0)
        {
            return Unauthorized(
                Fail(401, "Invalid email or password."));
        }

        var user = rows[0];

        var userId = Convert.ToInt32(user["UserId"]);

        DateTime? lockedUntil = user["LockedUntil"] == null
            ? null
            : Convert.ToDateTime(user["LockedUntil"]);

        if (lockedUntil.HasValue &&
            lockedUntil.Value > DateTime.UtcNow)
        {
            return StatusCode(
                423,
                Fail(423, "Account is temporarily locked."));
        }

        var passwordSalt = Convert.ToString(
            user["PasswordSalt"])!;

        var passwordHash = Convert.ToString(
            user["PasswordHash"])!;

        var passwordValid = _passwords.Verify(
            request.Password,
            passwordSalt,
            passwordHash);

        if (!passwordValid)
        {
            await _db.ExecuteAsync(
                "dbo.record_failed_login",
                new[]
                {
                    _db.P("@UserId", userId),
                    _db.P(
                        "@IpAddress",
                        HttpContext.Connection.RemoteIpAddress?.ToString())
                },
                ct);

            return Unauthorized(
                Fail(401, "Invalid email or password."));
        }

        var sessionId = Guid.NewGuid().ToString();

        var refreshToken = _tokens.CreateRefreshToken();
        var refreshHash = _tokens.HashToken(refreshToken);

        int? organisationId = user["OrganisationId"] == null
            ? null
            : Convert.ToInt32(user["OrganisationId"]);

        var roleCode = Convert.ToString(
            user["RoleCode"])!;

        var refreshDays = _configuration.GetValue<int>(
            "Jwt:RefreshTokenDays",
            7);

        await _db.ExecuteAsync(
            "dbo.create_session",
            new[]
            {
                _db.P("@SessionId", sessionId),
                _db.P("@UserId", userId),
                _db.P("@RefreshTokenHash", refreshHash),
                _db.P(
                    "@RefreshExpiresAt",
                    DateTime.UtcNow.AddDays(refreshDays)),
                _db.P(
                    "@IpAddress",
                    HttpContext.Connection.RemoteIpAddress?.ToString())
            },
            ct);

        await _db.ExecuteAsync(
            "dbo.record_successful_login",
            new[]
            {
                _db.P("@UserId", userId)
            },
            ct);

        var accessToken = _tokens.CreateAccessToken(
            userId,
            organisationId,
            roleCode,
            sessionId);

        var accessTokenMinutes =
            _configuration.GetValue<int>(
                "Jwt:AccessTokenMinutes",
                15);

        var response = new
        {
            accessToken,
            refreshToken,
            tokenType = "Bearer",
            expiresInMinutes = accessTokenMinutes,
            userId,
            organisationId,
            roleCode,
            displayName = user["DisplayName"],
            mustChangePassword = user["MustChangePassword"]
        };

        return Ok(
            Success(
                "Login successful.",
                response));
    }




    [AllowAnonymous]
    [HttpPost("login-otp")]
    public async Task<ActionResult<ApiResponse<object>>> LoginWithOtp(
    OtpLoginRequest request,
    CancellationToken ct)
    {
        var fixedOtp = _configuration["LoginOtp:FixedOtp"];

        if (string.IsNullOrWhiteSpace(fixedOtp))
        {
            return StatusCode(
                500,
                Fail(500, "OTP configuration is not available."));
        }

        if (request.Otp != fixedOtp)
        {
            return Unauthorized(
                Fail(401, "Invalid OTP."));
        }

        var email = request.Email
            .Trim()
            .ToLowerInvariant();

        var rows = await _db.RowsAsync(
            "dbo.login_user",
            new[]
            {
            _db.P("@Email", email)
            },
            ct);

        if (rows.Count == 0)
        {
            return Unauthorized(
                Fail(401, "Invalid email or OTP."));
        }

        var user = rows[0];

        var userId = Convert.ToInt32(
            user["UserId"]);

        DateTime? lockedUntil = user["LockedUntil"] == null
            ? null
            : Convert.ToDateTime(user["LockedUntil"]);

        if (lockedUntil.HasValue &&
            lockedUntil.Value > DateTime.UtcNow)
        {
            return StatusCode(
                423,
                Fail(423, "Account is temporarily locked."));
        }

        int? organisationId = user["OrganisationId"] == null
            ? null
            : Convert.ToInt32(user["OrganisationId"]);

        var roleCode = Convert.ToString(
            user["RoleCode"])!;

        var sessionId = Guid.NewGuid().ToString();

        var refreshToken =
            _tokens.CreateRefreshToken();

        var refreshHash =
            _tokens.HashToken(refreshToken);

        var refreshDays =
            _configuration.GetValue<int>(
                "Jwt:RefreshTokenDays",
                7);

        await _db.ExecuteAsync(
            "dbo.create_session",
            new[]
            {
            _db.P("@SessionId", sessionId),
            _db.P("@UserId", userId),
            _db.P("@RefreshTokenHash", refreshHash),
            _db.P(
                "@RefreshExpiresAt",
                DateTime.UtcNow.AddDays(refreshDays)),
            _db.P(
                "@IpAddress",
                HttpContext.Connection
                    .RemoteIpAddress?
                    .ToString())
            },
            ct);

        await _db.ExecuteAsync(
            "dbo.record_successful_login",
            new[]
            {
            _db.P("@UserId", userId)
            },
            ct);

        var accessToken =
            _tokens.CreateAccessToken(
                userId,
                organisationId,
                roleCode,
                sessionId);

        var accessTokenMinutes =
            _configuration.GetValue<int>(
                "Jwt:AccessTokenMinutes",
                15);

        var response = new
        {
            accessToken,
            refreshToken,
            tokenType = "Bearer",
            expiresInMinutes = accessTokenMinutes,
            userId,
            organisationId,
            roleCode,
            displayName = user["DisplayName"],
            mustChangePassword = user["MustChangePassword"],
            loginMethod = "OTP"
        };

        return Ok(
            Success(
                "OTP login successful.",
                response));
    }






    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public async Task<ActionResult<ApiResponse<object>>> RefreshToken(
        RefreshTokenRequest request,
        CancellationToken ct)
    {
        var hash = _tokens.HashToken(
            request.RefreshToken);

        var rows = await _db.RowsAsync(
            "dbo.get_session_by_refresh_token",
            new[]
            {
                _db.P("@RefreshTokenHash", hash)
            },
            ct);

        if (rows.Count == 0)
        {
            return Unauthorized(
                Fail(
                    401,
                    "Invalid or expired refresh token."));
        }

        var row = rows[0];

        var newRefresh = _tokens.CreateRefreshToken();
        var newHash = _tokens.HashToken(newRefresh);

        var refreshDays =
            _configuration.GetValue<int>(
                "Jwt:RefreshTokenDays",
                7);

        await _db.ExecuteAsync(
            "dbo.rotate_refresh_token",
            new[]
            {
                _db.P("@SessionId", row["SessionId"]),
                _db.P(
                    "@NewRefreshTokenHash",
                    newHash),
                _db.P(
                    "@RefreshExpiresAt",
                    DateTime.UtcNow.AddDays(refreshDays))
            },
            ct);

        var userId =
            Convert.ToInt32(row["UserId"]);

        int? organisationId =
            row["OrganisationId"] == null
                ? null
                : Convert.ToInt32(
                    row["OrganisationId"]);

        var roleCode =
            Convert.ToString(row["RoleCode"])!;

        var sessionId =
            Convert.ToString(row["SessionId"])!;

        var accessToken =
            _tokens.CreateAccessToken(
                userId,
                organisationId,
                roleCode,
                sessionId);

        var response = new
        {
            accessToken,
            refreshToken = newRefresh,
            tokenType = "Bearer"
        };

        return Ok(
            Success(
                "Token refreshed successfully.",
                response));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(
        ChangePasswordRequest request,
        CancellationToken ct)
    {
        if (!PasswordService.MeetsPolicy(
                request.NewPassword))
        {
            return BadRequest(
                Fail(
                    400,
                    "Password must be at least 12 characters and include uppercase, lowercase, number and special character."));
        }

        var userId = User.UserId();

        var rows = await _db.RowsAsync(
            "dbo.get_user_password",
            new[]
            {
                _db.P("@UserId", userId)
            },
            ct);

        if (rows.Count == 0)
        {
            return BadRequest(
                Fail(
                    400,
                    "Current password is incorrect."));
        }

        var passwordSalt =
            Convert.ToString(
                rows[0]["PasswordSalt"])!;

        var passwordHash =
            Convert.ToString(
                rows[0]["PasswordHash"])!;

        if (!_passwords.Verify(
                request.CurrentPassword,
                passwordSalt,
                passwordHash))
        {
            return BadRequest(
                Fail(
                    400,
                    "Current password is incorrect."));
        }

        var password =
            _passwords.Hash(
                request.NewPassword);

        await _db.ExecuteAsync(
            "dbo.change_login_password",
            new[]
            {
                _db.P("@UserId", userId),
                _db.P(
                    "@PasswordSalt",
                    password.Salt),
                _db.P(
                    "@PasswordHash",
                    password.Hash)
            },
            ct);

        return Ok(
            Success(
                "Password changed successfully.",
                null));
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword(
        ForgotPasswordRequest request,
        CancellationToken ct)
    {
        var resetToken =
            _tokens.CreateResetToken();

        var tokenHash =
            _tokens.HashToken(resetToken);

        await _db.ExecuteAsync(
            "dbo.create_password_reset",
            new[]
            {
                _db.P(
                    "@UserNameOrEmail",
                    request.Email
                        .Trim()
                        .ToLowerInvariant()),
                _db.P(
                    "@TokenHash",
                    tokenHash),
                _db.P(
                    "@ExpiresAt",
                    DateTime.UtcNow.AddMinutes(30))
            },
            ct);

        return Ok(
            Success(
                "If the account exists, password reset instructions will be sent through the configured notification service.",
                null));
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(
        ResetPasswordRequest request,
        CancellationToken ct)
    {
        if (!PasswordService.MeetsPolicy(
                request.NewPassword))
        {
            return BadRequest(
                Fail(
                    400,
                    "Password does not meet the required policy."));
        }

        var password =
            _passwords.Hash(
                request.NewPassword);

        var tokenHash =
            _tokens.HashToken(
                request.ResetToken);

        var changed =
            await _db.ScalarIntAsync(
                "dbo.reset_login_password",
                new[]
                {
                    _db.P(
                        "@TokenHash",
                        tokenHash),
                    _db.P(
                        "@PasswordSalt",
                        password.Salt),
                    _db.P(
                        "@PasswordHash",
                        password.Hash)
                },
                ct);

        if (changed != 1)
        {
            return BadRequest(
                Fail(
                    400,
                    "Reset token is invalid or expired."));
        }

        return Ok(
            Success(
                "Password reset successfully.",
                null));
    }



    [Authorize]
    [HttpGet("check-token-valid")]
    public async Task<ActionResult<ApiResponse<object>>> CheckTokenValid(
        CancellationToken ct)
    {
        var userId = User.UserId();

        var rows = await _db.RowsAsync(
            "dbo.get_current_user",
            new[]
            {
            _db.P("@UserId", userId)
            },
            ct);

        if (rows.Count == 0)
        {
            return Unauthorized(
                Fail(
                    401,
                    "User account is inactive or not available."));
        }

        var user = rows[0];

        int? organisationId =
            user["OrganisationId"] == null ||
            user["OrganisationId"] == DBNull.Value
                ? null
                : Convert.ToInt32(user["OrganisationId"]);

        int? employeeId =
            user["EmployeeId"] == null ||
            user["EmployeeId"] == DBNull.Value
                ? null
                : Convert.ToInt32(user["EmployeeId"]);

        int? departmentId =
            user["DepartmentId"] == null ||
            user["DepartmentId"] == DBNull.Value
                ? null
                : Convert.ToInt32(user["DepartmentId"]);

        int? designationId =
            user["DesignationId"] == null ||
            user["DesignationId"] == DBNull.Value
                ? null
                : Convert.ToInt32(user["DesignationId"]);

        int? locationId =
            user["LocationId"] == null ||
            user["LocationId"] == DBNull.Value
                ? null
                : Convert.ToInt32(user["LocationId"]);

        var response = new
        {
            userId = Convert.ToInt32(user["UserId"]),

            organisationId,
            employeeId,

            displayName = Convert.ToString(user["DisplayName"]),
            email = Convert.ToString(user["Email"]),

            roleId = Convert.ToInt32(user["RoleId"]),
            roleCode = Convert.ToString(user["RoleCode"]),
            roleName = Convert.ToString(user["RoleName"]),
            scopeType = Convert.ToString(user["ScopeType"]),

            employeeCode =
                employeeId.HasValue
                    ? Convert.ToString(user["EmployeeCode"])
                    : null,

            employeeName =
                employeeId.HasValue
                    ? Convert.ToString(user["EmployeeName"])
                    : null,

            mobileNumber =
                employeeId.HasValue
                    ? Convert.ToString(user["MobileNumber"])
                    : null,

            departmentId,
            designationId,
            locationId,

            mustChangePassword =
                Convert.ToBoolean(user["MustChangePassword"])
        };

        return Ok(
            Success(
                "Token is valid.",
                response));
    }



    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<object>>> Logout(
        CancellationToken ct)
    {
        await _db.ExecuteAsync(
            "dbo.revoke_session",
            new[]
            {
                _db.P(
                    "@SessionId",
                    User.SessionId()),
                _db.P(
                    "@UserId",
                    User.UserId())
            },
            ct);

        return Ok(
            Success(
                "Logged out successfully.",
                null));
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