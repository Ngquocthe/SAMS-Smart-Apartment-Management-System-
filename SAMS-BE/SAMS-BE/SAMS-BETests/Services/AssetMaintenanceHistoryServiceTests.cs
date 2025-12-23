using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Models;
using SAMS_BE.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAMS_BETests.Services
{
    [TestClass]
    public class AssetMaintenanceHistoryServiceTests
    {
        private Mock<IAssetMaintenanceHistoryRepository> _historyRepositoryMock = null!;
        private Mock<IAssetRepository> _assetRepositoryMock = null!;
        private Mock<IAssetMaintenanceScheduleRepository> _scheduleRepositoryMock = null!;
        private Mock<IUserRepository> _userRepositoryMock = null!;
        private Mock<ILogger<AssetMaintenanceHistoryService>> _loggerMock = null!;
        private AssetMaintenanceHistoryService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _historyRepositoryMock = new Mock<IAssetMaintenanceHistoryRepository>();
            _assetRepositoryMock = new Mock<IAssetRepository>();
            _scheduleRepositoryMock = new Mock<IAssetMaintenanceScheduleRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _loggerMock = new Mock<ILogger<AssetMaintenanceHistoryService>>();

            _service = new AssetMaintenanceHistoryService(
                _historyRepositoryMock.Object,
                _assetRepositoryMock.Object,
                _scheduleRepositoryMock.Object,
                _userRepositoryMock.Object,
                _loggerMock.Object);
        }

        #region Helper Methods

        private AssetMaintenanceHistory CreateTestHistory(
            Guid? historyId = null,
            Guid? assetId = null,
            Guid? scheduleId = null,
            DateTime? actionDate = null,
            string? action = "Maintenance",
            decimal? costAmount = null)
        {
            return new AssetMaintenanceHistory
            {
                HistoryId = historyId ?? Guid.NewGuid(),
                AssetId = assetId ?? Guid.NewGuid(),
                ScheduleId = scheduleId,
                ActionDate = actionDate ?? DateTime.UtcNow,
                Action = action ?? "Maintenance",
                CostAmount = costAmount,
                Notes = "Test notes",
                NextDueDate = null,
                Asset = new Asset
                {
                    AssetId = assetId ?? Guid.NewGuid(),
                    Code = "ASSET-001",
                    Name = "Test Asset",
                    Status = "ACTIVE"
                },
                Schedule = scheduleId.HasValue ? new AssetMaintenanceSchedule
                {
                    ScheduleId = scheduleId.Value,
                    AssetId = assetId ?? Guid.NewGuid(),
                    StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
                    Status = "SCHEDULED",
                    CreatedBy = Guid.NewGuid()
                } : null
            };
        }

        private Asset CreateTestAsset(Guid? assetId = null)
        {
            return new Asset
            {
                AssetId = assetId ?? Guid.NewGuid(),
                Code = "ASSET-001",
                Name = "Test Asset",
                Status = "ACTIVE"
            };
        }

        private AssetMaintenanceSchedule CreateTestSchedule(
            Guid? scheduleId = null,
            Guid? assetId = null,
            string? recurrenceType = null,
            int? recurrenceInterval = null)
        {
            return new AssetMaintenanceSchedule
            {
                ScheduleId = scheduleId ?? Guid.NewGuid(),
                AssetId = assetId ?? Guid.NewGuid(),
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
                Status = "SCHEDULED",
                RecurrenceType = recurrenceType,
                RecurrenceInterval = recurrenceInterval,
                ReminderDays = 3,
                Description = "Test schedule",
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };
        }

        private User CreateTestUser(Guid? userId = null, string? firstName = "John", string? lastName = "Doe")
        {
            return new User
            {
                UserId = userId ?? Guid.NewGuid(),
                Username = "johndoe",
                Email = "john@example.com",
                FirstName = firstName ?? "John",
                LastName = lastName ?? "Doe"
            };
        }

        #endregion

        #region Constructor Test

        [TestMethod]
        public void AssetMaintenanceHistoryServiceTest()
        {
            // Arrange & Act
            var service = new AssetMaintenanceHistoryService(
                _historyRepositoryMock.Object,
                _assetRepositoryMock.Object,
                _scheduleRepositoryMock.Object,
                _userRepositoryMock.Object,
                _loggerMock.Object);

            // Assert
            Assert.IsNotNull(service);
        }

        #endregion

        #region GetAllHistoriesAsync Tests

        [TestMethod]
        public async Task GetAllHistoriesAsync_Success_ReturnsHistoryDtos()
        {
            // Arrange
            var histories = new List<AssetMaintenanceHistory>
            {
                CreateTestHistory(action: "Maintenance 1"),
                CreateTestHistory(action: "Maintenance 2")
            };

            _historyRepositoryMock
                .Setup(r => r.GetAllHistoriesAsync())
                .ReturnsAsync(histories);

            _userRepositoryMock
                .Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new List<User>());

            // Act
            var result = await _service.GetAllHistoriesAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
            _historyRepositoryMock.Verify(r => r.GetAllHistoriesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task GetAllHistoriesAsync_EmptyList_ReturnsEmptyList()
        {
            // Arrange
            _historyRepositoryMock
                .Setup(r => r.GetAllHistoriesAsync())
                .ReturnsAsync(new List<AssetMaintenanceHistory>());

            // Act
            var result = await _service.GetAllHistoriesAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public async Task GetAllHistoriesAsync_Exception_ThrowsAndLogs()
        {
            // Arrange
            var exception = new Exception("Database error");
            _historyRepositoryMock
                .Setup(r => r.GetAllHistoriesAsync())
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.GetAllHistoriesAsync());
        }

        #endregion

        #region GetHistoryByIdAsync Tests

        [TestMethod]
        public async Task GetHistoryByIdAsync_HistoryExists_ReturnsHistoryDto()
        {
            // Arrange
            var historyId = Guid.NewGuid();
            var history = CreateTestHistory(historyId, action: "Maintenance");

            _historyRepositoryMock
                .Setup(r => r.GetHistoryByIdAsync(historyId))
                .ReturnsAsync(history);

            _userRepositoryMock
                .Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new List<User>());

            // Act
            var result = await _service.GetHistoryByIdAsync(historyId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(historyId, result.HistoryId);
            Assert.AreEqual("Maintenance", result.Action);
            _historyRepositoryMock.Verify(r => r.GetHistoryByIdAsync(historyId), Times.Once);
        }

        [TestMethod]
        public async Task GetHistoryByIdAsync_HistoryNotFound_ReturnsNull()
        {
            // Arrange
            var historyId = Guid.NewGuid();
            _historyRepositoryMock
                .Setup(r => r.GetHistoryByIdAsync(historyId))
                .ReturnsAsync((AssetMaintenanceHistory?)null);

            // Act
            var result = await _service.GetHistoryByIdAsync(historyId);

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region GetHistoriesByAssetIdAsync Tests

        [TestMethod]
        public async Task GetHistoriesByAssetIdAsync_Success_ReturnsHistories()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var histories = new List<AssetMaintenanceHistory>
            {
                CreateTestHistory(assetId: assetId),
                CreateTestHistory(assetId: assetId)
            };

            _historyRepositoryMock
                .Setup(r => r.GetHistoriesByAssetIdAsync(assetId))
                .ReturnsAsync(histories);

            _userRepositoryMock
                .Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new List<User>());

            // Act
            var result = await _service.GetHistoriesByAssetIdAsync(assetId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.All(h => h.AssetId == assetId));
        }

        #endregion

        #region GetHistoriesByScheduleIdAsync Tests

        [TestMethod]
        public async Task GetHistoriesByScheduleIdAsync_Success_ReturnsHistories()
        {
            // Arrange
            var scheduleId = Guid.NewGuid();
            var histories = new List<AssetMaintenanceHistory>
            {
                CreateTestHistory(scheduleId: scheduleId)
            };

            _historyRepositoryMock
                .Setup(r => r.GetHistoriesByScheduleIdAsync(scheduleId))
                .ReturnsAsync(histories);

            _userRepositoryMock
                .Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new List<User>());

            // Act
            var result = await _service.GetHistoriesByScheduleIdAsync(scheduleId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
        }

        #endregion

        #region CreateHistoryAsync Tests

        [TestMethod]
        public async Task CreateHistoryAsync_Success_WithoutSchedule_ReturnsHistoryDto()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var asset = CreateTestAsset(assetId);
            var createDto = new CreateAssetMaintenanceHistoryDto
            {
                AssetId = assetId,
                Action = "Maintenance",
                ActionDate = DateTime.UtcNow,
                Notes = "Test notes"
            };

            var history = CreateTestHistory(assetId: assetId, action: "Maintenance");

            _assetRepositoryMock
                .Setup(r => r.GetAssetByIdAsync(assetId))
                .ReturnsAsync(asset);

            _historyRepositoryMock
                .Setup(r => r.CreateHistoryAsync(It.IsAny<AssetMaintenanceHistory>()))
                .ReturnsAsync(history);

            _historyRepositoryMock
                .Setup(r => r.GetHistoryByIdAsync(history.HistoryId))
                .ReturnsAsync(history);

            _userRepositoryMock
                .Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new List<User>());

            // Act
            var result = await _service.CreateHistoryAsync(createDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(history.HistoryId, result.HistoryId);
            Assert.AreEqual("Maintenance", result.Action);
            _assetRepositoryMock.Verify(r => r.GetAssetByIdAsync(assetId), Times.Once);
            _historyRepositoryMock.Verify(r => r.CreateHistoryAsync(It.IsAny<AssetMaintenanceHistory>()), Times.Once);
        }

        [TestMethod]
        public async Task CreateHistoryAsync_AssetNotFound_ThrowsArgumentException()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var createDto = new CreateAssetMaintenanceHistoryDto
            {
                AssetId = assetId,
                Action = "Maintenance"
            };

            _assetRepositoryMock
                .Setup(r => r.GetAssetByIdAsync(assetId))
                .ReturnsAsync((Asset?)null);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await _service.CreateHistoryAsync(createDto),
                $"Asset với ID {assetId} không tồn tại");
        }

        [TestMethod]
        public async Task CreateHistoryAsync_ScheduleNotFound_ThrowsArgumentException()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var scheduleId = Guid.NewGuid();
            var asset = CreateTestAsset(assetId);
            var createDto = new CreateAssetMaintenanceHistoryDto
            {
                AssetId = assetId,
                ScheduleId = scheduleId,
                Action = "Maintenance"
            };

            _assetRepositoryMock
                .Setup(r => r.GetAssetByIdAsync(assetId))
                .ReturnsAsync(asset);

            _scheduleRepositoryMock
                .Setup(r => r.GetScheduleByIdAsync(scheduleId))
                .ReturnsAsync((AssetMaintenanceSchedule?)null);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await _service.CreateHistoryAsync(createDto),
                $"Schedule với ID {scheduleId} không tồn tại");
        }

        [TestMethod]
        public async Task CreateHistoryAsync_WithSchedule_UpdatesScheduleStatus()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var scheduleId = Guid.NewGuid();
            var asset = CreateTestAsset(assetId);
            var schedule = CreateTestSchedule(scheduleId, assetId);
            var createDto = new CreateAssetMaintenanceHistoryDto
            {
                AssetId = assetId,
                ScheduleId = scheduleId,
                Action = "Maintenance"
            };

            var history = CreateTestHistory(assetId: assetId, scheduleId: scheduleId);

            _assetRepositoryMock
                .Setup(r => r.GetAssetByIdAsync(assetId))
                .ReturnsAsync(asset);

            _scheduleRepositoryMock
                .Setup(r => r.GetScheduleByIdAsync(scheduleId))
                .ReturnsAsync(schedule);

            _historyRepositoryMock
                .Setup(r => r.CreateHistoryAsync(It.IsAny<AssetMaintenanceHistory>()))
                .ReturnsAsync(history);

            _scheduleRepositoryMock
                .Setup(r => r.UpdateScheduleAsync(It.IsAny<AssetMaintenanceSchedule>()))
                .ReturnsAsync(schedule);

            _historyRepositoryMock
                .Setup(r => r.GetHistoryByIdAsync(history.HistoryId))
                .ReturnsAsync(history);

            _userRepositoryMock
                .Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new List<User>());

            // Act
            var result = await _service.CreateHistoryAsync(createDto);

            // Assert
            Assert.IsNotNull(result);
            _scheduleRepositoryMock.Verify(
                r => r.UpdateScheduleAsync(It.Is<AssetMaintenanceSchedule>(s => s.Status == "DONE")),
                Times.Once);
        }

        [TestMethod]
        public async Task CreateHistoryAsync_WithRecurrence_CreatesNewSchedule()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var scheduleId = Guid.NewGuid();
            var asset = CreateTestAsset(assetId);
            var schedule = CreateTestSchedule(scheduleId, assetId, recurrenceType: "DAILY", recurrenceInterval: 30);
            var createDto = new CreateAssetMaintenanceHistoryDto
            {
                AssetId = assetId,
                ScheduleId = scheduleId,
                Action = "Maintenance"
            };

            var history = CreateTestHistory(assetId: assetId, scheduleId: scheduleId);
            history.NextDueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));

            _assetRepositoryMock
                .Setup(r => r.GetAssetByIdAsync(assetId))
                .ReturnsAsync(asset);

            _scheduleRepositoryMock
                .SetupSequence(r => r.GetScheduleByIdAsync(scheduleId))
                .ReturnsAsync(schedule)  // First call: validate schedule exists
                .ReturnsAsync(schedule)  // Second call: calculate nextDueDate
                .ReturnsAsync(schedule); // Third call: update status

            _historyRepositoryMock
                .Setup(r => r.CreateHistoryAsync(It.IsAny<AssetMaintenanceHistory>()))
                .ReturnsAsync(history);

            _scheduleRepositoryMock
                .Setup(r => r.UpdateScheduleAsync(It.IsAny<AssetMaintenanceSchedule>()))
                .ReturnsAsync(schedule);

            _scheduleRepositoryMock
                .Setup(r => r.CreateScheduleAsync(It.IsAny<AssetMaintenanceSchedule>()))
                .ReturnsAsync(new AssetMaintenanceSchedule());

            _historyRepositoryMock
                .Setup(r => r.GetHistoryByIdAsync(history.HistoryId))
                .ReturnsAsync(history);

            _userRepositoryMock
                .Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new List<User>());

            // Act
            var result = await _service.CreateHistoryAsync(createDto);

            // Assert
            Assert.IsNotNull(result);
            _scheduleRepositoryMock.Verify(r => r.CreateScheduleAsync(It.IsAny<AssetMaintenanceSchedule>()), Times.Once);
        }

        #endregion

        #region UpdateHistoryAsync Tests

        [TestMethod]
        public async Task UpdateHistoryAsync_Success_ReturnsUpdatedHistoryDto()
        {
            // Arrange
            var historyId = Guid.NewGuid();
            var existingHistory = CreateTestHistory(historyId, action: "Old Action");
            var updateDto = new UpdateAssetMaintenanceHistoryDto
            {
                Action = "Updated Action",
                CostAmount = 50000,
                Notes = "Updated notes"
            };

            var updatedHistory = CreateTestHistory(historyId, action: "Updated Action", costAmount: 50000);

            _historyRepositoryMock
                .Setup(r => r.GetHistoryByIdAsync(historyId))
                .ReturnsAsync(existingHistory);

            _historyRepositoryMock
                .Setup(r => r.UpdateHistoryAsync(It.IsAny<AssetMaintenanceHistory>()))
                .ReturnsAsync(updatedHistory);

            _historyRepositoryMock
                .SetupSequence(r => r.GetHistoryByIdAsync(historyId))
                .ReturnsAsync(existingHistory)
                .ReturnsAsync(updatedHistory);

            _userRepositoryMock
                .Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new List<User>());

            // Act
            var result = await _service.UpdateHistoryAsync(updateDto, historyId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(historyId, result.HistoryId);
            Assert.AreEqual("Updated Action", result.Action);
            _historyRepositoryMock.Verify(r => r.GetHistoryByIdAsync(historyId), Times.AtLeastOnce);
            _historyRepositoryMock.Verify(r => r.UpdateHistoryAsync(It.IsAny<AssetMaintenanceHistory>()), Times.Once);
        }

        [TestMethod]
        public async Task UpdateHistoryAsync_HistoryNotFound_ReturnsNull()
        {
            // Arrange
            var historyId = Guid.NewGuid();
            var updateDto = new UpdateAssetMaintenanceHistoryDto
            {
                Action = "Updated Action"
            };

            _historyRepositoryMock
                .Setup(r => r.GetHistoryByIdAsync(historyId))
                .ReturnsAsync((AssetMaintenanceHistory?)null);

            // Act
            var result = await _service.UpdateHistoryAsync(updateDto, historyId);

            // Assert
            Assert.IsNull(result);
            _historyRepositoryMock.Verify(r => r.UpdateHistoryAsync(It.IsAny<AssetMaintenanceHistory>()), Times.Never);
        }

        [TestMethod]
        public async Task UpdateHistoryAsync_PartialUpdate_UpdatesOnlyProvidedFields()
        {
            // Arrange
            var historyId = Guid.NewGuid();
            var existingHistory = CreateTestHistory(historyId, action: "Old Action", costAmount: 10000);
            var updateDto = new UpdateAssetMaintenanceHistoryDto
            {
                Action = "Updated Action"
                // Only updating Action, not CostAmount
            };

            var updatedHistory = CreateTestHistory(historyId, action: "Updated Action", costAmount: 10000);

            _historyRepositoryMock
                .Setup(r => r.GetHistoryByIdAsync(historyId))
                .ReturnsAsync(existingHistory);

            _historyRepositoryMock
                .Setup(r => r.UpdateHistoryAsync(It.IsAny<AssetMaintenanceHistory>()))
                .ReturnsAsync(updatedHistory);

            _historyRepositoryMock
                .SetupSequence(r => r.GetHistoryByIdAsync(historyId))
                .ReturnsAsync(existingHistory)
                .ReturnsAsync(updatedHistory);

            _userRepositoryMock
                .Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new List<User>());

            // Act
            var result = await _service.UpdateHistoryAsync(updateDto, historyId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Updated Action", result.Action);
            // CostAmount should remain unchanged
        }

        [TestMethod]
        public async Task UpdateHistoryAsync_Exception_ThrowsAndLogs()
        {
            // Arrange
            var historyId = Guid.NewGuid();
            var existingHistory = CreateTestHistory(historyId);
            var updateDto = new UpdateAssetMaintenanceHistoryDto
            {
                Action = "Updated Action"
            };

            var exception = new Exception("Database error");

            _historyRepositoryMock
                .Setup(r => r.GetHistoryByIdAsync(historyId))
                .ReturnsAsync(existingHistory);

            _historyRepositoryMock
                .Setup(r => r.UpdateHistoryAsync(It.IsAny<AssetMaintenanceHistory>()))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.UpdateHistoryAsync(updateDto, historyId));
        }

        #endregion
    }
}
