using System.ComponentModel.DataAnnotations;
using SAMS_BE.DTOs.Request.Apartment;

namespace SAMS_BE.DTOs.Request.Resident
{
    public class AddExistingResidentRequest
    {
        [Required(ErrorMessage = "Số giấy tờ không được để trống")]
        [MaxLength(64, ErrorMessage = "Số giấy tờ tối đa 64 ký tự")]
        public string IdNumber { get; set; } = null!;
        public string Username { get; set; } = string.Empty;
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [MaxLength(190, ErrorMessage = "Email tối đa 190 ký tự")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Cư dân phải được gán ít nhất một căn hộ")]
        public List<ResidentApartmentRequest>? Apartments { get; set; }
    }
}

