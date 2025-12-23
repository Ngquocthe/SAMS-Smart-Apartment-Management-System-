using System;
using System.ComponentModel.DataAnnotations;
using static SAMS_BE.Helpers.DateTimeHelper;

namespace SAMS_BE.DTOs
{
    public class CreateAnnouncementDto
    {
        [Required(ErrorMessage = "Tiêu đề là bắt buộc")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "Tiêu đề phải từ 5 đến 100 ký tự")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nội dung là bắt buộc")]
        [StringLength(5000, MinimumLength = 10, ErrorMessage = "Nội dung phải từ 10 đến 5000 ký tự")]
        public string Content { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngày hiển thị từ là bắt buộc")]
        public DateTime VisibleFrom { get; set; }

        public DateTime? VisibleTo { get; set; }

        [StringLength(255)]
        public string? VisibilityScope { get; set; } // "ALL", "RESIDENTS", "STAFF", etc.

        [StringLength(32)]
        public string Status { get; set; } = "ACTIVE";

        // New fields
        public bool IsPinned { get; set; } = false;

        [StringLength(50)]
        public string? Type { get; set; }
    }

    public class UpdateAnnouncementDto
    {
        [Required(ErrorMessage = "ID thông báo là bắt buộc")]
        public Guid AnnouncementId { get; set; }

        [Required(ErrorMessage = "Tiêu đề là bắt buộc")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "Tiêu đề phải từ 5 đến 100 ký tự")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nội dung là bắt buộc")]
        [StringLength(5000, MinimumLength = 10, ErrorMessage = "Nội dung phải từ 10 đến 5000 ký tự")]
        public string Content { get; set; } = string.Empty;

        public DateTime VisibleFrom { get; set; }

        public DateTime? VisibleTo { get; set; }

        [StringLength(255)]
        public string? VisibilityScope { get; set; }

        [StringLength(32)]
        public string Status { get; set; } = "ACTIVE";

        // New fields
        public bool IsPinned { get; set; } = false;

        [StringLength(50)]
        public string? Type { get; set; }
    }

    public class AnnouncementResponseDto
    {
        public Guid AnnouncementId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime VisibleFrom { get; set; }
        public DateTime? VisibleTo { get; set; }
        public string? VisibilityScope { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsPinned { get; set; }
        public string? Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsActive { get; set; } // True if current date is between VisibleFrom and VisibleTo
        public bool IsRead { get; set; } // True if user has read this announcement
        public Guid? ScheduleId { get; set; } // Link to maintenance schedule if applicable
        public Guid? BookingId { get; set; } // Link to amenity booking if applicable
    }

    public class AnnouncementListResponseDto
    {
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public List<AnnouncementResponseDto> Announcements { get; set; } = new();
    }
}
