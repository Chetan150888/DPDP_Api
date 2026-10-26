using System.ComponentModel.DataAnnotations;

namespace DPDP_Api.DTOs;

public class CreateStateRequest
{
    [Required, MaxLength(10)] public string StateCode { get; set; } = string.Empty;
    [Required, MaxLength(120)] public string StateName { get; set; } = string.Empty;
}

public sealed class UpdateStateRequest : CreateStateRequest
{
    [Range(1, int.MaxValue)] public int StateId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CreateCityRequest
{
    [Range(1, int.MaxValue)] public int StateId { get; set; }
    [Required, MaxLength(20)] public string CityCode { get; set; } = string.Empty;
    [Required, MaxLength(120)] public string CityName { get; set; } = string.Empty;
}

public sealed class UpdateCityRequest : CreateCityRequest
{
    [Range(1, int.MaxValue)] public int CityId { get; set; }
    public bool IsActive { get; set; } = true;
}
