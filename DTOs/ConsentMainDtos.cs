
using System;
using System.ComponentModel.DataAnnotations;

namespace DPDP_Api.DTOs
{
    public class ConsentMainSaveRequest
    {
     
        public string Name { get; set; } = string.Empty;

        public string? Mobile { get; set; }

        public string? Email { get; set; }

        
        public int ConsentFormId { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string Status { get; set; } = "PENDING";

        public int? LegalEntityId { get; set; }

        public string? IPAddress { get; set; }

        public string? MacId { get; set; }
    }

    public class ConsentMainCreateRequest : ConsentMainSaveRequest
    {
    }

    public class ConsentMainUpdateRequest : ConsentMainSaveRequest
    {
        [Range(1, long.MaxValue)]
        public long ConsentId { get; set; }

        [Range(1, int.MaxValue)]
        public int ExpectedVersion { get; set; }
    }
}
