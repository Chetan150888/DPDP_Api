
namespace DPDP_Api.DTOs
{
    public class ConsentDashboardDto
    {
        public ConsentSummaryDto ConsentSummary { get; set; } = new();

        public ConsentDashboardSummaryDto TicketSummary { get; set; } = new();

        public long TotalRecords { get; set; }

        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public List<ConsentDashboardTicketDto> Tickets { get; set; } = new();
    }

    public class ConsentSummaryDto
    {
        public long TotalConsents { get; set; }
        public long ActiveConsents { get; set; }
        public long ExpiredConsents { get; set; }
        public long WithdrawnConsents { get; set; }
        public long PendingConsents { get; set; }
        public long RejectedConsents { get; set; }
        public long ClosedConsents { get; set; }
    }

    public class ConsentDashboardSummaryDto
    {
        public long TotalCases { get; set; }
        public long Unassigned { get; set; }
        public long InProgress { get; set; }
        public long PendingApproval { get; set; }
        public long Approved { get; set; }
        public long Rejected { get; set; }
        public long Closed { get; set; }
    }

    public class ConsentDashboardTicketDto
    {
        public long TicketId { get; set; }
        public string? TicketNo { get; set; }
        public int OrganisationId { get; set; }
        public long ConsentId { get; set; }
        public string TicketType { get; set; } = "";
        public string TicketStatus { get; set; } = "";
        public string Priority { get; set; } = "";
        public string Subject { get; set; } = "";
        public int? AssignedTo { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
