using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace DPDP_Api.Security;

public sealed class TokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreateAccessToken(
        int userId,
        int? organisationId,
        string roleCode,
        string sessionId)
    {
        var key = _configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException(
                "JWT signing key is missing.");

        var claims = new List<Claim>
        {
            new Claim(
                JwtRegisteredClaimNames.Sub,
                userId.ToString()),

            new Claim(
                ClaimTypes.NameIdentifier,
                userId.ToString()),

            new Claim(
                ClaimTypes.Role,
                roleCode),

            new Claim(
                "session_id",
                sessionId),

            new Claim(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString("N"))
        };

        if (organisationId.HasValue)
        {
            claims.Add(
                new Claim(
                    "organisation_id",
                    organisationId.Value.ToString()));
        }

        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(key));

        var credentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256);

        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];

        var accessTokenMinutes =
            _configuration.GetValue<int>(
                "Jwt:AccessTokenMinutes",
                15);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                accessTokenMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    public string CreateRefreshToken()
    {
        return Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(64));
    }

    public string CreateResetToken()
    {
        return Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(48));
    }

    public string HashToken(string token)
    {
        var tokenBytes =
            Encoding.UTF8.GetBytes(token);

        var hash =
            SHA256.HashData(tokenBytes);

        return Convert.ToHexString(hash);
    }
}