using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SAMS_BE.Infrastructure.Persistence.Global.Models;

namespace SAMS_BE.Infrastructure.Persistence.Global;

public partial class GlobalDirectoryContext : DbContext
{

    public GlobalDirectoryContext()
    {
    }

    public GlobalDirectoryContext(DbContextOptions<GlobalDirectoryContext> options)
        : base(options)
    {
    }

    public virtual DbSet<announcement_global> announcement_globals { get; set; }

    public virtual DbSet<audit_log_global> audit_log_globals { get; set; }

    public virtual DbSet<building> buildings { get; set; }

    public virtual DbSet<user_building> user_buildings { get; set; }

    public virtual DbSet<user_registry> user_registries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<announcement_global>(entity =>
        {
            entity.Property(e => e.id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.created_at).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.created_byNavigation).WithMany(p => p.announcement_globals)
                .HasPrincipalKey(p => p.keycloak_user_id)
                .HasForeignKey(d => d.created_by)
                .HasConstraintName("FK_announcement_created_by");
        });

        modelBuilder.Entity<audit_log_global>(entity =>
        {
            entity.Property(e => e.created_at).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.actor_keycloak).WithMany(p => p.audit_log_globals)
                .HasPrincipalKey(p => p.keycloak_user_id)
                .HasForeignKey(d => d.actor_keycloak_id)
                .HasConstraintName("FK_audit_actor_user");

            entity.HasOne(d => d.building).WithMany(p => p.audit_log_globals).HasConstraintName("FK_audit_building");
        });

        modelBuilder.Entity<building>(entity =>
        {
            entity.Property(e => e.id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.create_at).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.status).HasDefaultValue((byte)1);
            entity.Property(e => e.image_url).HasMaxLength(500);

            entity.Property(e => e.description).HasColumnType("nvarchar(max)");

            entity.Property(e => e.total_area_m2).HasPrecision(12, 2);

            entity.Property(e => e.opening_date).HasColumnType("date");

            entity.Property(e => e.latitude).HasPrecision(10, 7);
            entity.Property(e => e.longitude).HasPrecision(10, 7);

            entity.Property(e => e.is_deleted).HasDefaultValue(false);
            entity.Property(e => e.deleted_at).HasPrecision(3);

            entity.Property(e => e.created_by);
            entity.Property(e => e.updated_by);
        });


        modelBuilder.Entity<user_building>(entity =>
        {
            entity.Property(e => e.id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.create_at).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.status).HasDefaultValue((byte)1);

            entity.HasOne(d => d.building).WithMany(p => p.user_buildings)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_user_building_building");

            entity.HasOne(d => d.keycloak_user).WithMany(p => p.user_buildings)
                .HasPrincipalKey(p => p.keycloak_user_id)
                .HasForeignKey(d => d.keycloak_user_id)
                .HasConstraintName("FK_user_building_user_registry");
        });

        modelBuilder.Entity<user_registry>(entity =>
        {
            entity.Property(e => e.id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.create_at).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.status).HasDefaultValue((byte)1);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
