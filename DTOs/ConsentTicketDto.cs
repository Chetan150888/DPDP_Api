
using System.ComponentModel.DataAnnotations;

namespace DPDP_Api.DTOs
{
    public class ConsentTicketCreateDto
    {
       
        public int OrganisationId { get; set; }

      
        public long ConsentId { get; set; }

     
        public string TicketType { get; set; } = "";

        public string Priority { get; set; } = "MEDIUM";

       
        public string Subject { get; set; } = "";

        public string? Description { get; set; }
    }

    public class ConsentTicketAssignDto
    {
      
        public int OrganisationId { get; set; }

      
        public long TicketId { get; set; }

     
        public int AssignedTo { get; set; }

      
        public string? Remarks { get; set; }
    }
}
