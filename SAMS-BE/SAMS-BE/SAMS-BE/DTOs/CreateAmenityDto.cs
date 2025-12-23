using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs;

public class CreateAmenityDto
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

    [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters")]
    public string? CategoryName { get; set; }

    [StringLength(50, MinimumLength = 3, ErrorMessage = "Vị trí phải từ 3 đến 50 ký tự")]
    [RegularExpression(@"^[a-zA-Z0-9ÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚĂĐĨŨƠàáâãèéêìíòóôõùúăđĩũơƯĂẠẢẤẦẨẪẬẮẰẲẴẶẸẺẼỀỀỂưăạảấầẩẫậắằẳẵặẹẻẽềềểỄỆỈỊỌỎỐỒỔỖỘỚỜỞỠỢỤỦỨỪễệỉịọỏốồổỗộớờởỡợụủứừỬỮỰỲỴÝỶỸửữựỳỵỷỹ\s,]+$", ErrorMessage = "Vị trí không được chứa ký tự đặc biệt (cho phép dấu phẩy)")]
    public string? Location { get; set; }

    public bool HasMonthlyPackage { get; set; } = true;

    public bool RequiresFaceVerification { get; set; } = false;

    [Required]
    [StringLength(20, ErrorMessage = "Fee type cannot exceed 20 characters")]
    public string FeeType { get; set; } = "Paid";

    [Required]
    [StringLength(32, ErrorMessage = "Status cannot exceed 32 characters")]
    public string Status { get; set; } = "ACTIVE";

    /// <summary>
    /// Danh sách các gói giá cho tiện ích (VD: 1 tháng 300k, 3 tháng 550k)
    /// Nếu không có gói nào, amenity sẽ được tạo không có packages
    /// </summary>
    public List<CreateAmenityPackageInlineDto>? Packages { get; set; }
}

/// <summary>
/// DTO cho việc tạo package inline khi tạo amenity
/// </summary>
public class CreateAmenityPackageInlineDto
{
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
    [Range(10000, 10000000, ErrorMessage = "Price must be between 10,000 and 10,000,000")]
    public int Price { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    [StringLength(32, ErrorMessage = "Status cannot exceed 32 characters")]
    public string Status { get; set; } = "ACTIVE";
}
