using System;

namespace SAMS_BE.DTOs;

public class AccessCardCapabilityDto
{
    public Guid CardCapabilityId { get; set; }
    public Guid CardId { get; set; }
    public Guid CardTypeId { get; set; }
    public string CardTypeName { get; set; } = null!;
    public string CardTypeCode { get; set; } = null!;
    public bool IsEnabled { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

public class CreateAccessCardCapabilityDto
{
    public Guid CardId { get; set; }
    public Guid CardTypeId { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public string CreatedBy { get; set; } = "buildingmanager";
}

public class UpdateAccessCardCapabilityDto
{
    public bool? IsEnabled { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public string? UpdatedBy { get; set; }
}
