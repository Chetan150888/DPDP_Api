namespace DPDP_Api.DTOs;

public sealed class CreateConsentNoticeVersionRequest
{
    public int OrganisationId { get; set; }
    public int ConsentNoticeId { get; set; }
    public string LanguageCode { get; set; } = "en";
    public string NoticeTitle { get; set; } = string.Empty;
    public string NoticeContent { get; set; } = string.Empty;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public sealed class UpdateConsentNoticeVersionRequest
{
    public int NoticeVersionId { get; set; }
    public int OrganisationId { get; set; }
    public string NoticeTitle { get; set; } = string.Empty;
    public string NoticeContent { get; set; } = string.Empty;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}