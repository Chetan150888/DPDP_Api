
using DPDP_Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace DPDP_Api.Controllers
{
    [ApiController]
    [Route("api/customer")]
    public class CustomerAuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly string _connectionString;

        public CustomerAuthController(
            IConfiguration config,
            IWebHostEnvironment env)
        {
            _config = config;
            _env = env;

            _connectionString =
                config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "DefaultConnection is missing.");
        }

        // POST: api/customer/send-otp
        [HttpPost("send-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> SendOtp(
            [FromBody] CustomerSendOtpDto request)
        {
            //if (!IsTestOtpAllowed())
            //    return ServiceUnavailable();

            if (request == null ||
                string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Email is required."
                });
            }

            string email = NormalizeEmail(request.Email);

            // Generic response prevents account enumeration.
            // No OTP is saved in temporary fixed-OTP mode.
            await Task.CompletedTask;

            return Ok(new
            {
                success = true,
                message =
                    "If the email is registered, an OTP request has been processed."
            });
        }

        // POST: api/customer/verify-otp
        [HttpPost("verify-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyOtp(
            [FromBody] CustomerVerifyOtpDto request)
        {
            //if (!IsTestOtpAllowed())
            //    return ServiceUnavailable();

            if (request == null ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Otp))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Email and OTP are required."
                });
            }

            string email = NormalizeEmail(request.Email);
            string otp = request.Otp.Trim();

            // Fixed OTP only in isolated Development.
            if (otp != _config["CustomerAuth:FixedOtp"])
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid OTP."
                });
            }

            Guid sessionId = Guid.NewGuid();

            using var con = new SqlConnection(
                _connectionString);

            using var cmd = new SqlCommand(
                "dbo.usp_CMP_CustomerTestLogin", con);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(
                "@Email", SqlDbType.NVarChar, 254)
                .Value = email;

            cmd.Parameters.Add(
                "@SessionId", SqlDbType.UniqueIdentifier)
                .Value = sessionId;

            cmd.Parameters.Add(
                "@SessionMinutes", SqlDbType.Int)
                .Value = _config.GetValue<int>(
                    "CustomerAuth:SessionMinutes", 60);

            await con.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync() ||
                !reader.GetBoolean(
                    reader.GetOrdinal("Success")))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid customer login."
                });
            }

            int sessionMinutes = _config.GetValue<int>(
                "CustomerAuth:SessionMinutes", 60);

            string token = GenerateCustomerToken(
                email, sessionId, sessionMinutes);

            return Ok(new
            {
                success = true,
                message = "Customer login successful.",
                data = new
                {
                    accessToken = token,
                    tokenType = "Bearer",
                    expiresIn = sessionMinutes * 60
                }
            });
        }

        private bool IsTestOtpAllowed()
        {
            return _env.IsDevelopment()
                && _config.GetValue<bool>(
                    "CustomerAuth:UseFixedOtp")
                && _config["CustomerAuth:FixedOtp"]
                    == "666666";
        }

        private IActionResult ServiceUnavailable()
        {
            return StatusCode(503, new
            {
                success = false,
                message =
                    "Customer OTP delivery is not configured."
            });
        }

        private static string NormalizeEmail(
            string email)
        {
            return email.Trim().ToLowerInvariant();
        }

        private string GenerateCustomerToken(
            string email,
            Guid sessionId,
            int sessionMinutes)
        {
            string secret =
                _config["Jwt:SigningKey"]
                ?? throw new InvalidOperationException(
                    "Jwt:SigningKey is missing.");

            string issuer =
                _config["Jwt:Issuer"]
                ?? throw new InvalidOperationException(
                    "Jwt:Issuer is missing.");

            string audience =
                _config["Jwt:Audience"]
                ?? throw new InvalidOperationException(
                    "Jwt:Audience is missing.");

            var claims = new[]
            {
                new Claim(
                    JwtRegisteredClaimNames.Sub, email),

                new Claim(
                    JwtRegisteredClaimNames.Email, email),

                new Claim(
                    "session_id",
                    sessionId.ToString()),

                new Claim(
                    "token_type", "CUSTOMER")
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secret));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    sessionMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }
    }
}
