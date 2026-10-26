using System.ComponentModel.DataAnnotations;

namespace DPDP_Api.DTOs
{
    public class DataElementRequest
    {
        public int? OrganisationId { get; set; }

        [Required, StringLength(40)]
        public string DataElementCode { get; set; } = string.Empty;

        [Required, StringLength(160)]
        public string DataElementName { get; set; } = string.Empty;

        [StringLength(30)]
        public string? DataType { get; set; }

        [StringLength(100)]
        public string? DataCategory { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsSensitive { get; set; }
        public bool IsChildData { get; set; }
    }

    public sealed class UpdateDataElementRequest : DataElementRequest
    {
        [Range(1, int.MaxValue)]
        public int DataElementId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
