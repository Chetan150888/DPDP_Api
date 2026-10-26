
using System.ComponentModel.DataAnnotations;

namespace DPDP_Api.DTOs
{
    public class RolePermissionSaveDto
    {
       
        public int OrganisationId { get; set; }

      
        public int RoleId { get; set; }

       
        public int PermissionId { get; set; }

        public bool IsGranted { get; set; }
    }
}
