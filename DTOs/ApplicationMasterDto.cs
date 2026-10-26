
using System.ComponentModel.DataAnnotations;

namespace DPDP_Api.DTOs
{
    public class ApplicationMasterSaveDto
    {
        public int? ApplicationId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int? OrganisationId { get; set; }

        [Required]
        [MaxLength(200)]
        public string ApplicationName { get; set; } = "";

        [MaxLength(500)]
        public string? ApplicationDescription { get; set; }

        public bool? IsActive { get; set; } = null;
    }

    public class ApplicationMasterListDto
    {
        public int ApplicationId { get; set; }
        public int OrganisationId { get; set; }
        public string ApplicationName { get; set; } = "";
        public string? ApplicationDescription { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public int CreatedBy { get; set; }
        public string CreatedDate { get; set; } = "";
        public int? ModifiedBy { get; set; }
        public string? ModifiedDate { get; set; }
    }
}
