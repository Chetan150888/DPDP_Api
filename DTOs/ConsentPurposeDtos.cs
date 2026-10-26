namespace DPDP_Api.DTOs;

public sealed class CreateConsentPurposeRequest
{
    public int OrganisationId { get; set; }

    public int LegalEntityId { get; set; }

    public string PurposeCode { get; set; } = string.Empty;

    public string PurposeName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string ProcessingBasis { get; set; } = "CONSENT";

    public bool IsConsentRequired { get; set; } = true;
}


public sealed class UpdateConsentPurposeRequest
{
    public int ConsentPurposeId { get; set; }

    public int OrganisationId { get; set; }

    public int LegalEntityId { get; set; }

    public string PurposeCode { get; set; } = string.Empty;

    public string PurposeName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string ProcessingBasis { get; set; } = "CONSENT";

    public bool IsConsentRequired { get; set; } = true;

    public bool IsActive { get; set; } = true;
}