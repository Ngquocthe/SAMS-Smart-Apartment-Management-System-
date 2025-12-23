using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs.Request.Building
{
    public class CreateBuildingRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập mã tòa nhà")]
        [MaxLength(30, ErrorMessage = "Tối đa 30 ký tự")]
        [RegularExpression(@"^[A-Za-z0-9-_]+$", ErrorMessage = "Chỉ cho phép chữ không dấu, số, gạch ngang và gạch dưới, không có khoảng trắng")]
        public string? Code { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên tòa nhà")]
        public string? BuildingName { get; set; }

        public string? Description { get; set; }

        public decimal? TotalAreaM2 { get; set; }

        public DateTime? OpeningDate { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public IFormFile? Avatar { get; set; } 
    }

}
