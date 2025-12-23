using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Interfaces.IRepository.GlobalAdmin;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Models;
using SAMS_BE.Services;
using SAMS_BE.Tenant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SAMS_BETests.Services
{
    [TestClass]
    public class AssetMaintenanceScheduleServiceTests
    {
        private Mock<IAssetMaintenanceScheduleRepository> _scheduleRepositoryMock = null!;
        private Mock<IAssetRepository> _assetRepositoryMock = null!;
        private Mock<ITicketRepository> _ticketRepositoryMock = null!;
        private Mock<IAnnouncementRepository> _announcementRepositoryMock = null!;
        private Mock<ITenantContextAccessor> _tenantContextAccessorMock = null!;
        private Mock<IBuildingRepository> _buildingRepositoryMock = null!;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock = null!;
        private Mock<ILogger<AssetMaintenanceScheduleService>> _loggerMock = null!;
        private Mock<IAmenityRepository> _amenityRepositoryMock = null!;
        private Mock<IAssetMaintenanceHistoryService> _maintenanceHistoryServiceMock = null!;
        private Mock<IServiceProvider> _serviceProviderMock = null!;
        private BuildingManagementContext _context = null!;
        private AssetMaintenanceScheduleService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _scheduleRepositoryMock = new Mock<IAssetMaintenanceScheduleRepository>();
            _assetRepositoryMock = new Mock<IAssetRepository>();
            _ticketRepositoryMock = new Mock<ITicketRepository>();
            _announcementRepositoryMock = new Mock<IAnnouncementRepository>();
            _tenantContextAccessorMock = new Mock<ITenantContextAccessor>();
            _buildingRepositoryMock = new Mock<IBuildingRepository>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _loggerMock = new Mock<ILogger<AssetMaintenanceScheduleService>>();
            _amenityRepositoryMock = new Mock<IAmenityRepository>();
            _maintenanceHistoryServiceMock = new Mock<IAssetMaintenanceHistoryService>();
            _serviceProviderMock = new Mock<IServiceProvider>();

            var options = new DbContextOptionsBuilder<BuildingManagementContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new BuildingManagementContext(options, _tenantContextAccessorMock.Object);

            _service = new AssetMaintenanceScheduleService(
                _scheduleRepositoryMock.Object,
                _assetRepositoryMock.Object,
                _ticketRepositoryMock.Object,
                _announcementRepositoryMock.Object,
                _tenantContextAccessorMock.Object,
                _buildingRepositoryMock.Object,
                _context,
                _httpContextAccessorMock.Object,
                _loggerMock.Object,
                _amenityRepositoryMock.Object,
                _maintenanceHistoryServiceMock.Object,
                _serviceProviderMock.Object);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context?.Dispose();
        }

        #region Helper Methods

        private AssetMaintenanceSchedule CreateTestSchedule(
            Guid? scheduleId = null,
            Guid? assetId = null,
            DateOnly? startDate = null,
            DateOnly? endDate = null,
            string? status = "SCHEDULED",
            string? recurrenceType = null,
            int? recurrenceInterval = null)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
            return new AssetMaintenanceSchedule
            {
                ScheduleId = scheduleId ?? Guid.NewGuid(),
                AssetId = assetId ?? Guid.NewGuid(),
                StartDate = startDate ?? today.AddDays(1),
                EndDate = endDate ?? today.AddDays(4),
                Status = status ?? "SCHEDULED",
                ReminderDays = 3,
                Description = "Test schedule",
                RecurrenceType = recurrenceType,
                RecurrenceInterval = recurrenceInterval,
                CreatedAt = DateTime.UtcNow,
                Asset = new Asset
                {
                    AssetId = assetId ?? Guid.NewGuid(),
                    Code = "ASSET-001",
                    Name = "Test Asset",
                    Status = "ACTIVE",
                    Category = new AssetCategory
                    {
                        CategoryId = Guid.NewGuid(),
                        Code = "CATEGORY-001",
                        Name = "Test Category",
                        DefaultReminderDays = 3
                    }
                }
            };
        }

        private Asset CreateTestAsset(Guid? assetId = null, int? defaultReminderDays = 3)
        {
            return new Asset
            {
                AssetId = assetId ?? Guid.NewGuid(),
                Code = "ASSET-001",
                Name = "Test Asset",
                Status = "ACTIVE",
                Category = new AssetCategory
                {
                    CategoryId = Guid.NewGuid(),
                    Code = "CATEGORY-001",
                    Name = "Test Category",
                    DefaultReminderDays = defaultReminderDays
                }
            };
        }

        private ClaimsPrincipal CreateTestUser(bool isManager = true, Guid? userId = null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, (userId ?? Guid.NewGuid()).ToString()),
                new Claim("sub", (userId ?? Guid.NewGuid()).ToString()),
                new Claim(ClaimTypes.Role, isManager ? "Building_Management" : "Resident")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            identity.AddClaim(new Claim(ClaimTypes.Authentication, "true"));
            return new ClaimsPrincipal(identity);
        }

        #endregion

        #region Constructor Test

        [TestMethod]
        public void AssetMaintenanceScheduleServiceTest()
        {
            // Arrange & Act
            var service = new AssetMaintenanceScheduleService(
                _scheduleRepositoryMock.Object,
                _assetRepositoryMock.Object,
                _ticketRepositoryMock.Object,
                _announcementRepositoryMock.Object,
                _tenantContextAccessorMock.Object,
                _buildingRepositoryMock.Object,
                _context,
                _httpContextAccessorMock.Object,
                _loggerMock.Object,
                _amenityRepositoryMock.Object,
                _maintenanceHistoryServiceMock.Object,
                _serviceProviderMock.Object);

            // Assert
            Assert.IsNotNull(service);
        }

        #endregion

        #region GetAllSchedulesAsync Tests

        [TestMethod]
        public async Task GetAllSchedulesAsync_Success_ReturnsScheduleDtos()
        {
            // Arrange
            var schedules = new List<AssetMaintenanceSchedule>
            {
                CreateTestSchedule(),
                CreateTestSchedule()
            };

            _scheduleRepositoryMock
                .Setup(r => r.GetAllSchedulesAsync())
                .ReturnsAsync(schedules);

            // Act
            var result = await _service.GetAllSchedulesAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
            _scheduleRepositoryMock.Verify(r => r.GetAllSchedulesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task GetAllSchedulesAsync_EmptyList_ReturnsEmptyList()
        {
            // Arrange
            _scheduleRepositoryMock
                .Setup(r => r.GetAllSchedulesAsync())
                .ReturnsAsync(new List<AssetMaintenanceSchedule>());

            // Act
            var result = await _service.GetAllSchedulesAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public async Task GetAllSchedulesAsync_Exception_ThrowsAndLogs()
        {
            // Arrange
            var exception = new Exception("Database error");
            _scheduleRepositoryMock
                .Setup(r => r.GetAllSchedulesAsync())
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.GetAllSchedulesAsync());
        }

        #endregion

        #region GetScheduleByIdAsync Tests

        [TestMethod]
        public async Task GetScheduleByIdAsync_ScheduleExists_ReturnsScheduleDto()
        {
            // Arrange
            var scheduleId = Guid.NewGuid();
            var schedule = CreateTestSchedule(scheduleId);

            _scheduleRepositoryMock
                .Setup(r => r.GetScheduleByIdAsync(scheduleId))
                .ReturnsAsync(schedule);

            // Act
            var result = await _service.GetScheduleByIdAsync(scheduleId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(scheduleId, result.ScheduleId);
            _scheduleRepositoryMock.Verify(r => r.GetScheduleByIdAsync(scheduleId), Times.Once);
        }

        [TestMethod]
        public async Task GetScheduleByIdAsync_ScheduleNotFound_ReturnsNull()
        {
            // Arrange
            var scheduleId = Guid.NewGuid();
            _scheduleRepositoryMock
                .Setup(r => r.GetScheduleByIdAsync(scheduleId))
                .ReturnsAsync((AssetMaintenanceSchedule?)null);

            // Act
            var result = await _service.GetScheduleByIdAsync(scheduleId);

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region GetSchedulesByAssetIdAsync Tests

        [TestMethod]
        public async Task GetSchedulesByAssetIdAsync_Success_ReturnsSchedules()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var schedules = new List<AssetMaintenanceSchedule>
            {
                CreateTestSchedule(assetId: assetId),
                CreateTestSchedule(assetId: assetId)
            };

            _scheduleRepositoryMock
                .Setup(r => r.GetSchedulesByAssetIdAsync(assetId))
                .ReturnsAsync(schedules);

            // Act
            var result = await _service.GetSchedulesByAssetIdAsync(assetId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.All(s => s.AssetId == assetId));
        }

        #endregion

        #region GetSchedulesByStatusAsync Tests

        [TestMethod]
        public async Task GetSchedulesByStatusAsync_Success_ReturnsSchedulesWithStatus()
        {
            // Arrange
            var status = "SCHEDULED";
            var schedules = new List<AssetMaintenanceSchedule>
            {
                CreateTestSchedule(status: "SCHEDULED")
            };

            _scheduleRepositoryMock
                .Setup(r => r.GetSchedulesByStatusAsync(status))
                .ReturnsAsync(schedules);

            // Act
            var result = await _service.GetSchedulesByStatusAsync(status);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.All(s => s.Status == status));
        }

        #endregion

        #region CreateScheduleAsync Tests

        [TestMethod]
        public async Task CreateScheduleAsync_Success_ReturnsScheduleDto()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var asset = CreateTestAsset(assetId);
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
            var createDto = new CreateAssetMaintenanceScheduleDto
            {
                AssetId = assetId,
                StartDate = today.AddDays(1),
                EndDate = today.AddDays(4),
                ReminderDays = 3,
                Description = "Test schedule",
                Status = "SCHEDULED"
            };

            var schedule = CreateTestSchedule(assetId: assetId);

            _assetRepositoryMock
                .Setup(r => r.GetAssetByIdAsync(assetId))
                .ReturnsAsync(asset);

            _scheduleRepositoryMock
                .Setup(r => r.IsAssetUnderMaintenanceAsync(assetId))
                .ReturnsAsync(false);

            _scheduleRepositoryMock
                .Setup(r => r.GetOverlappingSchedulesAsync(
                    assetId,
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<TimeOnly?>(),
                    It.IsAny<TimeOnly?>(),
                    It.IsAny<Guid?>()))
                .ReturnsAsync(new List<AssetMaintenanceSchedule>());

            _scheduleRepositoryMock
                .Setup(r => r.CreateScheduleAsync(It.IsAny<AssetMaintenanceSchedule>()))
                .ReturnsAsync(schedule);

            _scheduleRepositoryMock
                .Setup(r => r.GetScheduleByIdAsync(schedule.ScheduleId))
                .ReturnsAsync(schedule);

            _announcementRepositoryMock
                .Setup(r => r.CreateAnnouncementAsync(It.IsAny<Announcement>()))
                .ReturnsAsync(new Announcement());

            // Act
            var result = await _service.CreateScheduleAsync(createDto, null);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(schedule.ScheduleId, result.ScheduleId);
            _assetRepositoryMock.Verify(r => r.GetAssetByIdAsync(assetId), Times.Once);
            _scheduleRepositoryMock.Verify(r => r.CreateScheduleAsync(It.IsAny<AssetMaintenanceSchedule>()), Times.Once);
        }

        [TestMethod]
        public async Task CreateScheduleAsync_AssetNotFound_ThrowsArgumentException()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
            var createDto = new CreateAssetMaintenanceScheduleDto
            {
                AssetId = assetId,
                StartDate = today.AddDays(1),
                EndDate = today.AddDays(4)
            };

            _assetRepositoryMock
                .Setup(r => r.GetAssetByIdAsync(assetId))
                .ReturnsAsync((Asset?)null);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await _service.CreateScheduleAsync(createDto, null),
                $"Asset với ID {assetId} không tồn tại");
        }

        [TestMethod]
        public async Task CreateScheduleAsync_EndDateBeforeStartDate_ThrowsArgumentException()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var asset = CreateTestAsset(assetId);
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
            var createDto = new CreateAssetMaintenanceScheduleDto
            {
                AssetId = assetId,
                StartDate = today.AddDays(4),
                EndDate = today.AddDays(1) // EndDate before StartDate
            };

            _assetRepositoryMock
                .Setup(r => r.GetAssetByIdAsync(assetId))
                .ReturnsAsync(asset);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await _service.CreateScheduleAsync(createDto, null),
                "EndDate phải lớn hơn hoặc bằng StartDate");
        }

        [TestMethod]
        public async Task CreateScheduleAsync_StartDateInPast_ThrowsArgumentException()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var asset = CreateTestAsset(assetId);
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
            var createDto = new CreateAssetMaintenanceScheduleDto
            {
                AssetId = assetId,
                StartDate = today.AddDays(-1), // Past date
                EndDate = today.AddDays(4)
            };

            _assetRepositoryMock
                .Setup(r => r.GetAssetByIdAsync(assetId))
                .ReturnsAsync(asset);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await _service.CreateScheduleAsync(createDto, null),
                "Ngày bắt đầu không được trong quá khứ");
        }

        [TestMethod]
        public async Task CreateScheduleAsync_WithSkipDateValidation_AllowsPastDate()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var asset = CreateTestAsset(assetId);
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
            var createDto = new CreateAssetMaintenanceScheduleDto
            {
                AssetId = assetId,
                StartDate = today.AddDays(-1), // Past date but skip validation
                EndDate = today.AddDays(4)
            };

            var schedule = CreateTestSchedule(assetId: assetId);

            _assetRepositoryMock
                .Setup(r => r.GetAssetByIdAsync(assetId))
                .ReturnsAsync(asset);

            _scheduleRepositoryMock
                .Setup(r => r.CreateScheduleAsync(It.IsAny<AssetMaintenanceSchedule>()))
                .ReturnsAsync(schedule);

            _scheduleRepositoryMock
                .Setup(r => r.GetScheduleByIdAsync(schedule.ScheduleId))
                .ReturnsAsync(schedule);

            // Act
            var result = await _service.CreateScheduleAsync(createDto, null, skipDateValidation: true);

            // Assert
            Assert.IsNotNull(result);
            _scheduleRepositoryMock.Verify(r => r.CreateScheduleAsync(It.IsAny<AssetMaintenanceSchedule>()), Times.Once);
        }

        [TestMethod]
        public async Task CreateScheduleAsync_StartTimeInPast_ThrowsArgumentException()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var asset = CreateTestAsset(assetId);
            var nowVN = DateTime.UtcNow.AddHours(7);
            var today = DateOnly.FromDateTime(nowVN);
            var currentTime = TimeOnly.FromDateTime(nowVN);
            
            var createDto = new CreateAssetMaintenanceScheduleDto
            {
                AssetId = assetId,
                StartDate = today,
                EndDate = today,
                StartTime = currentTime.AddHours(-1), // 1 hour in the past
                EndTime = currentTime.AddHours(2)
            };

            _assetRepositoryMock
                .Setup(r => r.GetAssetByIdAsync(assetId))
                .ReturnsAsync(asset);

            // Act & Assert
            var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await _service.CreateScheduleAsync(createDto, null));
            
            Assert.IsTrue(exception.Message.Contains("Giờ bắt đầu không được trong quá khứ"));
        }

        [TestMethod]
        public async Task CreateScheduleAsync_AssetUnderMaintenance_ThrowsArgumentException()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var asset = CreateTestAsset(assetId);
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
            var createDto = new CreateAssetMaintenanceScheduleDto
            {
                AssetId = assetId,
                StartDate = today.AddDays(1),
                EndDate = today.AddDays(4)
            };

            _assetRepositoryMock
                .Setup(r => r.GetAssetByIdAsync(assetId))
                .ReturnsAsync(asset);

            _scheduleRepositoryMock
                .Setup(r => r.IsAssetUnderMaintenanceAsync(assetId))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await _service.CreateScheduleAsync(createDto, null),
                "đang trong quá trình bảo trì");
        }

        [TestMethod]
        public async Task CreateScheduleAsync_OverlappingSchedule_ThrowsArgumentException()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var asset = CreateTestAsset(assetId);
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
            var createDto = new CreateAssetMaintenanceScheduleDto
            {
                AssetId = assetId,
                StartDate = today.AddDays(1),
                EndDate = today.AddDays(4)
            };

            var overlappingSchedule = CreateTestSchedule(assetId: assetId);

            _assetRepositoryMock
                .Setup(r => r.GetAssetByIdAsync(assetId))
                .ReturnsAsync(asset);

            _scheduleRepositoryMock
                .Setup(r => r.IsAssetUnderMaintenanceAsync(assetId))
                .ReturnsAsync(false);

            _scheduleRepositoryMock
                .Setup(r => r.GetOverlappingSchedulesAsync(
                    assetId,
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<TimeOnly?>(),
                    It.IsAny<TimeOnly?>(),
                    It.IsAny<Guid?>()))
                .ReturnsAsync(new List<AssetMaintenanceSchedule> { overlappingSchedule });

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await _service.CreateScheduleAsync(createDto, null),
                "Lịch bảo trì trùng");
        }

        [TestMethod]
        public async Task CreateScheduleAsync_InvalidTimeRange_ThrowsArgumentException()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var asset = CreateTestAsset(assetId);
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
            var createDto = new CreateAssetMaintenanceScheduleDto
            {
                AssetId = assetId,
                StartDate = today.AddDays(1),
                EndDate = today.AddDays(1), // Same day
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(9, 0) // EndTime before StartTime
            };

            _assetRepositoryMock
                .Setup(r => r.GetAssetByIdAsync(assetId))
                .ReturnsAsync(asset);

            _scheduleRepositoryMock
                .Setup(r => r.IsAssetUnderMaintenanceAsync(assetId))
                .ReturnsAsync(false);

            _scheduleRepositoryMock
                .Setup(r => r.GetOverlappingSchedulesAsync(
                    assetId,
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<TimeOnly?>(),
                    It.IsAny<TimeOnly?>(),
                    It.IsAny<Guid?>()))
                .ReturnsAsync(new List<AssetMaintenanceSchedule>());

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await _service.CreateScheduleAsync(createDto, null),
                "giờ kết thúc phải sau giờ bắt đầu");
        }

        [TestMethod]
        public async Task CreateScheduleAsync_OnlyStartTime_ThrowsArgumentException()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var asset = CreateTestAsset(assetId);
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
            var createDto = new CreateAssetMaintenanceScheduleDto
            {
                AssetId = assetId,
                StartDate = today.AddDays(1),
                EndDate = today.AddDays(4),
                StartTime = new TimeOnly(10, 0)
                // Missing EndTime
            };

            _assetRepositoryMock
                .Setup(r => r.GetAssetByIdAsync(assetId))
                .ReturnsAsync(asset);

            _scheduleRepositoryMock
                .Setup(r => r.IsAssetUnderMaintenanceAsync(assetId))
                .ReturnsAsync(false);

            _scheduleRepositoryMock
                .Setup(r => r.GetOverlappingSchedulesAsync(
                    assetId,
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<TimeOnly?>(),
                    It.IsAny<TimeOnly?>(),
                    It.IsAny<Guid?>()))
                .ReturnsAsync(new List<AssetMaintenanceSchedule>());

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await _service.CreateScheduleAsync(createDto, null),
                "Nếu có StartTime thì phải có EndTime");
        }

        #endregion

        #region UpdateScheduleAsync Tests

        [TestMethod]
        public async Task UpdateScheduleAsync_Success_ReturnsUpdatedScheduleDto()
        {
            // Arrange
            var scheduleId = Guid.NewGuid();
            var existingSchedule = CreateTestSchedule(scheduleId);
            var updateDto = new UpdateAssetMaintenanceScheduleDto
            {
                Description = "Updated description",
                Status = "IN_PROGRESS"
            };

            var updatedSchedule = CreateTestSchedule(scheduleId, status: "IN_PROGRESS");
            updatedSchedule.Description = "Updated description";

            var user = CreateTestUser(isManager: true);
            var httpContext = new DefaultHttpContext
            {
                User = user
            };

            _httpContextAccessorMock
                .Setup(h => h.HttpContext)
                .Returns(httpContext);

            _scheduleRepositoryMock
                .Setup(r => r.GetScheduleByIdAsync(scheduleId))
                .ReturnsAsync(existingSchedule);

            _scheduleRepositoryMock
                .Setup(r => r.UpdateScheduleAsync(It.IsAny<AssetMaintenanceSchedule>()))
                .ReturnsAsync(updatedSchedule);

            _scheduleRepositoryMock
                .Setup(r => r.GetScheduleByIdAsync(scheduleId))
                .ReturnsAsync(updatedSchedule);

            // Act
            var result = await _service.UpdateScheduleAsync(updateDto, scheduleId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(scheduleId, result.ScheduleId);
            _scheduleRepositoryMock.Verify(r => r.GetScheduleByIdAsync(scheduleId), Times.AtLeastOnce);
            _scheduleRepositoryMock.Verify(r => r.UpdateScheduleAsync(It.IsAny<AssetMaintenanceSchedule>()), Times.Once);
        }

        [TestMethod]
        public async Task UpdateScheduleAsync_ScheduleNotFound_ReturnsNull()
        {
            // Arrange
            var scheduleId = Guid.NewGuid();
            var updateDto = new UpdateAssetMaintenanceScheduleDto
            {
                Description = "Updated description"
            };

            _scheduleRepositoryMock
                .Setup(r => r.GetScheduleByIdAsync(scheduleId))
                .ReturnsAsync((AssetMaintenanceSchedule?)null);

            // Act
            var result = await _service.UpdateScheduleAsync(updateDto, scheduleId);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task UpdateScheduleAsync_InvalidStatusTransition_ThrowsArgumentException()
        {
            // Arrange
            var scheduleId = Guid.NewGuid();
            var existingSchedule = CreateTestSchedule(scheduleId, status: "DONE");
            var updateDto = new UpdateAssetMaintenanceScheduleDto
            {
                Status = "SCHEDULED" // Invalid transition from DONE
            };

            var user = CreateTestUser(isManager: true);
            var httpContext = new DefaultHttpContext
            {
                User = user
            };

            _httpContextAccessorMock
                .Setup(h => h.HttpContext)
                .Returns(httpContext);

            _scheduleRepositoryMock
                .Setup(r => r.GetScheduleByIdAsync(scheduleId))
                .ReturnsAsync(existingSchedule);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await _service.UpdateScheduleAsync(updateDto, scheduleId),
                "Không thể đổi status");
        }

        #endregion

        #region DeleteScheduleAsync Tests

        [TestMethod]
        public async Task DeleteScheduleAsync_Success_ReturnsTrue()
        {
            // Arrange
            var scheduleId = Guid.NewGuid();
            _scheduleRepositoryMock
                .Setup(r => r.DeleteScheduleAsync(scheduleId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteScheduleAsync(scheduleId);

            // Assert
            Assert.IsTrue(result);
            _scheduleRepositoryMock.Verify(r => r.DeleteScheduleAsync(scheduleId), Times.Once);
        }

        [TestMethod]
        public async Task DeleteScheduleAsync_ScheduleNotFound_ReturnsFalse()
        {
            // Arrange
            var scheduleId = Guid.NewGuid();
            _scheduleRepositoryMock
                .Setup(r => r.DeleteScheduleAsync(scheduleId))
                .ReturnsAsync(false);

            // Act
            var result = await _service.DeleteScheduleAsync(scheduleId);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region GetSchedulesDueForReminderAsync Tests

        [TestMethod]
        public async Task GetSchedulesDueForReminderAsync_Success_ReturnsSchedules()
        {
            // Arrange
            var schedules = new List<AssetMaintenanceSchedule>
            {
                CreateTestSchedule()
            };

            _scheduleRepositoryMock
                .Setup(r => r.GetSchedulesDueForReminderAsync(It.IsAny<DateOnly>()))
                .ReturnsAsync(schedules);

            // Act
            var result = await _service.GetSchedulesDueForReminderAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
        }

        #endregion

        #region GetSchedulesDueForMaintenanceAsync Tests

        [TestMethod]
        public async Task GetSchedulesDueForMaintenanceAsync_Success_ReturnsSchedules()
        {
            // Arrange
            var schedules = new List<AssetMaintenanceSchedule>
            {
                CreateTestSchedule()
            };

            _scheduleRepositoryMock
                .Setup(r => r.GetSchedulesDueForMaintenanceAsync(It.IsAny<DateOnly>()))
                .ReturnsAsync(schedules);

            // Act
            var result = await _service.GetSchedulesDueForMaintenanceAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
        }

        #endregion
    }
}
