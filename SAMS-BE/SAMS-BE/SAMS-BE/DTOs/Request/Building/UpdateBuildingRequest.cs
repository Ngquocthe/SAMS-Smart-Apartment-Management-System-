using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs.Request.Building
{
    public class UpdateBuildingRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập tên tòa nhà")]
        public string? BuildingName { get; set; }

        public string? Description { get; set; }

        public decimal? TotalAreaM2 { get; set; }

        public DateTime? OpeningDate { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public IFormFile? Avatar { get; set; }

        public byte? Status { get; set; }
    }
}
