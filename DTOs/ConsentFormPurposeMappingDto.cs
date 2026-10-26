namespace DPDP_Api.DTOs;

public sealed class SaveConsentFormPurposeMappingRequest
{
    public int OrganisationId { get; set; }
    public int LegalEntityId { get; set; }
    public int FormId { get; set; }
    public int ConsentPurposeId { get; set; }
    public int DataCategoryId { get; set; }
    public int DataElementId { get; set; }
}