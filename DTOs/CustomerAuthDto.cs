
using System.ComponentModel.DataAnnotations;

namespace DPDP_Api.DTOs
{
    // Customer Send OTP Request
    public class CustomerSendOtpDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [MaxLength(254)]
        public string Email { get; set; } = string.Empty;
    }

    // Customer Verify OTP Request
    public class CustomerVerifyOtpDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [MaxLength(254)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "OTP is required.")]
        [RegularExpression(
            @"^\d{6}$",
            ErrorMessage = "OTP must contain exactly 6 digits.")]
        public string Otp { get; set; } = string.Empty;
    }
}
