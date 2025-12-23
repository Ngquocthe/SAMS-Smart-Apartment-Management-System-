using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("tickets", Schema = "building")]
[Index("Status", "Priority", "CreatedAt", Name = "IX_tickets_status_priority")]
public partial class Ticket
{
    [Key]
    [Column("ticket_id")]
    public Guid TicketId { get; set; }

    [Column("created_by_user_id")]
    public Guid? CreatedByUserId { get; set; }

    [Column("category")]
    [StringLength(64)]
    public string Category { get; set; } = null!;

    [Column("priority")]
    [StringLength(32)]
    public string? Priority { get; set; }

    [Column("subject")]
    [StringLength(255)]
    public string Subject { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("status")]
    [StringLength(32)]
    public string Status { get; set; } = null!;

    [Column("expected_completion_at")]
    [Precision(3)]
    public DateTime? ExpectedCompletionAt { get; set; }

    [Column("created_at")]
    [Precision(3)]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    [Precision(3)]
    public DateTime? UpdatedAt { get; set; }

    [Column("closed_at")]
    [Precision(3)]
    public DateTime? ClosedAt { get; set; }

    [Column("scope")]
    [StringLength(64)]
    public string? Scope { get; set; }

    [Column("apartment_id")]
    public Guid? ApartmentId { get; set; }

    [Column("has_invoice")]
    public bool HasInvoice { get; set; }

    [Column("vehicle_id")]
    public Guid? VehicleId { get; set; }

    [ForeignKey("ApartmentId")]
    [InverseProperty("Tickets")]
    public virtual Apartment? Apartment { get; set; }

    [InverseProperty("Ticket")]
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    [ForeignKey("CreatedByUserId")]
    [InverseProperty("TicketCreatedByUsers")]
    public virtual User? CreatedByUser { get; set; }

    [InverseProperty("Request")]
    public virtual ICollection<MaintenanceApartmentHistory> MaintenanceApartmentHistories { get; set; } = new List<MaintenanceApartmentHistory>();

    [InverseProperty("Ticket")]
    public virtual ICollection<TicketComment> TicketComments { get; set; } = new List<TicketComment>();

    [InverseProperty("Ticket")]
    public virtual ICollection<TicketAttachment> TicketAttachments { get; set; } = new List<TicketAttachment>();

    [InverseProperty("Ticket")]
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    [InverseProperty("Ticket")]
    public virtual ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();

    [ForeignKey("VehicleId")]
    public virtual Vehicle? Vehicle { get; set; }
}

