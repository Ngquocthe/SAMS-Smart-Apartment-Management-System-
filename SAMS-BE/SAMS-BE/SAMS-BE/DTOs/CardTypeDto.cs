using System;

namespace SAMS_BE.DTOs;

public class CardTypeDto
{
    public Guid CardTypeId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
