using System.ComponentModel.DataAnnotations;
using SAMS_BE.Enums;

namespace SAMS_BE.DTOs
{
    public class CreateSingleFloorRequestDto
    {
        [Required(ErrorMessage = "Số tầng là bắt buộc")]
        public int FloorNumber { get; set; }

        [Required(ErrorMessage = "Loại tầng là bắt buộc")]
        public FloorType FloorType { get; set; }

        public string? Name { get; set; }
    }

    public class CreateFloorsRequestDto
    {
        [Required(ErrorMessage = "Loại tầng là bắt buộc")]
        public FloorType FloorType { get; set; }

        [Required(ErrorMessage = "Số lượng tầng là bắt buộc")]
        [Range(1, 100, ErrorMessage = "Số lượng tầng phải từ 1 đến 100")]
        public int Count { get; set; }

        /// <summary>
        /// Số tầng bắt đầu (chỉ dùng cho các loại tầng không phải BASEMENT)
        /// Ví dụ: StartFloor = 1 sẽ tạo tầng 1, 2, 3, ...
        /// </summary>
        public int? StartFloor { get; set; }

        public List<int>? ExcludeFloors { get; set; } = new List<int>();
    }

    public class FloorResponseDto
    {
        public Guid FloorId { get; set; }
        public int FloorNumber { get; set; }
        public string? Name { get; set; }
        public string? FloorType { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int ApartmentCount { get; set; }
    }

    public class UpdateFloorRequestDto
    {
        public string? Name { get; set; }
        public FloorType? FloorType { get; set; }
    }

    public class CreateSingleFloorResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public FloorResponseDto? CreatedFloor { get; set; }
    }

    public class CreateFloorsResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<FloorResponseDto> CreatedFloors { get; set; } = new List<FloorResponseDto>();
        public int TotalCreated { get; set; }
        public List<int>? SkippedFloors { get; set; } = new List<int>();
    }
}
