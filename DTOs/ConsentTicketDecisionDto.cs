
using System.ComponentModel.DataAnnotations;

namespace DPDP_Api.DTOs
{
    public class ConsentTicketDecisionDto
    {
      
        public int OrganisationId { get; set; }

       
        public long TicketId { get; set; }

       
        public string Decision { get; set; } = "";

       
        public string? Remarks { get; set; }
    }
}
