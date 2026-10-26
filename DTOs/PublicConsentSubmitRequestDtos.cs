
using System.ComponentModel.DataAnnotations;

namespace DPDP_Api.DTOs
{
    public class PublicConsentSubmitRequestDtos
    {
      
        public string Name { get; set; } = string.Empty;

      
        public string? Mobile { get; set; }

      
        public string? Email { get; set; }

       
        public List<PublicConsentDecisionRequest> Decisions
        { get; set; } = new();
    }

    public class PublicConsentDecisionRequest
    {
        
        public int ConsentPurposeId { get; set; }

      
        public int NoticeVersionId { get; set; }

        
        //[RegularExpression("^(ACCEPT|REJECT)$")]
        public string Decision { get; set; } = string.Empty;
    }
}
