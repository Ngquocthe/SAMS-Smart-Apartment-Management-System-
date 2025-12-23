using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs;

public class UpdateAmenityDto
{
    [Required(ErrorMessage = "Mã tiện ích là bắt buộc")]
    [StringLength(7, MinimumLength = 7, ErrorMessage = "Mã tiện ích phải có đúng 7 ký tự")]
    [RegularExpression(@"^[A-Z]{3}_\d{3}$", ErrorMessage = "Mã phải có dạng ABC_123 (VD: GYM_001, POL_999)")]
    public string Code { get; set; } = null!;

    [Required(ErrorMessage = "Tên tiện ích là bắt buộc")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên tiện ích phải từ 3 đến 50 ký tự")]
    [RegularExpression(@"^[A-ZÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸĐ][a-zA-Z0-9ÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚĂĐĨŨƠàáâãèéêìíòóôõùúăđĩũơƯĂẠẢẤẦẨẪẬẮẰẲẴẶẸẺẼỀỀỂưăạảấầẩẫậắằẳẵặẹẻẽềềểỄỆỈỊỌỎỐỒỔỖỘỚỜỞỠỢỤỦỨỪễệỉịọỏốồổỗộớờởỡợụủứừỬỮỰỲỴÝỶỸửữựỳỵỷỹ\s]*$", ErrorMessage = "Chữ cái đầu phải viết hoa, chỉ chứa chữ cái, số và khoảng trắng")]
    public string Name { get; set; } = null!;

    public Guid? AssetId { get; set; }

    [StringLength(100, ErrorMessage = "CategoryName cannot exceed 100 characters.")]
    public string? CategoryName { get; set; }

    [StringLength(50, MinimumLength = 3, ErrorMessage = "Vị trí phải từ 3 đến 50 ký tự")]
    [RegularExpression(@"^[a-zA-Z0-9ÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚĂĐĨŨƠàáâãèéêìíòóôõùúăđĩũơƯĂẠẢẤẦẨẪẬẮẰẲẴẶẸẺẼỀỀỂưăạảấầẩẫậắằẳẵặẹẻẽềềểỄỆỈỊỌỎỐỒỔỖỘỚỜỞỠỢỤỦỨỪễệỉịọỏốồổỗộớờởỡợụủứừỬỮỰỲỴÝỶỸửữựỳỵỷỹ\s,]+$", ErrorMessage = "Vị trí không được chứa ký tự đặc biệt (cho phép dấu phẩy)")]
    public string? Location { get; set; }

    public bool HasMonthlyPackage { get; set; } = true;

    public bool RequiresFaceVerification { get; set; } = false;

    [Required(ErrorMessage = "FeeType is required.")]
    [StringLength(20, ErrorMessage = "FeeType cannot exceed 20 characters.")]
    public string FeeType { get; set; } = "Paid";

    [Required(ErrorMessage = "Status is required.")]
    [StringLength(32, ErrorMessage = "Status cannot exceed 32 characters.")]
    public string Status { get; set; } = "ACTIVE";

    /// <summary>
    /// Danh sách các gói giá mới (nếu có)
    /// Nếu null: không thay đổi packages hiện tại
    /// Nếu empty list: xóa tất cả packages
    /// Nếu có items: thay thế toàn bộ packages cũ bằng mới
    /// </summary>
    public List<UpdateAmenityPackageInlineDto>? Packages { get; set; }
}

/// <summary>
/// DTO cho việc update package inline khi update amenity
/// </summary>
public class UpdateAmenityPackageInlineDto
{
    /// <summary>
    /// Nếu có PackageId: update package đó
    /// Nếu null: tạo package mới
    /// </summary>
    public Guid? PackageId { get; set; }

    [Required(ErrorMessage = "Package name is required")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; } = null!;

    public int MonthCount { get; set; }

    [Range(1, 365, ErrorMessage = "Duration days must be between 1 and 365")]
    public int? DurationDays { get; set; }

    [StringLength(10, ErrorMessage = "Period unit cannot exceed 10 characters")]
    [RegularExpression("^(Day|Month)$", ErrorMessage = "Period unit must be either 'Day' or 'Month'")]
    public string? PeriodUnit { get; set; }

    [Required(ErrorMessage = "Price is required")]
    [Range(0, int.MaxValue, ErrorMessage = "Price must be non-negative")]
    public int Price { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    [StringLength(32, ErrorMessage = "Status cannot exceed 32 characters")]
    public string Status { get; set; } = "ACTIVE";
}

