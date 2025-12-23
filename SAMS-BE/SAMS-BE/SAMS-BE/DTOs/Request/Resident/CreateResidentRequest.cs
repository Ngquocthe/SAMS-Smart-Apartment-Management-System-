using System.ComponentModel.DataAnnotations;
using SAMS_BE.DTOs.Request.Apartment;

namespace SAMS_BE.DTOs.Request.Resident
{
    public class CreateResidentRequest
    {
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên cư dân không được để trống")]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "Họ cư dân không được để trống")]
        public string LastName { get; set; } = null!;

        [MaxLength(50, ErrorMessage = "Số điện thoại tối đa 50 ký tự")]
        public string? Phone { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [MaxLength(190, ErrorMessage = "Email tối đa 190 ký tự")]
        public string? Email { get; set; }

        [MaxLength(64, ErrorMessage = "Số giấy tờ tối đa 64 ký tự")]
        public string? IdNumber { get; set; }

        public DateOnly? Dob { get; set; }

        [MaxLength(16, ErrorMessage = "Giới tính tối đa 16 ký tự")]
        [Required(ErrorMessage = "Giới tính không được để trống")]
        public string? Gender { get; set; }

        [MaxLength(500, ErrorMessage = "Quê quán tối đa 500 ký tự")]
        public string? Address { get; set; }

        [StringLength(32)]
        public string? Status { get; set; }

        public bool? IsVerified { get; set; } = true;

        public DateTime? VerifiedAt { get; set; }

        [MaxLength(64, ErrorMessage = "Quốc tịch tối đa 64 ký tự")]
        public string? Nationality { get; set; }

        [MaxLength(1000, ErrorMessage = "Ghi chú nội bộ tối đa 1000 ký tự")]
        public string? InternalNote { get; set; }

        public string? Meta { get; set; }

        [Required(ErrorMessage = "Cư dân phải được gán ít nhất một căn hộ")]
        public List<ResidentApartmentRequest>? Apartments { get; set; }
    }
}
