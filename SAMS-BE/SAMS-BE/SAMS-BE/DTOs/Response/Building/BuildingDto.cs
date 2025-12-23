namespace SAMS_BE.DTOs.Response.Building
{
    public sealed class BuildingDto
    {
        public Guid Id { get; init; }
        public string Code { get; init; } = "";
        public string BuildingName { get; init; } = "";
        public string SchemaName { get; init; } = "";
        public string? AvatarUrl { get; init; }
        public decimal? Latitude { get; init; }
        public decimal? Longitude { get; init; }
        public decimal? TotalAreaM2 { get; init; }
        public string? Description { get; init; }
        public byte? Status { get; init; }
    }
}
