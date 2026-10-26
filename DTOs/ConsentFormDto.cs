namespace DPDP_Api.DTOs;

public sealed class CreateConsentFormRequest
{
    public int OrganisationId { get; set; }
    public int LegalEntityId { get; set; }
    public string FormName { get; set; } = string.Empty;
    public string FormUrl { get; set; } = string.Empty;
    public string? Tier { get; set; }
    public string? Remark { get; set; }
}

public sealed class UpdateConsentFormRequest
{
    public int FormId { get; set; }
    public int OrganisationId { get; set; }
    public int LegalEntityId { get; set; }
    public string FormName { get; set; } = string.Empty;
    public string FormUrl { get; set; } = string.Empty;
    public string? Tier { get; set; }
    public string Status { get; set; } = "DRAFT";
    public string? Remark { get; set; }
}