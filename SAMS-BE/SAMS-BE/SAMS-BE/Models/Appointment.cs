using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("appointments", Schema = "building")]
public partial class Appointment
{
    [Key]
    [Column("appointment_id")]
    public Guid AppointmentId { get; set; }

    [Column("ticket_id")]
    public Guid? TicketId { get; set; }

    [Column("apartment_id")]
    public Guid ApartmentId { get; set; }

    [Column("start_at")]
    [Precision(3)]
    public DateTime StartAt { get; set; }

    [Column("end_at")]
    [Precision(3)]
    public DateTime EndAt { get; set; }

    [Column("location")]
    [StringLength(255)]
    public string? Location { get; set; }

    [Column("created_at")]
    [Precision(3)]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("ApartmentId")]
    [InverseProperty("Appointments")]
    public virtual Apartment Apartment { get; set; } = null!;

    [ForeignKey("TicketId")]
    [InverseProperty("Appointments")]
    public virtual Ticket? Ticket { get; set; }
}
