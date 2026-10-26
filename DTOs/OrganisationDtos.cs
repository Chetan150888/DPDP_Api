using System.ComponentModel.DataAnnotations;

namespace DPDP_Api.DTOs;

public class CreateOrganisationRequest
{
   
    public string OrganisationCode { get; set; } = string.Empty;
    public string OrganisationName { get; set; } = string.Empty;

  
    public string? OrganisationType { get; set; }

   
    public string? Industry { get; set; }

 
    public string? RegisteredAddress { get; set; }

  
    public int StateId { get; set; }

   
    public int CityId { get; set; }

  
    public string? PinCode { get; set; }

  
    public string? ContactEmail { get; set; }

  
    public string? ContactNumber { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public sealed class UpdateOrganisationRequest : CreateOrganisationRequest
{
    [Range(1, int.MaxValue)]
    public int OrganisationId { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class CreateOrgAdminRequest
{
    [Range(1, int.MaxValue)]
    public int OrganisationId { get; set; }

    [Required]
    [MaxLength(80)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? MobileNumber { get; set; }

    [MaxLength(50)]
    public string? EmployeeCode { get; set; }

    public int? DepartmentId { get; set; }

    public int? DesignationId { get; set; }

    public int? LocationId { get; set; }

    [Required]
    public string Password { get; set; } = string.Empty;
}

public sealed class UpdateOrgAdminRequest
{
    [Range(1, int.MaxValue)]
    public int UserId { get; set; }

    [Required]
    [MaxLength(120)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? MobileNumber { get; set; }

    public int? DepartmentId { get; set; }

    public int? DesignationId { get; set; }

    public int? LocationId { get; set; }

    public bool IsActive { get; set; } = true;
}