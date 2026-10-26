
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace DPDP_Api.DTOs
{
    // POST: Customer Withdraw / Modify Request
    public class CustomerConsentRequestDto
    {
        [Range(1, long.MaxValue)]
        public long ConsentId { get; set; }

        [Required]
        public string RequestType { get; set; } = "";

        [Required]
        [MaxLength(2000)]
        public string Reason { get; set; } = "";

        public JsonElement? ProposedChanges { get; set; }
    }

    // GET: Customer Request List
    public class CustomerConsentRequestListDto
    {
        public long ChangeRequestId { get; set; }
        public long TicketId { get; set; }
        public string? TicketNo { get; set; }
        public long ConsentId { get; set; }
        public int OrganisationId { get; set; }

        public string RequestType { get; set; } = "";
        public string RequestStatus { get; set; } = "";
        public string TicketStatus { get; set; } = "";

        public string? Reason { get; set; }
        public string? ProposedChangesJson { get; set; }

        public string RequestedDate { get; set; } = "";
        public string? DecisionDate { get; set; }
        public string? DecisionRemarks { get; set; }
        public string? AppliedDate { get; set; }
    }

    // GET: Pagination Response
    public class CustomerConsentRequestPagedDto
    {
        public long TotalRecords { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }

        public List<CustomerConsentRequestListDto> Requests { get; set; }
            = new();
    }
}
