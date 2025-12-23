using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs;

public class CreateAssetDto
{
    [Required(ErrorMessage = "CategoryId is required")]
    [StringLength(64, ErrorMessage = "Category code cannot exceed 64 characters")]
    public string CategoryId { get; set; } = null!;

    [Required(ErrorMessage = "Mã tài sản là bắt buộc")]
    [StringLength(7, MinimumLength = 7, ErrorMessage = "Mã tài sản phải có đúng 7 ký tự")]
    [RegularExpression(@"^[A-Z]{3}_\d{3}$", ErrorMessage = "Mã phải có dạng ABC_123 (VD: FAN_001, AIR_999)")]
    public string Code { get; set; } = null!;

    [Required(ErrorMessage = "Tên tài sản là bắt buộc")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên tài sản phải từ 3 đến 50 ký tự")]
    [RegularExpression(@"^[A-ZÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸĐ][a-zA-Z0-9ÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚĂĐĨŨƠàáâãèéêìíòóôõùúăđĩũơƯĂẠẢẤẦẨẪẬẮẰẲẴẶẸẺẼỀỀỂưăạảấầẩẫậắằẳẵặẹẻẽềềểỄỆỈỊỌỎỐỒỔỖỘỚỜỞỠỢỤỦỨỪễệỉịọỏốồổỗộớờởỡợụủứừỬỮỰỲỴÝỶỸửữựỳỵỷỹ\s]*$", ErrorMessage = "Chữ cái đầu phải viết hoa, chỉ chứa chữ cái, số và khoảng trắng")]
    public string Name { get; set; } = null!;

    public Guid? ApartmentId { get; set; }

    public Guid? BlockId { get; set; }

    [Required(ErrorMessage = "Vị trí là bắt buộc")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Vị trí phải từ 3 đến 50 ký tự")]
    [RegularExpression(@"^[A-ZÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸĐ][a-zA-Z0-9ÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚĂĐĨŨƠàáâãèéêìíòóôõùúăđĩũơƯĂẠẢẤẦẨẪẬẮẰẲẴẶẸẺẼỀỀỂưăạảấầẩẫậắằẳẵặẹẻẽềềểỄỆỈỊỌỎỐỒỔỖỘỚỜỞỠỢỤỦỨỪễệỉịọỏốồổỗộớờởỡợụủứừỬỮỰỲỴÝỶỸửữựỳỵỷỹ\s,]*$", ErrorMessage = "Chữ cái đầu phải viết hoa, chỉ chứa chữ cái, số, khoảng trắng và dấu phẩy")]
    public string? Location { get; set; }

    public DateOnly? PurchaseDate { get; set; }

    public DateOnly? WarrantyExpire { get; set; }

    public int? MaintenanceFrequency { get; set; }

    [StringLength(32, ErrorMessage = "Status cannot exceed 32 characters")]
    public string Status { get; set; } = "ACTIVE";
}

