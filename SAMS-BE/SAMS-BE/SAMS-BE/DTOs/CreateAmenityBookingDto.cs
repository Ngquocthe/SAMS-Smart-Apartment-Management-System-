using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs;

/// <summary>
/// DTO cho việc tạo đăng ký tiện ích mới
/// - ApartmentId: optional - nếu không gửi thì backend tự động lấy primary apartment
/// - StartDate: TỰ ĐỘNG - hệ thống luôn lấy ngày hiện tại (ngày đăng ký)
/// - EndDate: TỰ ĐỘNG - hệ thống tính = StartDate + số tháng của package
/// </summary>
public class CreateAmenityBookingDto
{
    [Required(ErrorMessage = "Amenity ID is required")]
    public Guid AmenityId { get; set; }

    [Required(ErrorMessage = "Package ID is required")]
    public Guid PackageId { get; set; }

    /// <summary>
    /// ApartmentId của căn hộ đăng ký (optional)
    /// Nếu null: Backend tự động lấy primary apartment của user
    /// </summary>
    public Guid? ApartmentId { get; set; }

    [StringLength(1000, MinimumLength = 3, ErrorMessage = "Ghi chú phải từ 3 đến 1000 ký tự")]
    [RegularExpression(@"^[a-zA-Z0-9ÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚĂĐĨŨƠàáâãèéêìíòóôõùúăđĩũơƯĂẠẢẤẦẨẪẬẮẰẲẴẶẸẺẼỀỀỂưăạảấầẩẫậắằẳẵặẹẻẽềềểỄỆỈỊỌỎỐỒỔỖỘỚỜỞỠỢỤỦỨỪễệỉịọỏốồổỗộớờởỡợụủứừỬỮỰỲỴÝỶỸửữựỳỵỷỹ\s,.!?\-]+$", ErrorMessage = "Ghi chú không được chứa ký tự đặc biệt")]
    public string? Notes { get; set; }
}
