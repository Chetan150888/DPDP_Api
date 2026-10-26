
namespace DPDP_Api.DTOs
{
    public class UserAccessDto
    {
        public int UserId { get; set; }
        public int? OrganisationId { get; set; }
        public bool IsSuperAdmin { get; set; }
        public bool IsOrgAdmin { get; set; }

        public List<UserAccessRoleDto> Roles { get; set; } = new();
        public List<UserAccessPermissionDto> Permissions { get; set; } = new();
    }

    public class UserAccessRoleDto
    {
        public int RoleId { get; set; }
        public string RoleCode { get; set; } = "";
        public string RoleName { get; set; } = "";
    }

    public class UserAccessPermissionDto
    {
        public int PermissionId { get; set; }
        public string PermissionCode { get; set; } = "";
        public string ModuleCode { get; set; } = "";
        public string PermissionName { get; set; } = "";
    }
}
