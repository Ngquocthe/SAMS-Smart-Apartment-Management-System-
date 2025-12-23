using System.ComponentModel.DataAnnotations;
using Xunit.Sdk;

namespace SAMS_BE.DTOs.Request.Staff
{
    public sealed class StaffUpdateDto
    {

        [Required(ErrorMessage = "Phone is required")]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "FirstName is required")]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "LastName is required")]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        [MaxLength(255)]
        public string Address { get; set; } = string.Empty;

        public DateTime? Dob { get; set; }

        public string? CurrentAddress { get; set; }

        [Required(ErrorMessage = "RoleId is required")]
        public string RoleId { get; set; } = string.Empty;

        [Required(ErrorMessage = "At least one access role is required")]
        public List<string> AccessRoles { get; set; } = new();

        [Required(ErrorMessage = "HireDate is required")]
        public DateTime HireDate { get; set; }

        [Required(ErrorMessage = "BaseSalary is required")]
        [Range(0, double.MaxValue, ErrorMessage = "BaseSalary must be >= 0")]
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

        public string Currency { get; set; } = "VND";

        // ===== File =====
        public IFormFile? Avatar { get; set; }
        public IFormFile? CardPhoto { get; set; }
    }
}
