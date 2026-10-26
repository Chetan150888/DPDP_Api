
using System.ComponentModel.DataAnnotations;

namespace DPDP_Api.DTOs
{
    public class OrgConsentQuestionDto
    {
        [Range(1, int.MaxValue)]
        public int OrganisationId { get; set; }

        [Range(1, long.MaxValue)]
        public long TicketId { get; set; }

        [Required]
        [StringLength(2000, MinimumLength = 1)]
        public string MessageText { get; set; } = "";
    }

    public class CustomerConsentReplyDto
    {
        [Range(1, long.MaxValue)]
        public long TicketId { get; set; }

        [Required]
        [StringLength(2000, MinimumLength = 1)]
        public string MessageText { get; set; } = "";
    }
}
