using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs.Request.Apartment
{
    public class ResidentApartmentRequest
    {
        [Required(ErrorMessage = "Căn hộ không được để trống")]
        public Guid ApartmentId { get; set; }

        [Required(ErrorMessage = "Quan hệ với căn hộ không được để trống")]
        [StringLength(32, ErrorMessage = "Quan hệ với căn hộ tối đa 32 ký tự")]
        public string RelationType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngày bắt đầu cư trú không được để trống")]
        public DateOnly? StartDate { get; set; }

        public DateOnly? EndDate { get; set; }

        public bool IsPrimary { get; set; } = false;

        public List<IFormFile>? ContractFiles { get; set; }
    }
}
