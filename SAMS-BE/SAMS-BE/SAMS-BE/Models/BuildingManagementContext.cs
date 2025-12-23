using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components.Sections;
using Microsoft.EntityFrameworkCore;
using SAMS_BE.Tenant;

namespace SAMS_BE.Models;

public partial class BuildingManagementContext : DbContext
{
    private readonly ITenantContextAccessor _tenant;

    public BuildingManagementContext()
    {
    }

    public BuildingManagementContext(DbContextOptions<BuildingManagementContext> options, ITenantContextAccessor tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    public string TenantSchema => string.IsNullOrWhiteSpace(_tenant?.Schema) ? "building" : _tenant.Schema;

    public virtual DbSet<AccessCard> AccessCards { get; set; }

    public virtual DbSet<AccessCardType> AccessCardTypes { get; set; }

    public virtual DbSet<AccessCardCapability> AccessCardCapabilities { get; set; }

    public virtual DbSet<CardHistory> CardHistories { get; set; }


    public virtual DbSet<Amenity> Amenities { get; set; }

    public virtual DbSet<AmenityPackage> AmenityPackages { get; set; }

    public virtual DbSet<AmenityBooking> AmenityBookings { get; set; }

    public virtual DbSet<AmenityCheckIn> AmenityCheckIns { get; set; }

    public virtual DbSet<Announcement> Announcements { get; set; }

    public virtual DbSet<AnnouncementRead> AnnouncementReads { get; set; }

    public virtual DbSet<Apartment> Apartments { get; set; }

    public virtual DbSet<ApartmentService> ApartmentServices { get; set; }

    public virtual DbSet<Appointment> Appointments { get; set; }

    public virtual DbSet<Asset> Assets { get; set; }

    public virtual DbSet<AssetCategory> AssetCategories { get; set; }

    public virtual DbSet<AssetMaintenanceHistory> AssetMaintenanceHistories { get; set; }

    public virtual DbSet<AssetMaintenanceSchedule> AssetMaintenanceSchedules { get; set; }

    public virtual DbSet<Document> Documents { get; set; }

    public virtual DbSet<DocumentActionLog> DocumentActionLogs { get; set; }

    public virtual DbSet<DocumentVersion> DocumentVersions { get; set; }

    public virtual DbSet<File> Files { get; set; }

    public virtual DbSet<TicketAttachment> TicketAttachments { get; set; }

    public virtual DbSet<Floor> Floors { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<InvoiceDetail> InvoiceDetails { get; set; }

    public virtual DbSet<JournalEntry> JournalEntries { get; set; }

    public virtual DbSet<JournalEntryLine> JournalEntryLines { get; set; }

    public virtual DbSet<MaintenanceApartmentHistory> MaintenanceApartmentHistories { get; set; }

    public virtual DbSet<Meter> Meters { get; set; }

    public virtual DbSet<MeterReading> MeterReadings { get; set; }

    public virtual DbSet<ParkingEntry> ParkingEntries { get; set; }

    public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }

    public virtual DbSet<Receipt> Receipts { get; set; }

    public virtual DbSet<ResidentApartment> ResidentApartments { get; set; }

    public virtual DbSet<ResidentProfile> ResidentProfiles { get; set; }

    public virtual DbSet<ServicePrice> ServicePrices { get; set; }

    public virtual DbSet<ServiceTypeCategory> ServiceTypeCategories { get; set; }

    public virtual DbSet<ServiceType> ServiceTypes { get; set; }

    public virtual DbSet<StaffProfile> StaffProfiles { get; set; }

    public virtual DbSet<Ticket> Tickets { get; set; }

    public virtual DbSet<TicketComment> TicketComments { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Vehicle> Vehicles { get; set; }

    public virtual DbSet<VehicleType> VehicleTypes { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    public virtual DbSet<VoucherItem> VoucherItems { get; set; }

    public virtual DbSet<WorkRole> WorkRoles { get; set; }

    public virtual DbSet<Contract> Contracts { get; set; }
    public virtual DbSet<ContractDocument> ContractDocuments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var schema = TenantSchema;

        modelBuilder.HasDefaultSchema(schema);

        modelBuilder.Entity<WorkRole>(entity =>
        {
            entity.HasKey(e => e.RoleId);
            entity.HasIndex(e => e.RoleKey).IsUnique();
            entity.Property(e => e.RoleKey).HasMaxLength(100).IsRequired();
            entity.Property(e => e.RoleName).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<AccessCard>(entity =>
        {
            entity.HasKey(e => e.CardId);

            entity.ToTable("access_cards", schema);

            entity.HasIndex(e => e.CardNumber, "UQ_access_cards_number").IsUnique();

            entity.Property(e => e.CardId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("card_id");
            entity.Property(e => e.CardNumber)
                .HasMaxLength(128)
                .HasColumnName("card_number");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(190)
                .HasColumnName("created_by");
            entity.Property(e => e.ExpiredDate)
                .HasPrecision(3)
                .HasColumnName("expired_date");
            entity.Property(e => e.IssuedDate)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("issued_date");
            entity.Property(e => e.IssuedToApartmentId).HasColumnName("issued_to_apartment_id");
            entity.Property(e => e.IssuedToUserId).HasColumnName("issued_to_user_id");
            entity.Property(e => e.IsDelete)
                .HasDefaultValue(false)
                .HasColumnName("is_delete");
            entity.Property(e => e.Status)
                .HasMaxLength(32)
                .HasDefaultValue("PENDING_APPROVAL")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(190)
                .HasColumnName("updated_by");


            entity.HasOne(d => d.IssuedToApartment).WithMany(p => p.AccessCards)
                .HasForeignKey(d => d.IssuedToApartmentId)
                .HasConstraintName("FK_access_cards_apartment");

            entity.HasOne(d => d.IssuedToUser).WithMany(p => p.AccessCards)
                .HasForeignKey(d => d.IssuedToUserId)
                .HasConstraintName("FK_access_cards_user");
        });

        modelBuilder.Entity<AccessCardType>(entity =>
        {
            entity.HasKey(e => e.CardTypeId);

            entity.ToTable("access_card_types", schema);

            entity.HasIndex(e => e.Code, "UQ_access_card_types_code").IsUnique();

            entity.Property(e => e.CardTypeId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("card_type_id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsDelete)
                .HasDefaultValue(false)
                .HasColumnName("is_delete");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasColumnName("updated_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(190)
                .HasColumnName("created_by");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(190)
                .HasColumnName("updated_by");
        });

        modelBuilder.Entity<Contract>(entity =>
        {
            entity.ToTable("contracts", schema);

            entity.HasKey(e => e.ContractId);

            entity.Property(e => e.ContractId)
                  .HasDefaultValueSql("(newid())");

            entity.Property(e => e.Status)
                  .HasMaxLength(32)
                  .HasDefaultValue("ACTIVE");

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(e => e.Apartment)
                  .WithMany() // Apartment không cần navigation ngược
                  .HasForeignKey(e => e.ApartmentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        /* ================= CONTRACT DOCUMENT ================= */

        modelBuilder.Entity<ContractDocument>(entity =>
        {
            entity.ToTable("contract_documents", schema);

            entity.HasKey(e => e.ContractDocumentId);

            entity.Property(e => e.ContractDocumentId)
                  .HasDefaultValueSql("(newid())");

            entity.Property(e => e.DocumentType)
                  .HasMaxLength(32)
                  .HasDefaultValue("CONTRACT");

            entity.HasIndex(e => new { e.ContractId, e.DocumentId })
                  .IsUnique();

            entity.HasOne(e => e.Contract)
                  .WithMany(c => c.ContractDocuments)
                  .HasForeignKey(e => e.ContractId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Document)
                  .WithMany() // Document không cần biết ngược
                  .HasForeignKey(e => e.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Amenity>(entity =>
        {
            entity.HasKey(e => e.AmenityId);

            entity.ToTable("amenities", schema);

            entity.HasIndex(e => e.Code, "UQ_amenities_code").IsUnique();

            entity.Property(e => e.AmenityId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("amenity_id");
            entity.Property(e => e.AssetId)
                .HasColumnName("asset_id");
            entity.Property(e => e.Code)
                .HasMaxLength(64)
                .HasColumnName("code");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(100)
                .HasColumnName("category_name");
            entity.Property(e => e.Location)
                .HasMaxLength(255)
                .HasColumnName("location");
            entity.Property(e => e.HasMonthlyPackage)
                .HasDefaultValue(true)
                .HasColumnName("has_monthly_package");
            entity.Property(e => e.FeeType)
                .HasMaxLength(20)
                .HasDefaultValue("Paid")
                .HasColumnName("fee_type");
            entity.Property(e => e.Status)
                .HasMaxLength(32)
                .HasDefaultValue("ACTIVE")
                .HasColumnName("status");
            entity.Property(e => e.IsDelete)
                .HasDefaultValue(false)
                .HasColumnName("is_delete");

            entity.HasOne(d => d.Asset)
                .WithMany(p => p.Amenities)
                .HasForeignKey(d => d.AssetId)
                .HasConstraintName("FK_amenities_asset");
        });

        modelBuilder.Entity<AmenityPackage>(entity =>
        {
            entity.HasKey(e => e.PackageId);

            entity.ToTable("amenity_packages", schema);

            entity.Property(e => e.PackageId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("package_id");
            entity.Property(e => e.AmenityId)
                .HasColumnName("amenity_id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.MonthCount)
                .HasColumnName("month_count");
            entity.Property(e => e.DurationDays)
                .HasColumnName("duration_days");
            entity.Property(e => e.PeriodUnit)
                .HasMaxLength(10)
                .HasColumnName("period_unit");
            entity.Property(e => e.Price)
                .HasColumnName("price");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.Status)
                .HasMaxLength(32)
                .HasDefaultValue("ACTIVE")
                .HasColumnName("status");

            entity.HasOne(d => d.Amenity)
                .WithMany(p => p.AmenityPackages)
                .HasForeignKey(d => d.AmenityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ap_amenity");
        });

        modelBuilder.Entity<AmenityBooking>(entity =>
        {
            entity.HasKey(e => e.BookingId);

            entity.ToTable("amenity_bookings", schema);

            entity.Property(e => e.BookingId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("booking_id");
            entity.Property(e => e.AmenityId)
                .HasColumnName("amenity_id");
            entity.Property(e => e.PackageId)
                .HasColumnName("package_id");
            entity.Property(e => e.ApartmentId)
                .HasColumnName("apartment_id");
            entity.Property(e => e.UserId)
                .HasColumnName("user_id");
            entity.Property(e => e.StartDate)
                .HasColumnName("start_date");
            entity.Property(e => e.EndDate)
                .HasColumnName("end_date");
            entity.Property(e => e.Price)
                .HasColumnName("price");
            entity.Property(e => e.TotalPrice)
                .HasColumnName("total_price");
            entity.Property(e => e.Status)
                .HasMaxLength(32)
                .HasDefaultValue("Pending")
                .HasColumnName("status");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(32)
                .HasDefaultValue("Unpaid")
                .HasColumnName("payment_status");
            entity.Property(e => e.Notes)
                .HasMaxLength(1000)
                .HasColumnName("notes");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(190)
                .HasColumnName("created_by");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(190)
                .HasColumnName("updated_by");
            entity.Property(e => e.IsDelete)
                .HasDefaultValue(false)
                .HasColumnName("is_delete");

            entity.HasOne(d => d.Amenity)
                .WithMany(p => p.AmenityBookings)
                .HasForeignKey(d => d.AmenityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ab_amenity");

            entity.HasOne(d => d.Package)
                .WithMany(p => p.AmenityBookings)
                .HasForeignKey(d => d.PackageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ab_package");

            entity.HasOne(d => d.Apartment)
                .WithMany(p => p.AmenityBookings)
                .HasForeignKey(d => d.ApartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ab_apartment");

            entity.HasOne(d => d.User)
                .WithMany(p => p.AmenityBookings)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_ab_user");
        });

        modelBuilder.Entity<AmenityCheckIn>(entity =>
        {
            entity.HasKey(e => e.CheckInId);

            entity.ToTable("amenity_check_ins", schema);

            entity.Property(e => e.CheckInId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("check_in_id");
            entity.Property(e => e.BookingId)
                .HasColumnName("booking_id");
            entity.Property(e => e.CheckedInForUserId)
                .HasColumnName("checked_in_for_user_id");
            entity.Property(e => e.CheckedInByUserId)
                .HasColumnName("checked_in_by_user_id");
            entity.Property(e => e.Similarity)
                .HasColumnName("similarity");
            entity.Property(e => e.IsSuccess)
                .HasDefaultValue(true)
                .HasColumnName("is_success");
            entity.Property(e => e.ResultStatus)
                .HasMaxLength(32)
                .HasDefaultValue("Success")
                .HasColumnName("result_status");
            entity.Property(e => e.Message)
                .HasMaxLength(500)
                .HasColumnName("message");
            entity.Property(e => e.CapturedImageUrl)
                .HasMaxLength(500)
                .HasColumnName("captured_image_url");
            entity.Property(e => e.IsManualOverride)
                .HasDefaultValue(false)
                .HasColumnName("is_manual_override");
            entity.Property(e => e.CheckedInAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("checked_in_at");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(190)
                .HasColumnName("created_by");

            entity.HasOne(d => d.Booking)
                .WithMany(p => p.AmenityCheckIns)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_amenity_check_ins_booking");

            entity.HasOne(d => d.CheckedInForUser)
                .WithMany(p => p.AmenityCheckInsAsTarget)
                .HasForeignKey(d => d.CheckedInForUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_amenity_check_ins_checked_for");

            entity.HasOne(d => d.CheckedInByUser)
                .WithMany(p => p.AmenityCheckInsHandled)
                .HasForeignKey(d => d.CheckedInByUserId)
                .HasConstraintName("FK_amenity_check_ins_checked_by");
        });

        modelBuilder.Entity<Announcement>(entity =>
        {
            entity.ToTable("announcements", schema);

            entity.Property(e => e.AnnouncementId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("announcement_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(190)
                .HasColumnName("created_by");
            entity.Property(e => e.Status)
                .HasMaxLength(32)
                .HasDefaultValue("PUBLISHED")
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(190)
                .HasColumnName("updated_by");
            entity.Property(e => e.VisibilityScope)
                .HasMaxLength(255)
                .HasColumnName("visibility_scope");
            entity.Property(e => e.VisibleFrom)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("visible_from");
            entity.Property(e => e.VisibleTo)
                .HasPrecision(3)
                .HasColumnName("visible_to");
            entity.Property(e => e.IsPinned)
                .HasDefaultValue(false)
                .HasColumnName("is_pinned");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasColumnName("type");
            entity.Property(e => e.ScheduleId).HasColumnName("schedule_id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");

            entity.HasOne(d => d.Schedule).WithMany(p => p.Announcements)
                .HasForeignKey(d => d.ScheduleId)
                .HasConstraintName("FK_announcements_schedule");

            entity.HasOne(d => d.Booking).WithMany(p => p.Announcements)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK_announcements_amenity_booking");
        });

        modelBuilder.Entity<AnnouncementRead>(entity =>
        {
            entity.ToTable("announcement_reads", schema);

            entity.HasIndex(e => e.AnnouncementId, "UQ_announcement_reads").IsUnique();

            entity.Property(e => e.AnnouncementReadId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("announcement_read_id");
            entity.Property(e => e.AnnouncementId).HasColumnName("announcement_id");
            entity.Property(e => e.ReadAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("read_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Announcement).WithOne(p => p.AnnouncementRead)
                .HasForeignKey<AnnouncementRead>(d => d.AnnouncementId)
                .HasConstraintName("FK_ar_announcement");

            entity.HasOne(d => d.User).WithMany(p => p.AnnouncementReads)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ar_user_read");
        });

        modelBuilder.Entity<Apartment>(entity =>
        {
            entity.ToTable("apartments", schema);

            entity.Property(e => e.ApartmentId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("apartment_id");
            entity.Property(e => e.AreaM2)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("area_m2");
            entity.Property(e => e.Bedrooms).HasColumnName("bedrooms");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(190)
                .HasColumnName("created_by");
            entity.Property(e => e.FloorId).HasColumnName("floor_id");
            entity.Property(e => e.Image)
                .HasMaxLength(250)
                .HasColumnName("image");
            entity.Property(e => e.Number)
                .HasMaxLength(64)
                .HasColumnName("number");
            entity.Property(e => e.Status)
                .HasMaxLength(32)
                .HasDefaultValue("ACTIVE")
                .HasColumnName("status");
            entity.Property(e => e.Type)
                .HasMaxLength(100)
                .HasColumnName("type");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(190)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Floor).WithMany(p => p.Apartments)
                .HasForeignKey(d => d.FloorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_apartments_floor");
        });

        modelBuilder.Entity<ApartmentService>(entity =>
        {
            entity.ToTable("apartment_services", schema);

            entity.HasIndex(e => new { e.ApartmentId, e.ServiceId, e.StartDate }, "UQ_apartment_services").IsUnique();

            entity.Property(e => e.ApartmentServiceId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("apartment_service_id");
            entity.Property(e => e.ApartmentId).HasColumnName("apartment_id");
            entity.Property(e => e.BillingCycle)
                .HasMaxLength(16)
                .HasDefaultValue("MONTHLY")
                .HasColumnName("billing_cycle");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.Meta).HasColumnName("meta");
            entity.Property(e => e.Quantity)
                .HasColumnType("decimal(18, 4)")
                .HasColumnName("quantity");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Status)
                .HasMaxLength(32)
                .HasDefaultValue("ACTIVE")
                .HasColumnName("status");

            entity.HasOne(d => d.Apartment).WithMany(p => p.ApartmentServices)
                .HasForeignKey(d => d.ApartmentId)
                .HasConstraintName("FK_as_apartment");

            entity.HasOne(d => d.Service).WithMany(p => p.ApartmentServices)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_as_service");
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.ToTable("appointments", schema);

            entity.Property(e => e.AppointmentId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("appointment_id");
            entity.Property(e => e.ApartmentId).HasColumnName("apartment_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.EndAt)
                .HasPrecision(3)
                .HasColumnName("end_at");
            entity.Property(e => e.Location)
                .HasMaxLength(255)
                .HasColumnName("location");
            entity.Property(e => e.StartAt)
                .HasPrecision(3)
                .HasColumnName("start_at");
            entity.Property(e => e.TicketId).HasColumnName("ticket_id");

            entity.HasOne(d => d.Apartment).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.ApartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_appointments_apartment");

            entity.HasOne(d => d.Ticket).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.TicketId)
                .HasConstraintName("FK_appointments_ticket");
        });

        modelBuilder.Entity<Asset>(entity =>
        {
            entity.ToTable("assets", schema);

            entity.HasIndex(e => e.Code, "UQ_assets_code").IsUnique();

            entity.Property(e => e.AssetId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("asset_id");
            entity.Property(e => e.ApartmentId).HasColumnName("apartment_id");
            entity.Property(e => e.BlockId).HasColumnName("block_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Code)
                .HasMaxLength(64)
                .HasColumnName("code");
            entity.Property(e => e.Location)
                .HasMaxLength(255)
                .HasColumnName("location");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.PurchaseDate).HasColumnName("purchase_date");
            entity.Property(e => e.Status)
                .HasMaxLength(32)
                .HasDefaultValue("ACTIVE")
                .HasColumnName("status");
            entity.Property(e => e.WarrantyExpire).HasColumnName("warranty_expire");
            entity.Property(e => e.MaintenanceFrequency).HasColumnName("maintenance_frequency");
            entity.Property(e => e.IsDelete)
                .HasDefaultValue(false)
                .HasColumnName("is_delete");

            entity.HasOne(d => d.Apartment).WithMany(p => p.Assets)
                .HasForeignKey(d => d.ApartmentId)
                .HasConstraintName("FK_assets_apartment");

            entity.HasOne(d => d.Category).WithMany(p => p.Assets)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_assets_category");
        });

        modelBuilder.Entity<AssetCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId);

            entity.ToTable("asset_categories", schema);

            entity.HasIndex(e => e.Code, "UQ_asset_categories_code").IsUnique();

            entity.Property(e => e.CategoryId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("category_id");
            entity.Property(e => e.Code)
                .HasMaxLength(64)
                .HasColumnName("code");
            entity.Property(e => e.Description)
                .HasMaxLength(1000)
                .HasColumnName("description");
            entity.Property(e => e.MaintenanceFrequency).HasColumnName("maintenance_frequency");
            entity.Property(e => e.DefaultReminderDays)
                .HasDefaultValue(3)
                .HasColumnName("default_reminder_days");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
        });

        modelBuilder.Entity<AssetMaintenanceHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId);

            entity.ToTable("asset_maintenance_history", schema);

            entity.Property(e => e.HistoryId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("history_id");
            entity.Property(e => e.Action)
                .HasMaxLength(255)
                .HasColumnName("action");
            entity.Property(e => e.ActionDate)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("action_date");
            entity.Property(e => e.AssetId).HasColumnName("asset_id");
            entity.Property(e => e.ScheduleId).HasColumnName("schedule_id");
            entity.Property(e => e.CostAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("cost_amount");
            entity.Property(e => e.NextDueDate).HasColumnName("next_due_date");
            entity.Property(e => e.Notes)
                .HasMaxLength(1000)
                .HasColumnName("notes");

            entity.HasOne(d => d.Asset).WithMany(p => p.AssetMaintenanceHistories)
                .HasForeignKey(d => d.AssetId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_amh_asset");

            entity.HasOne(d => d.Schedule).WithMany(p => p.AssetMaintenanceHistories)
                .HasForeignKey(d => d.ScheduleId)
                .HasConstraintName("FK_amh_schedule");
        });

        modelBuilder.Entity<AssetMaintenanceSchedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId);

            entity.ToTable("asset_maintenance_schedule", schema);

            entity.Property(e => e.ScheduleId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("schedule_id");
            entity.Property(e => e.AssetId).HasColumnName("asset_id");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.StartTime)
                .HasConversion(
                    v => v.HasValue ? v.Value.ToTimeSpan() : (TimeSpan?)null,
                    v => v.HasValue ? TimeOnly.FromTimeSpan(v.Value) : null)
                .HasColumnName("start_time");
            entity.Property(e => e.EndTime)
                .HasConversion(
                    v => v.HasValue ? v.Value.ToTimeSpan() : (TimeSpan?)null,
                    v => v.HasValue ? TimeOnly.FromTimeSpan(v.Value) : null)
                .HasColumnName("end_time");
            entity.Property(e => e.ReminderDays)
                .HasDefaultValue(3)
                .HasColumnName("reminder_days");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Status)
                .HasMaxLength(32)
                .HasDefaultValue("SCHEDULED")
                .HasColumnName("status");
            entity.Property(e => e.RecurrenceType)
                .HasMaxLength(32)
                .HasColumnName("recurrence_type");
            entity.Property(e => e.RecurrenceInterval).HasColumnName("recurrence_interval");

            entity.HasOne(d => d.Asset).WithMany(p => p.AssetMaintenanceSchedules)
                .HasForeignKey(d => d.AssetId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ams_asset");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.AssetMaintenanceSchedules)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_ams_created_by");
        });

        modelBuilder.Entity<CardHistory>(entity =>
        {
            entity.ToTable("card_history", schema);

            entity.Property(e => e.CardHistoryId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("card_history_id");
            entity.Property(e => e.CardId).HasColumnName("card_id");
            entity.Property(e => e.CardTypeId).HasColumnName("card_type_id");
            entity.Property(e => e.EventCode)
                .HasMaxLength(64)
                .HasColumnName("event_code");
            entity.Property(e => e.EventTimeUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(DATEADD(HOUR, 7, SYSUTCDATETIME()))")
                .HasColumnName("event_time_utc");
            entity.Property(e => e.FieldName)
                .HasMaxLength(128)
                .HasColumnName("field_name");
            entity.Property(e => e.OldValue)
                .HasMaxLength(500)
                .HasColumnName("old_value");
            entity.Property(e => e.NewValue)
                .HasMaxLength(500)
                .HasColumnName("new_value");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.ValidFrom)
                .HasPrecision(3)
                .HasColumnName("valid_from");
            entity.Property(e => e.ValidTo)
                .HasPrecision(3)
                .HasColumnName("valid_to");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(190)
                .HasColumnName("created_by");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasColumnName("created_at");
            entity.Property(e => e.IsDelete)
                .HasColumnName("is_delete");

            entity.HasOne(d => d.Card).WithMany(p => p.CardHistories)
                .HasForeignKey(d => d.CardId)
                .HasConstraintName("FK_card_history_card");
        });


        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("documents", schema);

            entity.Property(e => e.DocumentId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("document_id");
            entity.Property(e => e.Category)
                .HasMaxLength(64)
                .HasColumnName("category");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(190)
                .HasColumnName("created_by");
            entity.Property(e => e.Status)
                .HasMaxLength(32)
                .HasDefaultValue("PENDING_APPROVAL")
                .HasColumnName("status");
            entity.Property(e => e.CurrentVersion)
                .HasColumnName("current_version");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.VisibilityScope)
                .HasMaxLength(120)
                .HasColumnName("visibility_scope");
            entity.Property(e => e.IsDelete)
                .HasDefaultValue(false)
                .HasColumnName("is_delete");
        });

        modelBuilder.Entity<DocumentActionLog>(entity =>
        {
            entity.HasKey(e => e.ActionLogId);

            entity.ToTable("document_action_log", schema);

            entity.Property(e => e.ActionLogId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("action_log_id");
            entity.Property(e => e.Action)
                .HasMaxLength(64)
                .HasColumnName("action");
            entity.Property(e => e.ActionAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("action_at");
            entity.Property(e => e.ActorId).HasColumnName("actor_id");
            entity.Property(e => e.Detail)
                .HasMaxLength(1000)
                .HasColumnName("detail");
            entity.Property(e => e.DocumentId).HasColumnName("document_id");

            entity.HasOne(d => d.Document).WithMany(p => p.DocumentActionLogs)
                .HasForeignKey(d => d.DocumentId)
                .HasConstraintName("FK_dal_document");
        });

        modelBuilder.Entity<DocumentVersion>(entity =>
        {
            entity.ToTable("document_versions", schema);

            entity.HasIndex(e => new { e.DocumentId, e.VersionNo }, "UQ_document_versions").IsUnique();

            entity.Property(e => e.DocumentVersionId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("document_version_id");
            entity.Property(e => e.ChangedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("changed_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(190)
                .HasColumnName("created_by");
            entity.Property(e => e.DocumentId).HasColumnName("document_id");
            entity.Property(e => e.FileId).HasColumnName("file_id");
            entity.Property(e => e.Note)
                .HasMaxLength(500)
                .HasColumnName("note");
            entity.Property(e => e.VersionNo).HasColumnName("version_no");

            entity.HasOne(d => d.Document).WithMany(p => p.DocumentVersions)
                .HasForeignKey(d => d.DocumentId)
                .HasConstraintName("FK_dv_document");

            entity.HasOne(d => d.File).WithMany(p => p.DocumentVersions)
                .HasForeignKey(d => d.FileId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dv_file");
        });

        modelBuilder.Entity<File>(entity =>
        {
            entity.ToTable("files", schema);

            entity.Property(e => e.FileId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("file_id");
            entity.Property(e => e.MimeType)
                .HasMaxLength(128)
                .HasColumnName("mime_type");
            entity.Property(e => e.OriginalName)
                .HasMaxLength(255)
                .HasColumnName("original_name");
            entity.Property(e => e.StoragePath)
                .HasMaxLength(1000)
                .HasColumnName("storage_path");
            entity.Property(e => e.UploadedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("uploaded_at");
            entity.Property(e => e.UploadedBy)
                .HasMaxLength(190)
                .HasColumnName("uploaded_by");
        });

        modelBuilder.Entity<Floor>(entity =>
        {
            entity.ToTable("floors", schema);

            entity.Property(e => e.FloorId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("floor_id");
            entity.Property(e => e.FloorNumber).HasColumnName("floor_number");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.ToTable("invoices", schema);

            entity.HasIndex(e => new { e.ApartmentId, e.DueDate }, "IX_invoices_apartment_due");

            entity.HasIndex(e => e.InvoiceNo, "UQ_invoices_no").IsUnique();

            entity.Property(e => e.InvoiceId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("invoice_id");
            entity.Property(e => e.ApartmentId).HasColumnName("apartment_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(190)
                .HasColumnName("created_by");
            entity.Property(e => e.DueDate).HasColumnName("due_date");
            entity.Property(e => e.InvoiceNo)
                .HasMaxLength(64)
                .HasColumnName("invoice_no");
            entity.Property(e => e.IssueDate).HasColumnName("issue_date");
            entity.Property(e => e.Note)
                .HasMaxLength(1000)
                .HasColumnName("note");
            entity.Property(e => e.Status)
                .HasMaxLength(32)
                .HasDefaultValue("PENDING")
                .HasColumnName("status");
            entity.Property(e => e.SubtotalAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("subtotal_amount");
            entity.Property(e => e.TaxAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("tax_amount");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("total_amount");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(190)
                .HasColumnName("updated_by");
            entity.Property(e => e.TicketId).HasColumnName("ticket_id");

            entity.HasOne(d => d.Apartment).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.ApartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_invoices_apartment");

            entity.HasOne(d => d.Ticket).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.TicketId)
                .HasConstraintName("FK_invoices_ticket");
        });

        modelBuilder.Entity<InvoiceDetail>(entity =>
        {
            entity.ToTable("invoice_details", schema);

            entity.Property(e => e.InvoiceDetailId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("invoice_detail_id");
            entity.Property(e => e.Amount)
                .HasComputedColumnSql("(round([quantity]*[unit_price],(2)))", true)
                .HasColumnType("decimal(37, 12)")
                .HasColumnName("amount");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.InvoiceId).HasColumnName("invoice_id");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1m)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("quantity");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("unit_price");
            entity.Property(e => e.VatAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("vat_amount");
            entity.Property(e => e.VatRate)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("vat_rate");

            entity.HasOne(d => d.Invoice).WithMany(p => p.InvoiceDetails)
                .HasForeignKey(d => d.InvoiceId)
                .HasConstraintName("FK_invoice_details_invoice");

            entity.HasOne(d => d.Service).WithMany(p => p.InvoiceDetails)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_invoice_details_service");
        });

        modelBuilder.Entity<JournalEntry>(entity =>
        {
            entity.HasKey(e => e.EntryId);

            entity.ToTable("journal_entries", schema);

            entity.HasIndex(e => e.EntryNumber, "UQ_journal_entries_number").IsUnique();

            entity.Property(e => e.EntryId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("entry_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Description)
                .HasMaxLength(1000)
                .HasColumnName("description");
            entity.Property(e => e.EntryType)
                .HasMaxLength(32)
                .HasColumnName("entry_type");
            entity.Property(e => e.EntryDate).HasColumnName("entry_date");
            entity.Property(e => e.EntryNumber)
                .HasMaxLength(64)
                .HasColumnName("entry_number");
            entity.Property(e => e.FiscalPeriod)
                .HasMaxLength(20)
                .HasColumnName("fiscal_period");
            entity.Property(e => e.PostedBy).HasColumnName("posted_by");
            entity.Property(e => e.PostedDate)
                .HasPrecision(3)
                .HasColumnName("posted_date");
            entity.Property(e => e.ReferenceId).HasColumnName("reference_id");
            entity.Property(e => e.ReferenceType)
                .HasMaxLength(32)
                .HasColumnName("reference_type");
            entity.Property(e => e.Status)
                .HasMaxLength(16)
                .HasDefaultValue("DRAFT")
                .HasColumnName("status");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.JournalEntryCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_je_created_by");

            entity.HasOne(d => d.PostedByNavigation).WithMany(p => p.JournalEntryPostedByNavigations)
                .HasForeignKey(d => d.PostedBy)
                .HasConstraintName("FK_je_posted_by");
        });

        modelBuilder.Entity<JournalEntryLine>(entity =>
        {
            entity.HasKey(e => e.LineId);

            entity.ToTable("journal_entry_lines", schema);

            entity.HasIndex(e => new { e.EntryId, e.LineNumber }, "UQ_jel_entry_line").IsUnique();

            entity.Property(e => e.LineId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("line_id");
            entity.Property(e => e.AccountCode)
                .HasMaxLength(64)
                .HasColumnName("account_code");
            entity.Property(e => e.ApartmentId).HasColumnName("apartment_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreditAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("credit_amount");
            entity.Property(e => e.DebitAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("debit_amount");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.EntryId).HasColumnName("entry_id");
            entity.Property(e => e.LineNumber).HasColumnName("line_number");

            entity.HasOne(d => d.Apartment).WithMany(p => p.JournalEntryLines)
                .HasForeignKey(d => d.ApartmentId)
                .HasConstraintName("FK_jel_apartment");

            entity.HasOne(d => d.Entry).WithMany(p => p.JournalEntryLines)
                .HasForeignKey(d => d.EntryId)
                .HasConstraintName("FK_jel_entry");
        });

        modelBuilder.Entity<MaintenanceApartmentHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_mah");

            entity.ToTable("maintenance_apartment_history", schema);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.ApartmentId).HasColumnName("apartment_id");
            entity.Property(e => e.Attachment)
                .HasMaxLength(1000)
                .HasColumnName("attachment");
            entity.Property(e => e.CreationTime)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("creation_time");
            entity.Property(e => e.CreatorUserId).HasColumnName("creator_user_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.HandlerUserId).HasColumnName("handler_user_id");
            entity.Property(e => e.Priority)
                .HasMaxLength(32)
                .HasDefaultValue("NORMAL")
                .HasColumnName("priority");
            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.RequestType)
                .HasMaxLength(64)
                .HasColumnName("request_type");
            entity.Property(e => e.SlaDueTime)
                .HasPrecision(3)
                .HasColumnName("sla_due_time");
            entity.Property(e => e.Status)
                .HasMaxLength(32)
                .HasDefaultValue("OPEN")
                .HasColumnName("status");
            entity.Property(e => e.TargetDepartment)
                .HasMaxLength(128)
                .HasColumnName("target_department");

            entity.HasOne(d => d.Apartment).WithMany(p => p.MaintenanceApartmentHistories)
                .HasForeignKey(d => d.ApartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_mah_apartment");

            entity.HasOne(d => d.CreatorUser).WithMany(p => p.MaintenanceApartmentHistoryCreatorUsers)
                .HasForeignKey(d => d.CreatorUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_mah_creator");

            entity.HasOne(d => d.HandlerUser).WithMany(p => p.MaintenanceApartmentHistoryHandlerUsers)
                .HasForeignKey(d => d.HandlerUserId)
                .HasConstraintName("FK_mah_handler");

            entity.HasOne(d => d.Request).WithMany(p => p.MaintenanceApartmentHistories)
                .HasForeignKey(d => d.RequestId)
                .HasConstraintName("FK_mah_request");
        });

        modelBuilder.Entity<Meter>(entity =>
        {
            entity.ToTable("meters", schema);

            entity.HasIndex(e => new { e.ApartmentId, e.ServiceId, e.SerialNo }, "UQ_meters").IsUnique();

            entity.Property(e => e.MeterId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("meter_id");
            entity.Property(e => e.ApartmentId).HasColumnName("apartment_id");
            entity.Property(e => e.InstalledAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("installed_at");
            entity.Property(e => e.Meta).HasColumnName("meta");
            entity.Property(e => e.SerialNo)
                .HasMaxLength(128)
                .HasColumnName("serial_no");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.Status)
                .HasMaxLength(32)
                .HasDefaultValue("ACTIVE")
                .HasColumnName("status");

            entity.HasOne(d => d.Apartment).WithMany(p => p.Meters)
                .HasForeignKey(d => d.ApartmentId)
                .HasConstraintName("FK_meters_apartment");

            entity.HasOne(d => d.Service).WithMany(p => p.Meters)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_meters_service");
        });

        modelBuilder.Entity<MeterReading>(entity =>
        {
            entity.HasKey(e => e.ReadingId);

            entity.ToTable("meter_readings", schema);

            entity.HasIndex(e => new { e.MeterId, e.ReadingTime }, "IX_meter_readings_time");

            entity.HasIndex(e => new { e.MeterId, e.ReadingTime }, "UQ_meter_readings").IsUnique();

            entity.Property(e => e.ReadingId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("reading_id");
            entity.Property(e => e.CapturedBy)
                .HasMaxLength(190)
                .HasColumnName("captured_by");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.IndexValue)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("index_value");
            entity.Property(e => e.MeterId).HasColumnName("meter_id");
            entity.Property(e => e.Note)
                .HasMaxLength(500)
                .HasColumnName("note");
            entity.Property(e => e.ReadingTime)
                .HasPrecision(3)
                .HasColumnName("reading_time");

            entity.HasOne(d => d.Meter).WithMany(p => p.MeterReadings)
                .HasForeignKey(d => d.MeterId)
                .HasConstraintName("FK_meter_readings_meter");
        });

        modelBuilder.Entity<ParkingEntry>(entity =>
        {
            entity.ToTable("parking_entries", schema);

            entity.HasIndex(e => new { e.CardId, e.EntryTime }, "IX_parking_entries_card_time");

            entity.Property(e => e.ParkingEntryId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("parking_entry_id");
            entity.Property(e => e.CardId).HasColumnName("card_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.EntryGate)
                .HasMaxLength(64)
                .HasColumnName("entry_gate");
            entity.Property(e => e.EntryTime)
                .HasPrecision(3)
                .HasColumnName("entry_time");
            entity.Property(e => e.ExitGate)
                .HasMaxLength(64)
                .HasColumnName("exit_gate");
            entity.Property(e => e.ExitTime)
                .HasPrecision(3)
                .HasColumnName("exit_time");
            entity.Property(e => e.FeeAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("fee_amount");
            entity.Property(e => e.FeeCurrency)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasDefaultValue("VND")
                .IsFixedLength()
                .HasColumnName("fee_currency");
            entity.Property(e => e.PlateSnapshot)
                .HasMaxLength(255)
                .HasColumnName("plate_snapshot");
            entity.Property(e => e.VehicleId).HasColumnName("vehicle_id");

            entity.HasOne(d => d.Card).WithMany(p => p.ParkingEntries)
                .HasForeignKey(d => d.CardId)
                .HasConstraintName("FK_parking_entries_card");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.ParkingEntries)
                .HasForeignKey(d => d.VehicleId)
                .HasConstraintName("FK_parking_entries_vehicle");
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.ToTable("payment_methods", schema);

            entity.HasIndex(e => e.Code, "UQ_payment_methods_code").IsUnique();

            entity.Property(e => e.PaymentMethodId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("payment_method_id");
            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasColumnName("active");
            entity.Property(e => e.Code)
                .HasMaxLength(64)
                .HasColumnName("code");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Receipt>(entity =>
        {
            entity.ToTable("receipts", schema);

            entity.HasIndex(e => e.InvoiceId, "UQ_receipts_invoice").IsUnique();

            entity.HasIndex(e => e.ReceiptNo, "UQ_receipts_no").IsUnique();

            entity.Property(e => e.ReceiptId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("receipt_id");
            entity.Property(e => e.AmountTotal)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount_total");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.InvoiceId).HasColumnName("invoice_id");
            entity.Property(e => e.MethodId).HasColumnName("method_id");
            entity.Property(e => e.Note)
                .HasMaxLength(1000)
                .HasColumnName("note");
            entity.Property(e => e.ReceiptNo)
                .HasMaxLength(64)
                .HasColumnName("receipt_no");
            entity.Property(e => e.ReceivedDate)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("received_date");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Receipts)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_receipts_created_by");

            entity.HasOne(d => d.Invoice).WithOne(p => p.Receipt)
                .HasForeignKey<Receipt>(d => d.InvoiceId)
                .HasConstraintName("FK_receipts_invoice");

            entity.HasOne(d => d.Method).WithMany(p => p.Receipts)
                .HasForeignKey(d => d.MethodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_receipts_method");
        });

        modelBuilder.Entity<ResidentApartment>(entity =>
        {
            entity.ToTable("resident_apartments", schema);

            entity.HasIndex(e => e.ApartmentId, "IX_resident_apartments_apartment");

            entity.HasIndex(e => new { e.ResidentId, e.ApartmentId, e.RelationType, e.StartDate }, "UQ_ra_unique").IsUnique();

            entity.Property(e => e.ResidentApartmentId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("resident_apartment_id");
            entity.Property(e => e.ApartmentId).HasColumnName("apartment_id");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.IsPrimary).HasColumnName("is_primary");
            entity.Property(e => e.RelationType)
                .HasMaxLength(32)
                .HasColumnName("relation_type");
            entity.Property(e => e.ResidentId).HasColumnName("resident_id");
            entity.Property(e => e.StartDate).HasColumnName("start_date");

            entity.HasOne(d => d.Apartment).WithMany(p => p.ResidentApartments)
                .HasForeignKey(d => d.ApartmentId)
                .HasConstraintName("FK_ra_apartment");

            entity.HasOne(d => d.Resident).WithMany(p => p.ResidentApartments)
                .HasForeignKey(d => d.ResidentId)
                .HasConstraintName("FK_ra_resident");
        });

        modelBuilder.Entity<ResidentProfile>(entity =>
        {
            entity.HasKey(e => e.ResidentId);

            entity.ToTable("resident_profiles", schema);

            entity.HasIndex(e => e.UserId, "UQ_resident_profiles_user").IsUnique();

            entity.Property(e => e.ResidentId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("resident_id");
            entity.Property(e => e.Address)
                .HasMaxLength(500)
                .HasColumnName("address");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Dob).HasColumnName("dob");
            entity.Property(e => e.Email)
                .HasMaxLength(190)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.Gender)
                .HasMaxLength(16)
                .HasColumnName("gender");
            entity.Property(e => e.IdNumber)
                .HasMaxLength(64)
                .HasColumnName("id_number");
            entity.Property(e => e.Meta).HasColumnName("meta");
            entity.Property(e => e.Phone)
                .HasMaxLength(50)
                .HasColumnName("phone");
            entity.Property(e => e.Status)
                .HasMaxLength(32)
                .HasDefaultValue("ACTIVE")
                .HasColumnName("status");
            entity.Property(e => e.IsVerified)
               .HasDefaultValue(false)
               .HasColumnName("is_verified");
            entity.Property(e => e.VerifiedAt)
                .HasPrecision(3)
                .HasColumnName("verified_at");
            entity.Property(e => e.Nationality)
                .HasMaxLength(64)
                .HasColumnName("nationality");
            entity.Property(e => e.InternalNote)
                .HasMaxLength(1000)
                .HasColumnName("internal_note");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.ResidentProfile)
                .HasForeignKey<ResidentProfile>(d => d.UserId)
                .HasConstraintName("FK_resident_profiles_user");
        });

        modelBuilder.Entity<ServicePrice>(entity =>
        {
            entity.HasKey(e => e.ServicePrices);

            entity.ToTable("service_prices", schema);

            entity.Property(e => e.ServicePrices)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("service_prices");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.ApprovedDate)
                .HasPrecision(3)
                .HasColumnName("approved_date");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.EffectiveDate).HasColumnName("effective_date");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.Notes)
                .HasMaxLength(1000)
                .HasColumnName("notes");
            entity.Property(e => e.ServiceTypeId).HasColumnName("service_type_id");
            entity.Property(e => e.Status)
                .HasMaxLength(32)
                .HasDefaultValue("ACTIVE")
                .HasColumnName("status");
            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("unit_price");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasColumnName("updated_at");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.ServicePriceApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK_fee_approved_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ServicePriceCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_fee_created_by");

            entity.HasOne(d => d.ServiceType).WithMany(p => p.ServicePrices)
                .HasForeignKey(d => d.ServiceTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_fee_service_type");
        });

        modelBuilder.Entity<ServiceTypeCategory>(entity =>
        {
            entity.Property(e => e.CategoryId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
        });

        modelBuilder.Entity<ServiceType>(entity =>
        {
            entity.Property(e => e.ServiceTypeId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsRecurring).HasDefaultValue(true);
            // IsDelete là nullable, không cần default value
            // entity.Property(e => e.IsDelete).HasDefaultValue(false);

            entity.HasOne(d => d.Category).WithMany(p => p.ServiceTypes).HasForeignKey(d => d.CategoryId).HasConstraintName("FK_service_types_category");
        });

        modelBuilder.Entity<ServiceType>()
                    .HasQueryFilter(x => x.IsDelete != true);

        modelBuilder.Entity<Asset>()
                    .HasQueryFilter(x => !x.IsDelete);

        modelBuilder.Entity<StaffProfile>(entity =>
        {
            entity.HasKey(e => e.StaffCode);

            entity.ToTable("staff_profiles", schema);

            entity.Property(e => e.StaffCode)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("staff_code");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.HireDate).HasColumnName("hire_date");
            entity.Property(e => e.Notes)
                .HasMaxLength(1000)
                .HasColumnName("notes");
            entity.Property(e => e.TerminationDate).HasColumnName("termination_date");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.Property(e => e.IsActive)
        .HasColumnName("is_active")
        // Nếu bạn chạy code-first có thể bật default; nếu database-first đã có DEFAULT thì có thể bỏ dòng dưới
        .HasDefaultValue(true);

            entity.Property(e => e.CurrentAddress)
                .HasMaxLength(250)
                .HasColumnName("current_address");

            entity.Property(e => e.EmergencyContactName)
                .HasMaxLength(150)
                .HasColumnName("emergency_contact_name");

            entity.Property(e => e.EmergencyContactPhone)
                .HasMaxLength(20)
                .HasColumnName("emergency_contact_phone");

            entity.Property(e => e.EmergencyContactRelation)
                .HasMaxLength(50)
                .HasColumnName("emergency_contact_relation");

            entity.Property(e => e.BankAccountNo)
                .HasMaxLength(50)
                .HasColumnName("bank_account_no");

            entity.Property(e => e.BankName)
                .HasMaxLength(100)
                .HasColumnName("bank_name");

            entity.Property(e => e.BaseSalary)
                .HasColumnType("decimal(18,2)")
                .HasColumnName("base_salary");

            entity.Property(e => e.TaxCode)
                .HasMaxLength(50)
                .HasColumnName("tax_code");

            entity.Property(e => e.SocialInsuranceNo)
                .HasMaxLength(50)
                .HasColumnName("social_insurance_no");

            entity.Property(e => e.CardPhotoUrl)
                .HasMaxLength(300)
                .HasColumnName("card_photo_url");

            entity.HasOne(d => d.User).WithMany(p => p.StaffProfiles)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_staff_profiles_user");

            entity.HasOne(d => d.Role)
              .WithMany(p => p.StaffProfiles)
              .HasForeignKey(d => d.RoleId)
              .OnDelete(DeleteBehavior.Restrict)
              .HasConstraintName("FK_staff_profiles_role");
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("tickets", schema);

            entity.HasIndex(e => new { e.Status, e.Priority, e.CreatedAt }, "IX_tickets_status_priority");

            entity.Property(e => e.TicketId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("ticket_id");
            entity.Property(e => e.Category)
                .HasMaxLength(64)
                .HasColumnName("category");
            entity.Property(e => e.ClosedAt)
                .HasPrecision(3)
                .HasColumnName("closed_at");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(DATEADD(HOUR, 7, SYSUTCDATETIME()))")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpectedCompletionAt)
                .HasPrecision(3)
                .HasColumnName("expected_completion_at");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Priority)
                .HasMaxLength(32)
                .HasColumnName("priority");
            entity.Property(e => e.Status)
                .HasMaxLength(32)
                .HasDefaultValue("Mới tạo")
                .HasColumnName("status");
            entity.Property(e => e.Subject)
                .HasMaxLength(255)
                .HasColumnName("subject");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasColumnName("updated_at");
            entity.Property(e => e.Scope).HasColumnName("scope");
            entity.Property(e => e.ApartmentId).HasColumnName("apartment_id");
            entity.Property(e => e.HasInvoice)
                .HasDefaultValue(false)
                .HasColumnName("has_invoice");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.TicketCreatedByUsers)
                .HasForeignKey(d => d.CreatedByUserId)
                .HasConstraintName("FK_tickets_users");

            entity.HasOne(d => d.Apartment).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.ApartmentId)
                .HasConstraintName("FK_tickets_apartment");
        });

        modelBuilder.Entity<TicketComment>(entity =>
        {
            entity.HasKey(e => e.CommentId);

            entity.ToTable("ticket_comments", schema);

            entity.Property(e => e.CommentId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("comment_id");
            entity.Property(e => e.CommentTime)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("comment_time");
            entity.Property(e => e.CommentedBy).HasColumnName("commented_by");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.TicketId).HasColumnName("ticket_id");

            entity.HasOne(d => d.Ticket).WithMany(p => p.TicketComments)
                .HasForeignKey(d => d.TicketId)
                .HasConstraintName("FK_ticket_comments_ticket");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users", schema);

            entity.HasIndex(e => e.Email, "UQ_users_email").IsUnique();

            entity.HasIndex(e => e.Username, "UQ_users_username").IsUnique();

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_id");
            entity.Property(e => e.Address)
                .HasMaxLength(150)
                .HasColumnName("address");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Dob).HasColumnName("dob");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .HasColumnName("email");
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(300)
                .HasColumnName("avatar_url");
            entity.Property(e => e.CheckinPhotoUrl)
                .HasMaxLength(300)
                .HasColumnName("checkin_photo_url");
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .HasColumnName("first_name");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasColumnName("last_name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasColumnName("updated_at");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .HasColumnName("username");
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.ToTable("vehicles", schema);

            entity.HasIndex(e => e.ResidentId, "IX_vehicles_resident");

            entity.HasIndex(e => e.LicensePlate, "UQ_vehicles_plate").IsUnique();

            entity.Property(e => e.VehicleId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("vehicle_id");
            entity.Property(e => e.ApartmentId).HasColumnName("apartment_id");
            entity.Property(e => e.BrandModel)
                .HasMaxLength(128)
                .HasColumnName("brand_model");
            entity.Property(e => e.Color)
                .HasMaxLength(64)
                .HasColumnName("color");
            entity.Property(e => e.LicensePlate)
                .HasMaxLength(64)
                .HasColumnName("license_plate");
            entity.Property(e => e.Meta).HasColumnName("meta");
            entity.Property(e => e.ParkingCardId).HasColumnName("parking_card_id");
            entity.Property(e => e.RegisteredAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("registered_at");
            entity.Property(e => e.ResidentId).HasColumnName("resident_id");
            entity.Property(e => e.Status)
                .HasMaxLength(32)
                .HasDefaultValue("ACTIVE")
                .HasColumnName("status");
            entity.Property(e => e.VehicleTypeId).HasColumnName("vehicle_type_id");

            entity.HasOne(d => d.Apartment).WithMany(p => p.Vehicles)
                .HasForeignKey(d => d.ApartmentId)
                .HasConstraintName("FK_vehicles_apartment");

            entity.HasOne(d => d.ParkingCard).WithMany(p => p.Vehicles)
                .HasForeignKey(d => d.ParkingCardId)
                .HasConstraintName("FK_vehicles_card");

            entity.HasOne(d => d.Resident).WithMany(p => p.Vehicles)
                .HasForeignKey(d => d.ResidentId)
                .HasConstraintName("FK_vehicles_resident");

            entity.HasOne(d => d.VehicleType).WithMany(p => p.Vehicles)
                .HasForeignKey(d => d.VehicleTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_vehicles_type");
        });

        modelBuilder.Entity<VehicleType>(entity =>
        {
            entity.ToTable("vehicle_types", schema);

            entity.HasIndex(e => e.Code, "UQ_vehicle_types_code").IsUnique();

            entity.Property(e => e.VehicleTypeId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("vehicle_type_id");
            entity.Property(e => e.Code)
                .HasMaxLength(64)
                .HasColumnName("code");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.ToTable("vouchers", schema);

            entity.HasIndex(e => e.VoucherNumber, "UQ_vouchers_number").IsUnique();

            entity.Property(e => e.VoucherId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("voucher_id");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.ApprovedDate)
                .HasPrecision(3)
                .HasColumnName("approved_date");
            entity.Property(e => e.CompanyInfo).HasColumnName("company_info");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Description)
                .HasMaxLength(1000)
                .HasColumnName("description");
            entity.Property(e => e.Status)
                .HasMaxLength(32)
                .HasDefaultValue("DRAFT")
                .HasColumnName("status");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("total_amount");
            entity.Property(e => e.Type)
                .HasMaxLength(32)
                .HasColumnName("type");
            entity.Property(e => e.VoucherNumber)
                .HasMaxLength(64)
                .HasColumnName("voucher_number");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.VoucherApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK_vouchers_approved_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.VoucherCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_vouchers_created_by");

            entity.Property(e => e.TicketId).HasColumnName("ticket_id");

            entity.HasOne(d => d.Ticket).WithMany(p => p.Vouchers)
                .HasForeignKey(d => d.TicketId)
                .HasConstraintName("FK_vouchers_ticket");

            entity.Property(e => e.HistoryId).HasColumnName("history_id");

            entity.HasOne(d => d.History).WithMany(p => p.Vouchers)
                .HasForeignKey(d => d.HistoryId)
                .HasConstraintName("FK_vouchers_history");
        });

        modelBuilder.Entity<VoucherItem>(entity =>
        {
            entity.HasKey(e => e.VoucherItemsId);

            entity.ToTable("voucher_items", schema);

            entity.Property(e => e.VoucherItemsId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("voucher_items_id");
            entity.Property(e => e.ApartmentId).HasColumnName("apartment_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.Quantity)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("quantity");
            entity.Property(e => e.ServiceTypeId).HasColumnName("service_type_id");
            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("unit_price");
            entity.Property(e => e.VoucherId).HasColumnName("voucher_id");

            entity.HasOne(d => d.Apartment).WithMany(p => p.VoucherItems)
                .HasForeignKey(d => d.ApartmentId)
                .HasConstraintName("FK_vi_apartment");

            entity.HasOne(d => d.ServiceType).WithMany(p => p.VoucherItems)
                .HasForeignKey(d => d.ServiceTypeId)
                .HasConstraintName("FK_vi_service_type");

            entity.HasOne(d => d.Voucher).WithMany(p => p.VoucherItems)
                .HasForeignKey(d => d.VoucherId)
                .HasConstraintName("FK_vi_voucher");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}