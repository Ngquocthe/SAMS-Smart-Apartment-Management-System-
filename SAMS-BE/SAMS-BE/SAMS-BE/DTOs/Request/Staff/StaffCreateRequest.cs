using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs.Request.Staff
{
    public sealed class StaffCreateRequest
    {

        [Required(ErrorMessage = "Tên tài khoản không được để trống")]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên không được để trống")]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ không được để trống")]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        [MaxLength(255)]
        public string Address { get; set; } = string.Empty;

        public DateTime? Dob { get; set; }

        public string? CurrentAddress { get; set; }

        [Required(ErrorMessage = "Hãy chọn tòa nhà làm việc")]
        public Guid BuildingId { get; set; }

        [Required(ErrorMessage = "Hãy chọn vị trí làm việc")]
        public string RoleId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nhân viên cần có ít nhất một quyền")]
        public List<string> AccessRoles { get; set; } = new();

        [Required(ErrorMessage = "Ngày bắt đầu làm việc không được để trống")]
        public DateTime HireDate { get; set; }

        [Required(ErrorMessage = "Lương cơ bản không được để trống")]
        [Range(0, double.MaxValue, ErrorMessage = "Lương cơ bản phải lớn hơn 0")]
        public decimal BaseSalary { get; set; }

        public DateTime? TerminationDate { get; set; }
        public bool IsActive { get; set; }
        public string? Notes { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? EmergencyContactRelation { get; set; }
        public string? BankAccountNo { get; set; }
        public string? BankName { get; set; }
        public string? BankBranch { get; set; }
        public string? TaxCode { get; set; }
        public string? SocialInsuranceNo { get; set; }

        // ===== File =====
        public IFormFile? Avatar { get; set; }
        public IFormFile? CardPhoto { get; set; }
    }
}
