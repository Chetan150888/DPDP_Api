using System.ComponentModel.DataAnnotations;

namespace DPDP_Api.DTOs;

public sealed class EmployeeCreateRequest
{
    public int? OrganisationId { get; set; }

    [Required]
    [StringLength(50)]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string EmployeeName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [StringLength(30)]
    public string? MobileNumber { get; set; }

    public int? DepartmentId { get; set; }

    public int? DesignationId { get; set; }

    public int? LocationId { get; set; }

    public int? ReportingManagerId { get; set; }

    public DateTime? DateOfJoining { get; set; }
}

public sealed class EmployeeUpdateRequest
{
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    public int? OrganisationId { get; set; }

    [Required]
    [StringLength(50)]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string EmployeeName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [StringLength(30)]
    public string? MobileNumber { get; set; }

    public int? DepartmentId { get; set; }

    public int? DesignationId { get; set; }

    public int? LocationId { get; set; }

    public int? ReportingManagerId { get; set; }

    public DateTime? DateOfJoining { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class EmployeeGrantAccessRequest
{
    public int? OrganisationId { get; set; }

    [Range(1, int.MaxValue)]
    public int RoleId { get; set; }

    [Required]
    public string TemporaryPassword { get; set; } = string.Empty;
}

public sealed class EmployeeChangeRoleRequest
{
    public int? OrganisationId { get; set; }

    [Range(1, int.MaxValue)]
    public int RoleId { get; set; }
}

public sealed class EmployeeRevokeAccessRequest
{
    public int? OrganisationId { get; set; }
}
