namespace SAMS_BE.DTOs
{
    /// <summary>
    /// DTO response cho vehicle type
    /// </summary>
    public class VehicleTypeDto
    {
        public Guid VehicleTypeId { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
    }
}
