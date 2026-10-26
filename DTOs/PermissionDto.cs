
namespace DPDP_CMP.DTOs
{
    public class PermissionSaveDto
    {
        public int OrganisationId { get; set; }
        public string PermissionCode { get; set; } = "";
        public string PermissionName { get; set; } = "";
        public string ModuleCode { get; set; } = "";
    }

    public class PermissionListDto
    {
        public int PermissionId { get; set; }
        public int? OrganisationId { get; set; }
        public string PermissionCode { get; set; } = "";
        public string ModuleCode { get; set; } = "";
        public string PermissionName { get; set; } = "";
        public bool IsActive { get; set; }
    }
}
