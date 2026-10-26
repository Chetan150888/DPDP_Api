using System.ComponentModel.DataAnnotations;
namespace DPDP_Api.DTOs;

public sealed class DataElementCategoryRequest
{
    public int? OrganisationId { get; set; }
    [Required, StringLength(80)] public string CategoryCode { get; set; } = string.Empty;
    [Required, StringLength(160)] public string CategoryName { get; set; } = string.Empty;
    [StringLength(500)] public string? Description { get; set; }
}

public sealed class UpdateDataElementCategoryRequest
{
    [Range(1, int.MaxValue)] public int CategoryId { get; set; }
    public int? OrganisationId { get; set; }
    [Required, StringLength(80)] public string CategoryCode { get; set; } = string.Empty;
    [Required, StringLength(160)] public string CategoryName { get; set; } = string.Empty;
    [StringLength(500)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
