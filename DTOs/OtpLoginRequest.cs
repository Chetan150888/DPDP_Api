
using System.ComponentModel.DataAnnotations;
namespace DPDP_Api.DTOs
{
    public class OtpLoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Otp { get; set; } = string.Empty;
    }
}
