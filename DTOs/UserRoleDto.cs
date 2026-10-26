
using System.ComponentModel.DataAnnotations;

namespace DPDP_Api.DTOs
{
    public class UserRoleSaveDto
    {
     
        public string Action { get; set; } = "";

       
        public int UserId { get; set; }

      
        public int OrganisationId { get; set; }

        public int? RoleId { get; set; }
    }
}
