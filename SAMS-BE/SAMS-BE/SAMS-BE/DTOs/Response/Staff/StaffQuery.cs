namespace SAMS_BE.DTOs.Response.Staff
{
    public sealed class StaffQuery
    {
        public string? Search { get; init; }
        public Guid? RoleId { get; init; }

        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string? Sort { get; init; }
    }
}
