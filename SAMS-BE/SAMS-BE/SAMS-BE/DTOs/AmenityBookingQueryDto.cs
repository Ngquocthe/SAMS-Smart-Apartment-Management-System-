namespace SAMS_BE.DTOs;

/// <summary>
/// DTO cho việc query danh sách bookings với các bộ lọc
/// </summary>
public class AmenityBookingQueryDto
{
    public Guid? AmenityId { get; set; }

    public Guid? ApartmentId { get; set; }

    public Guid? UserId { get; set; }

    public string? Status { get; set; }

    public string? PaymentStatus { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public string? SortBy { get; set; } = "StartDate";

    public string? SortOrder { get; set; } = "desc";
}

