using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("staff_profiles", Schema = "building")]
public partial class StaffProfile
{
    [Key]
    [Column("staff_code")]
    public Guid StaffCode { get; set; }

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("hire_date")]
    public DateOnly? HireDate { get; set; }

    [Column("termination_date")]
    public DateOnly? TerminationDate { get; set; }

    [Column("notes")]
    [StringLength(1000)]
    public string? Notes { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("current_address")]
    [StringLength(250)]
    public string? CurrentAddress { get; set; }

    [Column("emergency_contact_name")]
    [StringLength(150)]
    public string? EmergencyContactName { get; set; }

    [Column("emergency_contact_phone")]
    [StringLength(20)]
    public string? EmergencyContactPhone { get; set; }

    [Column("emergency_contact_relation")]
    [StringLength(50)]
    public string? EmergencyContactRelation { get; set; }

    [Column("bank_account_no")]
    [StringLength(50)]
    public string? BankAccountNo { get; set; }

    [Column("bank_name")]
    [StringLength(100)]
    public string? BankName { get; set; }


    [Column("base_salary", TypeName = "decimal(18,2)")]
    public decimal? BaseSalary { get; set; }

    [Column("tax_code")]
    [StringLength(50)]
    public string? TaxCode { get; set; }

    [Column("social_insurance_no")]
    [StringLength(50)]
    public string? SocialInsuranceNo { get; set; }

    // Ảnh thẻ
    [Column("card_photo_url")]
    [StringLength(300)]
    public string? CardPhotoUrl { get; set; }

    [Column("role_id")]
    public Guid RoleId { get; set; }

    [ForeignKey(nameof(RoleId))]
    public WorkRole Role { get; set; } = null!;

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<JournalEntry> JournalEntryCreatedByNavigations { get; set; } = new List<JournalEntry>();

    [InverseProperty("PostedByNavigation")]
    public virtual ICollection<JournalEntry> JournalEntryPostedByNavigations { get; set; } = new List<JournalEntry>();

    [InverseProperty("ApprovedByNavigation")]
    public virtual ICollection<ServicePrice> ServicePriceApprovedByNavigations { get; set; } = new List<ServicePrice>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<ServicePrice> ServicePriceCreatedByNavigations { get; set; } = new List<ServicePrice>();

    [ForeignKey("UserId")]
    [InverseProperty("StaffProfiles")]
    public virtual User? User { get; set; }

    [InverseProperty("ApprovedByNavigation")]
    public virtual ICollection<Voucher> VoucherApprovedByNavigations { get; set; } = new List<Voucher>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<Voucher> VoucherCreatedByNavigations { get; set; } = new List<Voucher>();
}
