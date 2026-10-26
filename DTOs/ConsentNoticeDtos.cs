namespace DPDP_Api.DTOs;

public sealed class CreateConsentNoticeRequest
{
    public int OrganisationId { get; set; }
    public int LegalEntityId { get; set; }
    public int ConsentPurposeId { get; set; }
    public string NoticeCode { get; set; } = string.Empty;
    public string NoticeName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class UpdateConsentNoticeRequest
{
    public int ConsentNoticeId { get; set; }
    public int OrganisationId { get; set; }
    public int LegalEntityId { get; set; }
    public int ConsentPurposeId { get; set; }
    public string NoticeCode { get; set; } = string.Empty;
    public string NoticeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}