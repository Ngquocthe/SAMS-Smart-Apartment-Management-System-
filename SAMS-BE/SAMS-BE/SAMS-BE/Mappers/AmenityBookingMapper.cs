using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Mappers;

public static class AmenityBookingMapper
{
    /// <summary>
    /// Map từ AmenityBooking entity sang AmenityBookingDto
    /// </summary>
    public static AmenityBookingDto ToDto(this AmenityBooking entity)
    {
        var dto = new AmenityBookingDto
        {
            BookingId = entity.BookingId,
            AmenityId = entity.AmenityId,
            AmenityName = entity.Amenity?.Name,
            PackageId = entity.PackageId,
            PackageName = entity.Package?.Name,
            MonthCount = entity.Package?.MonthCount,
            DurationDays = entity.Package?.DurationDays,
            PeriodUnit = entity.Package?.PeriodUnit,
            ApartmentId = entity.ApartmentId,
            ApartmentCode = entity.Apartment?.Number,
            UserId = entity.UserId,
            UserName = entity.User?.Username,
            UserPhone = entity.User?.Phone,
            ResidentName = entity.User?.ResidentProfile?.FullName ??
                            ($"{entity.User?.LastName} {entity.User?.FirstName}").Trim(),
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Status = entity.Status,
            TotalPrice = entity.TotalPrice,
            Price = entity.Price,
            PaymentStatus = entity.PaymentStatus,
            Notes = entity.Notes,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            Amenity = entity.Amenity?.ToDto(),
            Package = entity.Package?.ToDto(),
            Location = entity.Amenity?.Location
        };
        dto.CanCancel = string.Equals(entity.Status, "Pending", StringComparison.OrdinalIgnoreCase);
        return dto;
    }

    /// <summary>
    /// Map từ CreateAmenityBookingDto sang AmenityBooking entity
    /// - ApartmentId sẽ được gán sau trong Service hoặc Controller
    /// - StartDate: TỰ ĐỘNG lấy ngày hiện tại (ngày đăng ký)
    /// - EndDate: TỰ ĐỘNG tính = StartDate + MonthCount (nếu Month) hoặc DurationDays (nếu Day)
    /// - Price: Lấy từ Package
    /// </summary>
    public static AmenityBooking ToEntity(this CreateAmenityBookingDto dto, Guid userId, int packagePrice, int monthCount, int? durationDays = null, string? periodUnit = null)
    {
        // Luôn luôn lấy ngày hiện tại làm StartDate (theo giờ Việt Nam UTC+7)
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        
        // Tính EndDate dựa trên period_unit
        DateOnly endDate;
        if (periodUnit == "Day" && durationDays.HasValue && durationDays.Value > 0)
        {
            // Tính theo ngày
            endDate = startDate.AddDays(durationDays.Value);
        }
        else
        {
            // Tính theo tháng (mặc định hoặc periodUnit = "Month")
            endDate = startDate.AddMonths(monthCount);
        }
        
        return new AmenityBooking
        {
            BookingId = Guid.NewGuid(),
            AmenityId = dto.AmenityId,
            PackageId = dto.PackageId,
            // ApartmentId sẽ được gán trong Service/Controller
            UserId = userId,
            StartDate = startDate,
            EndDate = endDate,
            Price = packagePrice,
            TotalPrice = packagePrice, // Có thể cộng thêm phụ phí nếu cần
            Status = "Pending", // Trạng thái mặc định
            PaymentStatus = "Unpaid", // Chưa thanh toán
            Notes = dto.Notes,
            // Lưu theo giờ Việt Nam (UTC+7) để đồng bộ với default constraint DB
            CreatedAt = DateTime.UtcNow.AddHours(7),
            CreatedBy = userId.ToString()
        };
    }

    /// <summary>
    /// Map từ UpdateAmenityBookingDto để cập nhật entity
    /// StartDate giữ nguyên, chỉ update PackageId và tính lại EndDate
    /// </summary>
    public static void UpdateEntity(this AmenityBooking entity, UpdateAmenityBookingDto dto, Guid userId, int packagePrice, int monthCount, int? durationDays = null, string? periodUnit = null)
    {
        entity.PackageId = dto.PackageId;
        
        // Tính lại EndDate dựa trên period_unit của package mới
        if (periodUnit == "Day" && durationDays.HasValue && durationDays.Value > 0)
        {
            // Tính theo ngày
            entity.EndDate = entity.StartDate.AddDays(durationDays.Value);
        }
        else
        {
            // Tính theo tháng (mặc định hoặc periodUnit = "Month")
            entity.EndDate = entity.StartDate.AddMonths(monthCount);
        }
        
        entity.Price = packagePrice;
        entity.TotalPrice = packagePrice;
        entity.Notes = dto.Notes;
        entity.UpdatedAt = DateTime.UtcNow.AddHours(7);
        entity.UpdatedBy = userId.ToString();
    }

    /// <summary>
    /// Map collection từ AmenityBooking entities sang AmenityBookingDto list
    /// </summary>
    public static IEnumerable<AmenityBookingDto> ToDto(this IEnumerable<AmenityBooking> entities)
    {
        return entities.Select(e => e.ToDto());
    }
}

