using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs
{
    public class CreateApartmentDto
    {
        [Required(ErrorMessage = "Số căn hộ là bắt buộc")]
        [MaxLength(10, ErrorMessage = "Số căn hộ không được vượt quá 10 ký tự")]
        public string Number { get; set; } = string.Empty;

        [Range(0.1, double.MaxValue, ErrorMessage = "Diện tích phải lớn hơn 0")]
        public decimal? AreaM2 { get; set; }

        [Range(0, 10, ErrorMessage = "Số phòng ngủ từ 0 đến 10")]
        public int? Bedrooms { get; set; }

        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        [MaxLength(32)]
        public string Status { get; set; } = "ACTIVE";

        [MaxLength(250)]
        public string? Image { get; set; }

        [MaxLength(100)]
        public string? Type { get; set; }
    }

    public class CreateApartmentsRequestDto
    {
        [Required(ErrorMessage = "Mã tòa là bắt buộc")]
        [MaxLength(10, ErrorMessage = "Mã tòa không được vượt quá 10 ký tự")]
        public string BuildingCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tầng gốc là bắt buộc")]
        public int SourceFloorNumber { get; set; }

        [Required(ErrorMessage = "Danh sách apartments là bắt buộc")]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 apartment")]
        public List<CreateApartmentDto> Apartments { get; set; } = new List<CreateApartmentDto>();
    }

    public class CreateSingleApartmentRequestDto
    {
        [Required(ErrorMessage = "Mã tòa là bắt buộc")]
        [MaxLength(10, ErrorMessage = "Mã tòa không được vượt quá 10 ký tự")]
        public string BuildingCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số tầng là bắt buộc")]
        public int FloorNumber { get; set; }

        [Required(ErrorMessage = "Số căn hộ là bắt buộc (2 chữ số)")]
        [RegularExpression(@"^\d{2}$", ErrorMessage = "Số căn hộ phải là 2 chữ số (01-99)")]
        public string ApartmentNumber { get; set; } = string.Empty;

        [Range(0.1, double.MaxValue, ErrorMessage = "Diện tích phải lớn hơn 0")]
        public decimal? AreaM2 { get; set; }

        [Range(0, 10, ErrorMessage = "Số phòng ngủ từ 0 đến 10")]
        public int? Bedrooms { get; set; }

        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        [MaxLength(32)]
        public string Status { get; set; } = "ACTIVE";

        [MaxLength(250)]
        public string? Image { get; set; }

        [MaxLength(100)]
        public string? Type { get; set; }
    }

    public class ReplicateApartmentsRequestDto
    {
        [Required(ErrorMessage = "Mã tòa là bắt buộc")]
        [MaxLength(10, ErrorMessage = "Mã tòa không được vượt quá 10 ký tự")]
        public string BuildingCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tầng gốc là bắt buộc")]
        public int SourceFloorNumber { get; set; }

        [Required(ErrorMessage = "Danh sách tầng đích là bắt buộc")]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 tầng đích")]
        public List<int> TargetFloorNumbers { get; set; } = new List<int>();
    }

    public class OwnerInfoDto
    {
        public Guid? ResidentId { get; set; }
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }

    public class ResidentInfoDto
    {
        public Guid ResidentId { get; set; }
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string RelationType { get; set; } = string.Empty; // OWNER, FAMILY_MEMBER
        public bool IsPrimary { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class ApartmentResponseDto
    {
        public Guid ApartmentId { get; set; }
        public Guid FloorId { get; set; }
        public string Number { get; set; } = string.Empty;
        public decimal? AreaM2 { get; set; }
        public int? Bedrooms { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Image { get; set; }
        public string? Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }

        // Floor information
        public int FloorNumber { get; set; }
        public string? FloorName { get; set; }

        // Owner information (deprecated - use Residents list)
        public OwnerInfoDto? OwnerInfo { get; set; }

        // Residents list with relation type
        public List<ResidentInfoDto> Residents { get; set; } = new List<ResidentInfoDto>();

        // Resident and vehicle counts
        public int ResidentCount { get; set; }
        public int VehicleCount { get; set; }
    }

    public class CreateApartmentsResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<ApartmentResponseDto> CreatedApartments { get; set; } = new List<ApartmentResponseDto>();
        public int TotalCreated { get; set; }
    }

    public class ReplicateApartmentsResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<ApartmentResponseDto> ReplicatedApartments { get; set; } = new List<ApartmentResponseDto>();
        public int TotalReplicated { get; set; }
        public List<int> SkippedFloors { get; set; } = new List<int>();
        public string? SkippedReason { get; set; }
    }

    public class FloorApartmentSummaryDto
    {
        public int FloorNumber { get; set; }
        public string FloorName { get; set; } = string.Empty;
        public int ApartmentCount { get; set; }
        public bool HasApartments { get; set; }
        public List<ApartmentResponseDto> Apartments { get; set; } = new List<ApartmentResponseDto>();
    }

    public class RefactorApartmentNamesRequestDto
    {
        [Required(ErrorMessage = "Mã tòa mới là bắt buộc")]
        [MaxLength(10, ErrorMessage = "Mã tòa không được vượt quá 10 ký tự")]
        public string NewBuildingCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Danh sách tầng là bắt buộc")]
        [MinLength(1, ErrorMessage = "Phải chọn ít nhất 1 tầng")]
        public List<int> FloorNumbers { get; set; } = new List<int>();

        [MaxLength(20, ErrorMessage = "Mã cũ không được vượt quá 20 ký tự")]
        public string? OldPrefix { get; set; }
    }

    public class RefactorApartmentNamesResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<ApartmentUpdateResult> UpdatedApartments { get; set; } = new List<ApartmentUpdateResult>();
        public int TotalUpdated { get; set; }
        public List<int> ProcessedFloors { get; set; } = new List<int>();
        public List<int> SkippedFloors { get; set; } = new List<int>();
    }

    public class ApartmentUpdateResult
    {
        public Guid ApartmentId { get; set; }
        public string OldNumber { get; set; } = string.Empty;
        public string NewNumber { get; set; } = string.Empty;
        public int FloorNumber { get; set; }
        public bool Updated { get; set; }
        public string? ErrorMessage { get; set; }
    }
}