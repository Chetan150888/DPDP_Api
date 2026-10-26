using System.ComponentModel.DataAnnotations;

namespace DPDP_Api.DTOs;

public class DepartmentRequest
{
    [Range(1, int.MaxValue)] public int OrganisationId { get; set; }
    public int? BusinessUnitId { get; set; }
    public int? ParentDepartmentId { get; set; }
    [Required, MaxLength(40)] public string DepartmentCode { get; set; } = string.Empty;
    [Required, MaxLength(160)] public string DepartmentName { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
}
public sealed class UpdateDepartmentRequest : DepartmentRequest { [Range(1,int.MaxValue)] public int DepartmentId { get; set; } public bool IsActive { get; set; } = true; }

public class DesignationRequest
{
    [Range(1, int.MaxValue)] public int OrganisationId { get; set; }
    [Required, MaxLength(40)] public string DesignationCode { get; set; } = string.Empty;
    [Required, MaxLength(160)] public string DesignationName { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
}
public sealed class UpdateDesignationRequest : DesignationRequest { [Range(1,int.MaxValue)] public int DesignationId { get; set; } public bool IsActive { get; set; } = true; }

public class LocationRequest
{
    [Range(1, int.MaxValue)] public int OrganisationId { get; set; }
    public int? ParentLocationId { get; set; }
    [Required, MaxLength(40)] public string LocationCode { get; set; } = string.Empty;
    [Required, MaxLength(180)] public string LocationName { get; set; } = string.Empty;
    [Required, RegularExpression("^(LOCATION|BRANCH)$")] public string LocationType { get; set; } = "LOCATION";
    [MaxLength(250)] public string? AddressLine1 { get; set; }
    [MaxLength(250)] public string? AddressLine2 { get; set; }
    public int? StateId { get; set; }
    public int? CityId { get; set; }
    [MaxLength(20)] public string? PinCode { get; set; }
    public bool IsProcessingLocation { get; set; }
}
public sealed class UpdateLocationRequest : LocationRequest { [Range(1,int.MaxValue)] public int LocationId { get; set; } public bool IsActive { get; set; } = true; }

public class BusinessUnitRequest
{
    [Range(1,int.MaxValue)] public int OrganisationId { get; set; }
    [Required, MaxLength(40)] public string BusinessUnitCode { get; set; } = string.Empty;
    [Required, MaxLength(160)] public string BusinessUnitName { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
}
public sealed class UpdateBusinessUnitRequest : BusinessUnitRequest { [Range(1,int.MaxValue)] public int BusinessUnitId { get; set; } public bool IsActive { get; set; } = true; }

public class LegalEntityRequest
{
    [Range(1,int.MaxValue)] public int OrganisationId { get; set; }
    [Required, MaxLength(40)] public string LegalEntityCode { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string LegalEntityName { get; set; } = string.Empty;
    [MaxLength(80)] public string? EntityType { get; set; }
    [MaxLength(50)] public string? CIN { get; set; }
    [MaxLength(20)] public string? PAN { get; set; }
    [MaxLength(500)] public string? RegisteredAddress { get; set; }
    public int? StateId { get; set; }
    public int? CityId { get; set; }
    [MaxLength(20)] public string? PinCode { get; set; }
}
public sealed class UpdateLegalEntityRequest : LegalEntityRequest { [Range(1,int.MaxValue)] public int LegalEntityId { get; set; } public bool IsActive { get; set; } = true; }

public class SubsidiaryRequest
{
    [Range(1,int.MaxValue)] public int OrganisationId { get; set; }
    public int? LegalEntityId { get; set; }
    [Required, MaxLength(40)] public string SubsidiaryCode { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string SubsidiaryName { get; set; } = string.Empty;
    [MaxLength(3)] public string CountryCode { get; set; } = "IND";
    [Range(0,100)] public decimal? OwnershipPercent { get; set; }
}
public sealed class UpdateSubsidiaryRequest : SubsidiaryRequest { [Range(1,int.MaxValue)] public int SubsidiaryId { get; set; } public bool IsActive { get; set; } = true; }
