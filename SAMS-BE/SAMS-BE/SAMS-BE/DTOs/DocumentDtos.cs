using SAMS_BE.Enums;
using SAMS_BE.Constants;
using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs
{
    public class CreateDocumentDto
    {
        [Required(ErrorMessage = "Category là bắt buộc")]
        [RegularExpression("^(Administrative|Financial|Technical|Legal|Resident)$",
            ErrorMessage = "Category phải là một trong: Administrative, Financial, Technical, Legal, Resident")]
        public string Category { get; set; } = null!;

        [Required(ErrorMessage = "Title là bắt buộc")]
        [MinLength(3, ErrorMessage = "Title phải có ít nhất 3 ký tự")]
        [StringLength(255, ErrorMessage = "Title không được vượt quá 255 ký tự")]
        public string Title { get; set; } = null!;

        [StringLength(120, ErrorMessage = "VisibilityScope không được vượt quá 120 ký tự")]
        [RegularExpression("^(Public|Accounting|Receptionist|Resident)$",
            ErrorMessage = "VisibilityScope phải là một trong: Public, Accounting, Receptionist, Resident")]
        public string? VisibilityScope { get; set; }

        [StringLength(190, ErrorMessage = "CreatedBy không được vượt quá 190 ký tự")]
        public string? CreatedBy { get; set; }
    }

    public class DocumentQueryDto
    {
        public string? Title { get; set; }
        public string? Category { get; set; }
        public string? Status { get; set; }
        public string? VisibilityScope { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class ChangeDocumentStatusDto
    {
        public string Status { get; set; } = null!;
        public string? Detail { get; set; }
        public Guid? ActorId { get; set; }
    }

    public class UpdateDocumentDto
    {
        [MinLength(3, ErrorMessage = "Title phải có ít nhất 3 ký tự")]
        [StringLength(255, ErrorMessage = "Title không được vượt quá 255 ký tự")]
        public string? Title { get; set; }

        [RegularExpression("^(Administrative|Financial|Technical|Legal|Resident|Vendor)$",
            ErrorMessage = "Category phải là một trong: Administrative, Financial, Technical, Legal, Resident, Vendor")]
        public string? Category { get; set; }

        [StringLength(120, ErrorMessage = "VisibilityScope không được vượt quá 120 ký tự")]
        [RegularExpression("^(Public|Accounting|Receptionist|Resident)$",
            ErrorMessage = "VisibilityScope phải là một trong: Public, Accounting, Receptionist, Resident")]
        public string? VisibilityScope { get; set; }
    }

    public class LatestDocumentDto
    {
        public Guid DocumentId { get; set; }
        public string Category { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string? VisibilityScope { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }

        public int LatestVersionNo { get; set; }
        public int? CurrentVersion { get; set; }
        public Guid FileId { get; set; }
        public string? VersionNote { get; set; }
        public DateTime ChangedAt { get; set; }
        public string OriginalFileName { get; set; } = null!;
        public string MimeType { get; set; } = null!;
    }

    public class ResidentDocumentDto
    {
        public Guid DocumentId { get; set; }
        public string Category { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string? VisibilityScope { get; set; }
        public DateTime ChangedAt { get; set; }
        public string? CreatedBy { get; set; }
        public int LatestVersionNo { get; set; }
        public int? CurrentVersion { get; set; }
        public Guid FileId { get; set; }
        public string? VersionNote { get; set; }
        public string OriginalFileName { get; set; } = null!;
        public string MimeType { get; set; } = null!;
    }

    public class DocumentCategoryDto
    {
        public string Value { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
    }

    public class RequestRestoreDto
    {
        public string? Reason { get; set; }
    }
}