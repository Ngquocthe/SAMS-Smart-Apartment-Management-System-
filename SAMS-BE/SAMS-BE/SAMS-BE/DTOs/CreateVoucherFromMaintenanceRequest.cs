using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs
{
    public class CreateVoucherFromMaintenanceRequest
    {
        [Required]
        public Guid HistoryId { get; set; }

        [Required(ErrorMessage = "Số tiền là bắt buộc")]
        [Range(10000, 100000000, ErrorMessage = "Số tiền phải từ 10.000 đến 100.000.000 VNĐ")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Bên sửa chữa là bắt buộc")]
        [StringLength(500, MinimumLength = 3, ErrorMessage = "Bên sửa chữa phải từ 3 đến 500 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9ÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚĂĐĨŨƠàáâãèéêìíòóôõùúăđĩũơƯĂẠẢẤẦẨẪẬẮẰẲẴẶẸẺẼỀỀỂưăạảấầẩẫậắằẳẵặẹẻẽềềểỄỆỈỊỌỎỐỒỔỖỘỚỜỞỠỢỤỦỨỪễệỉịọỏốồổỗộớờởỡợụủứừỬỮỰỲỴÝỶỸửữựỳỵỷỹ\s\-.,()&]+$", 
            ErrorMessage = "Bên sửa chữa chỉ được chứa chữ cái, số và các ký tự: dấu cách, gạch ngang, dấu chấm, dấu phẩy, ngoặc đơn, và dấu &")]
        public string CompanyInfo { get; set; } = null!;

        [StringLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9ÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚĂĐĨŨƠàáâãèéêìíòóôõùúăđĩũơƯĂẠẢẤẦẨẪẬẮẰẲẴẶẸẺẼỀỀỂưăạảấầẩẫậắằẳẵặẹẻẽềềểỄỆỈỊỌỎỐỒỔỖỘỚỜỞỠỢỤỦỨỪễệỉịọỏốồổỗộớờởỡợụủứừỬỮỰỲỴÝỶỸửữựỳỵỷỹ\s,.!?\-():/\n\r]+$", 
            ErrorMessage = "Ghi chú chỉ được chứa chữ cái, số và các ký tự: dấu cách, dấu chấm, dấu phẩy, dấu chấm than, dấu hỏi, gạch ngang, ngoặc đơn, dấu hai chấm, dấu gạch chéo")]
        public string? Note { get; set; }

        public Guid? ServiceTypeId { get; set; }
    }
}