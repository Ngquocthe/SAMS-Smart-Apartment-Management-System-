using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IRepository.GlobalAdmin;
using SAMS_BE.Models;
using SAMS_BE.Services;
using SAMS_BE.Tenant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAMS_BE.Services.Tests
{
    [TestClass]
    public class DashboardServiceTests
    {
        private BuildingManagementContext _db = null!;
        private Mock<IBuildingRepository> _buildingRepoMock = null!;
        private Mock<ILogger<DashboardService>> _loggerMock = null!;
        private DashboardService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<BuildingManagementContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _db = new BuildingManagementContext(options, new TenantContextAccessor());
            _buildingRepoMock = new Mock<IBuildingRepository>();
            _loggerMock = new Mock<ILogger<DashboardService>>();

            _service = new DashboardService(_db, _buildingRepoMock.Object, _loggerMock.Object);
        }

        private async Task SeedBasicDataAsync()
        {
            // Apartments (cần FloorId và Status, CreatedAt để thỏa nullable)
            var floorForApt = new Floor
            {
                FloorId = Guid.NewGuid(),
                FloorNumber = 0,
                Name = "Tầng 0"
            };
            await _db.Floors.AddAsync(floorForApt);

            var a1 = new Apartment
            {
                ApartmentId = Guid.NewGuid(),
                FloorId = floorForApt.FloorId,
                Number = "A101",
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow
            };
            var a2 = new Apartment
            {
                ApartmentId = Guid.NewGuid(),
                FloorId = floorForApt.FloorId,
                Number = "A102",
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow
            };
            await _db.Apartments.AddRangeAsync(a1, a2);

            // Residents
            await _db.ResidentProfiles.AddRangeAsync(
                new ResidentProfile { ResidentId = Guid.NewGuid(), FullName = "R1", Status = "ACTIVE", CreatedAt = DateTime.UtcNow },
                new ResidentProfile { ResidentId = Guid.NewGuid(), FullName = "R2", Status = "ACTIVE", CreatedAt = DateTime.UtcNow }
            );

            // ResidentApartments: only a1 occupied
            await _db.ResidentApartments.AddAsync(new ResidentApartment
            {
                ResidentApartmentId = Guid.NewGuid(),
                ApartmentId = a1.ApartmentId,
                IsPrimary = true,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),
                EndDate = null,
                RelationType = "Owner"
            });

            // Tickets
            await _db.Tickets.AddRangeAsync(
                new Ticket { TicketId = Guid.NewGuid(), Category = "Bảo trì", Status = "Mới tạo", Subject = "BT1", CreatedAt = DateTime.UtcNow.AddHours(-1) },
                new Ticket { TicketId = Guid.NewGuid(), Category = "An ninh", Status = "Đang xử lý", Subject = "AN1", CreatedAt = DateTime.UtcNow.AddHours(-2), Priority = "Khẩn cấp" }
            );

            // Invoices
            var today = DateOnly.FromDateTime(DateTime.Now);
            await _db.Invoices.AddRangeAsync(
                new Invoice
                {
                    InvoiceId = Guid.NewGuid(),
                    InvoiceNo = "HD001",
                    Status = "PAID",
                    IssueDate = today,
                    TotalAmount = 1_000_000,
                    CreatedAt = DateTime.UtcNow.AddHours(-3)
                },
                new Invoice
                {
                    InvoiceId = Guid.NewGuid(),
                    InvoiceNo = "HD002",
                    Status = "OVERDUE",
                    IssueDate = today.AddMonths(-1),
                    TotalAmount = 2_000_000,
                    CreatedAt = DateTime.UtcNow.AddHours(-4),
                    DueDate = today.AddDays(-1)
                }
            );

            // Floors for occupancy chart
            var floor = new Floor
            {
                FloorId = Guid.NewGuid(),
                FloorNumber = 1,
                Name = "Tầng 1",
                Apartments = new List<Apartment> { a1, a2 }
            };
            await _db.Floors.AddAsync(floor);

            await _db.SaveChangesAsync();

            // Buildings from global repository
            _buildingRepoMock
                .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SAMS_BE.DTOs.Response.Building.BuildingDto>
                {
                    new SAMS_BE.DTOs.Response.Building.BuildingDto { Id = Guid.NewGuid(), BuildingName = "B1" },
                    new SAMS_BE.DTOs.Response.Building.BuildingDto { Id = Guid.NewGuid(), BuildingName = "B2" }
                });
        }
    }
}