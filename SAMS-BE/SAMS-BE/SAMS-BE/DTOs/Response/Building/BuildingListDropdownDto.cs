namespace SAMS_BE.DTOs.Response.Building
{
    public class BuildingListDropdownDto
    {
        public Guid Id { get; init; }
        public string BuildingName { get; init; } = "";
        public string SchemaName { get; init; } = "";
    }
}
