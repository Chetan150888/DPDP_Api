using System.Security.Claims;

namespace DPDP_Api.Extensions;

public static class ClaimsExtensions
{
    public static int UserId(this ClaimsPrincipal user) => int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public static int? OrganisationId(this ClaimsPrincipal user) => int.TryParse(user.FindFirstValue("organisation_id"), out var id) ? id : null;
    public static string RoleCode(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    public static string SessionId(this ClaimsPrincipal user) => user.FindFirstValue("session_id") ?? string.Empty;
}
