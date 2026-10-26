using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DPDP_Api.DTOs
{
    public class ConsentFormDetailsSubmitRequest
    {
        public int OrganisationId { get; set; }
        [Range(1, int.MaxValue)]
        public int ConsentFormId { get; set; }

      

        public string PublicToken { get; set; } = string.Empty;

      

        public string Name { get; set; } = string.Empty;

   

        public string? Mobile { get; set; }

       

        public string? Email { get; set; }

    

        public Guid SubmissionKey { get; set; }

       

        public List<ConsentElementChoiceDto> Decisions { get; set; } = new();
    }

    public class ConsentElementChoiceDto
    {
        [Range(1, int.MaxValue)]
        public int ConsentPurposeId { get; set; }

        [Range(1, int.MaxValue)]
        public int DataCategoryId { get; set; }

        [Range(1, int.MaxValue)]
        public int DataElementId { get; set; }

        [Range(1, int.MaxValue)]
        public int NoticeVersionId { get; set; }

        [Required, RegularExpression("^(ACCEPT|REJECT)$")]
        public string Decision { get; set; } = string.Empty;
    }
}
