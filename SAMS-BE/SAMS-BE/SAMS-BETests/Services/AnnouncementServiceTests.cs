using AutoMapper;
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

namespace SAMS_BE.Services.Tests
{
    [TestClass]
    public class AnnouncementServiceTests
    {
        private Mock<IAnnouncementRepository> _announcementRepositoryMock = null!;
        private Mock<IMapper> _mapperMock = null!;
        private AnnouncementService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _announcementRepositoryMock = new Mock<IAnnouncementRepository>();
            _mapperMock = new Mock<IMapper>();
            _service = new AnnouncementService(_announcementRepositoryMock.Object, _mapperMock.Object);
        }

        #region Helper Methods

        private Announcement CreateTestAnnouncement(
            Guid? announcementId = null,
            string? title = null,
            string? status = null,
            DateTime? visibleFrom = null,
            DateTime? visibleTo = null,
            string? type = null)
        {
            var now = DateTime.UtcNow.AddHours(7);
            return new Announcement
            {
                AnnouncementId = announcementId ?? Guid.NewGuid(),
                Title = title ?? "Test Announcement",
                Content = "Test Content",
                VisibleFrom = visibleFrom ?? now.Date,
                VisibleTo = visibleTo,
                VisibilityScope = "ALL",
                Status = status ?? "ACTIVE",
                IsPinned = false,
                Type = type,
                CreatedAt = now,
                CreatedBy = "TestUser"
            };
        }

        private AnnouncementResponseDto CreateTestAnnouncementResponseDto(
            Guid? announcementId = null,
            bool isActive = true,
            bool isRead = false)
        {
            var now = DateTime.UtcNow.AddHours(7);
            return new AnnouncementResponseDto
            {
                AnnouncementId = announcementId ?? Guid.NewGuid(),
                Title = "Test Announcement",
                Content = "Test Content",
                VisibleFrom = now.Date,
                VisibleTo = null,
                VisibilityScope = "ALL",
                Status = "ACTIVE",
                IsPinned = false,
                Type = null,
                CreatedAt = now,
                CreatedBy = "TestUser",
                IsActive = isActive,
                IsRead = isRead
            };
        }

        private CreateAnnouncementDto CreateTestCreateAnnouncementDto()
        {
            var now = DateTime.UtcNow.AddHours(7);
            return new CreateAnnouncementDto
            {
                Title = "New Announcement",
                Content = "New Content",
                VisibleFrom = now.Date,
                VisibleTo = now.Date.AddDays(7),
                VisibilityScope = "ALL",
                Status = "ACTIVE",
                IsPinned = false,
                Type = null
            };
        }

        private UpdateAnnouncementDto CreateTestUpdateAnnouncementDto(Guid announcementId)
        {
            var now = DateTime.UtcNow.AddHours(7);
            return new UpdateAnnouncementDto
            {
                AnnouncementId = announcementId,
                Title = "Updated Announcement",
                Content = "Updated Content",
                VisibleFrom = now.Date,
                VisibleTo = now.Date.AddDays(7),
                VisibilityScope = "ALL",
                Status = "ACTIVE",
                IsPinned = false,
                Type = null
            };
        }

        #endregion

        #region GetAnnouncementByIdAsync Tests

        [TestMethod]
        public async Task GetAnnouncementByIdAsync_AnnouncementExists_ReturnsAnnouncementResponseDto()
        {
            // Arrange
            var announcementId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var announcement = CreateTestAnnouncement(announcementId);
            var responseDto = CreateTestAnnouncementResponseDto(announcementId);

            _announcementRepositoryMock
                .Setup(r => r.GetAnnouncementByIdAsync(announcementId))
                .ReturnsAsync(announcement);

            _mapperMock
                .Setup(m => m.Map<AnnouncementResponseDto>(announcement))
                .Returns(responseDto);

            _announcementRepositoryMock
                .Setup(r => r.IsAnnouncementReadByUserAsync(announcementId, userId))
                .ReturnsAsync(false);

            // Act
            var result = await _service.GetAnnouncementByIdAsync(announcementId, userId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(announcementId, result.AnnouncementId);
            Assert.IsFalse(result.IsRead);
            _announcementRepositoryMock.Verify(r => r.GetAnnouncementByIdAsync(announcementId), Times.Once);
            _announcementRepositoryMock.Verify(r => r.IsAnnouncementReadByUserAsync(announcementId, userId), Times.Once);
        }

        [TestMethod]
        public async Task GetAnnouncementByIdAsync_AnnouncementNotFound_ReturnsNull()
        {
            // Arrange
            var announcementId = Guid.NewGuid();
            _announcementRepositoryMock
                .Setup(r => r.GetAnnouncementByIdAsync(announcementId))
                .ReturnsAsync((Announcement?)null);

            // Act
            var result = await _service.GetAnnouncementByIdAsync(announcementId);

            // Assert
            Assert.IsNull(result);
            _announcementRepositoryMock.Verify(r => r.GetAnnouncementByIdAsync(announcementId), Times.Once);
        }

        [TestMethod]
        public async Task GetAnnouncementByIdAsync_WithoutUserId_DoesNotCheckReadStatus()
        {
            // Arrange
            var announcementId = Guid.NewGuid();
            var announcement = CreateTestAnnouncement(announcementId);
            var responseDto = CreateTestAnnouncementResponseDto(announcementId);

            _announcementRepositoryMock
                .Setup(r => r.GetAnnouncementByIdAsync(announcementId))
                .ReturnsAsync(announcement);

            _mapperMock
                .Setup(m => m.Map<AnnouncementResponseDto>(announcement))
                .Returns(responseDto);

            // Act
            var result = await _service.GetAnnouncementByIdAsync(announcementId, null);

            // Assert
            Assert.IsNotNull(result);
            _announcementRepositoryMock.Verify(r => r.IsAnnouncementReadByUserAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        }

        [TestMethod]
        public async Task GetAnnouncementByIdAsync_Exception_ThrowsException()
        {
            // Arrange
            var announcementId = Guid.NewGuid();
            var exception = new Exception("Database error");
            _announcementRepositoryMock
                .Setup(r => r.GetAnnouncementByIdAsync(announcementId))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.GetAnnouncementByIdAsync(announcementId));
        }

        #endregion

        #region GetAllAnnouncementsAsync Tests

        [TestMethod]
        public async Task GetAllAnnouncementsAsync_Success_ReturnsPaginatedList()
        {
            // Arrange
            var announcements = new List<Announcement>
            {
                CreateTestAnnouncement(),
                CreateTestAnnouncement(),
                CreateTestAnnouncement()
            };

            _announcementRepositoryMock
                .Setup(r => r.GetAllAnnouncementsAsync(null))
                .ReturnsAsync(announcements);

            _mapperMock
                .Setup(m => m.Map<AnnouncementResponseDto>(It.IsAny<Announcement>()))
                .Returns((Announcement a) => CreateTestAnnouncementResponseDto(a.AnnouncementId));

            // Act
            var result = await _service.GetAllAnnouncementsAsync(1, 10, null);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.TotalCount);
            Assert.AreEqual(1, result.PageNumber);
            Assert.AreEqual(10, result.PageSize);
            Assert.AreEqual(3, result.Announcements.Count);
            _announcementRepositoryMock.Verify(r => r.GetAllAnnouncementsAsync(null), Times.Once);
        }

        [TestMethod]
        public async Task GetAllAnnouncementsAsync_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var announcements = new List<Announcement>();
            for (int i = 0; i < 25; i++)
            {
                announcements.Add(CreateTestAnnouncement());
            }

            _announcementRepositoryMock
                .Setup(r => r.GetAllAnnouncementsAsync(null))
                .ReturnsAsync(announcements);

            _mapperMock
                .Setup(m => m.Map<AnnouncementResponseDto>(It.IsAny<Announcement>()))
                .Returns((Announcement a) => CreateTestAnnouncementResponseDto(a.AnnouncementId));

            // Act
            var result = await _service.GetAllAnnouncementsAsync(2, 10, null);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(25, result.TotalCount);
            Assert.AreEqual(2, result.PageNumber);
            Assert.AreEqual(10, result.PageSize);
            Assert.AreEqual(10, result.Announcements.Count);
        }

        [TestMethod]
        public async Task GetAllAnnouncementsAsync_WithExcludeTypes_FiltersCorrectly()
        {
            // Arrange
            var excludeTypes = new List<string> { "MAINTENANCE_REMINDER", "MAINTENANCE_ASSIGNMENT" };
            var announcements = new List<Announcement>
            {
                CreateTestAnnouncement(type: "GENERAL"),
                CreateTestAnnouncement(type: "GENERAL")
            };

            _announcementRepositoryMock
                .Setup(r => r.GetAllAnnouncementsAsync(excludeTypes))
                .ReturnsAsync(announcements);

            _mapperMock
                .Setup(m => m.Map<AnnouncementResponseDto>(It.IsAny<Announcement>()))
                .Returns((Announcement a) => CreateTestAnnouncementResponseDto(a.AnnouncementId));

            // Act
            var result = await _service.GetAllAnnouncementsAsync(1, 10, excludeTypes);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.TotalCount);
            _announcementRepositoryMock.Verify(r => r.GetAllAnnouncementsAsync(excludeTypes), Times.Once);
        }

        [TestMethod]
        public async Task GetAllAnnouncementsAsync_Exception_ThrowsException()
        {
            // Arrange
            var exception = new Exception("Database error");
            _announcementRepositoryMock
                .Setup(r => r.GetAllAnnouncementsAsync(null))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.GetAllAnnouncementsAsync());
        }

        #endregion

        #region GetActiveAnnouncementsAsync Tests

        [TestMethod]
        public async Task GetActiveAnnouncementsAsync_Success_ReturnsActiveAnnouncements()
        {
            // Arrange
            var announcements = new List<Announcement>
            {
                CreateTestAnnouncement(),
                CreateTestAnnouncement()
            };

            _announcementRepositoryMock
                .Setup(r => r.GetActiveAnnouncementsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(announcements);

            _mapperMock
                .Setup(m => m.Map<AnnouncementResponseDto>(It.IsAny<Announcement>()))
                .Returns((Announcement a) => CreateTestAnnouncementResponseDto(a.AnnouncementId, isActive: true));

            // Act
            var result = await _service.GetActiveAnnouncementsAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.All(a => a.IsActive));
            _announcementRepositoryMock.Verify(r => r.GetActiveAnnouncementsAsync(It.IsAny<DateTime>()), Times.Once);
        }

        [TestMethod]
        public async Task GetActiveAnnouncementsAsync_Exception_ThrowsException()
        {
            // Arrange
            var exception = new Exception("Database error");
            _announcementRepositoryMock
                .Setup(r => r.GetActiveAnnouncementsAsync(It.IsAny<DateTime>()))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.GetActiveAnnouncementsAsync());
        }

        #endregion

        #region GetAnnouncementsByDateRangeAsync Tests

        [TestMethod]
        public async Task GetAnnouncementsByDateRangeAsync_Success_ReturnsAnnouncementsInRange()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddHours(7).Date;
            var endDate = startDate.AddDays(7);
            var announcements = new List<Announcement>
            {
                CreateTestAnnouncement(visibleFrom: startDate),
                CreateTestAnnouncement(visibleFrom: startDate.AddDays(1))
            };

            _announcementRepositoryMock
                .Setup(r => r.GetAnnouncementsByDateRangeAsync(startDate, endDate))
                .ReturnsAsync(announcements);

            _mapperMock
                .Setup(m => m.Map<AnnouncementResponseDto>(It.IsAny<Announcement>()))
                .Returns((Announcement a) => CreateTestAnnouncementResponseDto(a.AnnouncementId));

            // Act
            var result = await _service.GetAnnouncementsByDateRangeAsync(startDate, endDate);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            _announcementRepositoryMock.Verify(r => r.GetAnnouncementsByDateRangeAsync(startDate, endDate), Times.Once);
        }

        [TestMethod]
        public async Task GetAnnouncementsByDateRangeAsync_Exception_ThrowsException()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddHours(7).Date;
            var endDate = startDate.AddDays(7);
            var exception = new Exception("Database error");
            _announcementRepositoryMock
                .Setup(r => r.GetAnnouncementsByDateRangeAsync(startDate, endDate))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.GetAnnouncementsByDateRangeAsync(startDate, endDate));
        }

        #endregion

        #region GetAnnouncementsByVisibilityScopeAsync Tests

        [TestMethod]
        public async Task GetAnnouncementsByVisibilityScopeAsync_Success_ReturnsAnnouncementsByScope()
        {
            // Arrange
            var scope = "RESIDENTS";
            var announcements = new List<Announcement>
            {
                CreateTestAnnouncement(),
                CreateTestAnnouncement()
            };

            _announcementRepositoryMock
                .Setup(r => r.GetAnnouncementsByVisibilityScopeAsync(scope))
                .ReturnsAsync(announcements);

            _mapperMock
                .Setup(m => m.Map<AnnouncementResponseDto>(It.IsAny<Announcement>()))
                .Returns((Announcement a) => CreateTestAnnouncementResponseDto(a.AnnouncementId));

            // Act
            var result = await _service.GetAnnouncementsByVisibilityScopeAsync(scope);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            _announcementRepositoryMock.Verify(r => r.GetAnnouncementsByVisibilityScopeAsync(scope), Times.Once);
        }

        [TestMethod]
        public async Task GetAnnouncementsByVisibilityScopeAsync_Exception_ThrowsException()
        {
            // Arrange
            var scope = "RESIDENTS";
            var exception = new Exception("Database error");
            _announcementRepositoryMock
                .Setup(r => r.GetAnnouncementsByVisibilityScopeAsync(scope))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.GetAnnouncementsByVisibilityScopeAsync(scope));
        }

        #endregion

        #region CreateAnnouncementAsync Tests

        [TestMethod]
        public async Task CreateAnnouncementAsync_VisibleToBeforeVisibleFrom_ThrowsException()
        {
            // Arrange
            var now = DateTime.UtcNow.AddHours(7).Date;
            var createDto = CreateTestCreateAnnouncementDto();
            createDto.VisibleFrom = now.AddDays(5);
            createDto.VisibleTo = now.AddDays(3);

            // Act & Assert
            var exception = await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.CreateAnnouncementAsync(createDto));
            Assert.IsTrue(exception.Message.Contains("VisibleTo phải bằng hoặc sau VisibleFrom") || 
                         exception.Message.Contains("ArgumentException"));
        }

        [TestMethod]
        public async Task CreateAnnouncementAsync_Exception_ThrowsException()
        {
            // Arrange
            var createDto = CreateTestCreateAnnouncementDto();
            var exception = new Exception("Database error");
            _announcementRepositoryMock
                .Setup(r => r.CreateAnnouncementAsync(It.IsAny<Announcement>()))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.CreateAnnouncementAsync(createDto));
        }

        #endregion

        #region UpdateAnnouncementAsync Tests

        [TestMethod]
        public async Task UpdateAnnouncementAsync_AnnouncementNotFound_ReturnsNull()
        {
            // Arrange
            var announcementId = Guid.NewGuid();
            var updateDto = CreateTestUpdateAnnouncementDto(announcementId);

            _announcementRepositoryMock
                .Setup(r => r.GetAnnouncementByIdAsync(announcementId))
                .ReturnsAsync((Announcement?)null);

            // Act
            var result = await _service.UpdateAnnouncementAsync(updateDto);

            // Assert
            Assert.IsNull(result);
            _announcementRepositoryMock.Verify(r => r.UpdateAnnouncementAsync(It.IsAny<Announcement>()), Times.Never);
        }
        [TestMethod]
        public async Task UpdateAnnouncementAsync_VisibleToBeforeVisibleFrom_ThrowsException()
        {
            // Arrange
            var announcementId = Guid.NewGuid();
            var existingAnnouncement = CreateTestAnnouncement(announcementId);
            var now = DateTime.UtcNow.AddHours(7).Date;
            var updateDto = CreateTestUpdateAnnouncementDto(announcementId);
            updateDto.VisibleFrom = now.AddDays(5);
            updateDto.VisibleTo = now.AddDays(3);

            _announcementRepositoryMock
                .Setup(r => r.GetAnnouncementByIdAsync(announcementId))
                .ReturnsAsync(existingAnnouncement);

            // Act & Assert
            var exception = await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.UpdateAnnouncementAsync(updateDto));
            Assert.IsTrue(exception.Message.Contains("VisibleTo phải bằng hoặc sau VisibleFrom") || 
                         exception.Message.Contains("ArgumentException"));
        }

        [TestMethod]
        public async Task UpdateAnnouncementAsync_Exception_ThrowsException()
        {
            // Arrange
            var announcementId = Guid.NewGuid();
            var updateDto = CreateTestUpdateAnnouncementDto(announcementId);
            var exception = new Exception("Database error");
            _announcementRepositoryMock
                .Setup(r => r.GetAnnouncementByIdAsync(announcementId))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.UpdateAnnouncementAsync(updateDto));
        }

        #endregion

        #region DeleteAnnouncementAsync Tests

        [TestMethod]
        public async Task DeleteAnnouncementAsync_AnnouncementExists_ReturnsTrue()
        {
            // Arrange
            var announcementId = Guid.NewGuid();
            _announcementRepositoryMock
                .Setup(r => r.DeleteAnnouncementAsync(announcementId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteAnnouncementAsync(announcementId);

            // Assert
            Assert.IsTrue(result);
            _announcementRepositoryMock.Verify(r => r.DeleteAnnouncementAsync(announcementId), Times.Once);
        }

        [TestMethod]
        public async Task DeleteAnnouncementAsync_AnnouncementNotFound_ReturnsFalse()
        {
            // Arrange
            var announcementId = Guid.NewGuid();
            _announcementRepositoryMock
                .Setup(r => r.DeleteAnnouncementAsync(announcementId))
                .ReturnsAsync(false);

            // Act
            var result = await _service.DeleteAnnouncementAsync(announcementId);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task DeleteAnnouncementAsync_Exception_ThrowsException()
        {
            // Arrange
            var announcementId = Guid.NewGuid();
            var exception = new Exception("Database error");
            _announcementRepositoryMock
                .Setup(r => r.DeleteAnnouncementAsync(announcementId))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.DeleteAnnouncementAsync(announcementId));
        }

        #endregion

        #region GetUnreadAnnouncementsForUserAsync Tests

        [TestMethod]
        public async Task GetUnreadAnnouncementsForUserAsync_Success_ReturnsUnreadAnnouncements()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var announcements = new List<Announcement>
            {
                CreateTestAnnouncement(),
                CreateTestAnnouncement()
            };

            _announcementRepositoryMock
                .Setup(r => r.GetUnreadAnnouncementsForUserAsync(userId, null, null))
                .ReturnsAsync(announcements);

            _mapperMock
                .Setup(m => m.Map<AnnouncementResponseDto>(It.IsAny<Announcement>()))
                .Returns((Announcement a) => CreateTestAnnouncementResponseDto(a.AnnouncementId, isRead: false));

            // Act
            var result = await _service.GetUnreadAnnouncementsForUserAsync(userId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.All(a => !a.IsRead));
            _announcementRepositoryMock.Verify(r => r.GetUnreadAnnouncementsForUserAsync(userId, null, null), Times.Once);
        }

        [TestMethod]
        public async Task GetUnreadAnnouncementsForUserAsync_WithScopeAndTypes_FiltersCorrectly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var scope = "RESIDENTS";
            var includeTypes = new List<string> { "MAINTENANCE_REMINDER" };
            var announcements = new List<Announcement>
            {
                CreateTestAnnouncement(type: "MAINTENANCE_REMINDER")
            };

            _announcementRepositoryMock
                .Setup(r => r.GetUnreadAnnouncementsForUserAsync(userId, scope, includeTypes))
                .ReturnsAsync(announcements);

            _mapperMock
                .Setup(m => m.Map<AnnouncementResponseDto>(It.IsAny<Announcement>()))
                .Returns((Announcement a) => CreateTestAnnouncementResponseDto(a.AnnouncementId, isRead: false));

            // Act
            var result = await _service.GetUnreadAnnouncementsForUserAsync(userId, scope, includeTypes);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            _announcementRepositoryMock.Verify(r => r.GetUnreadAnnouncementsForUserAsync(userId, scope, includeTypes), Times.Once);
        }

        [TestMethod]
        public async Task GetUnreadAnnouncementsForUserAsync_Exception_ThrowsException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var exception = new Exception("Database error");
            _announcementRepositoryMock
                .Setup(r => r.GetUnreadAnnouncementsForUserAsync(userId, null, null))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.GetUnreadAnnouncementsForUserAsync(userId));
        }

        #endregion

        #region GetUnreadAnnouncementCountForUserAsync Tests

        [TestMethod]
        public async Task GetUnreadAnnouncementCountForUserAsync_Success_ReturnsCount()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var count = 5;
            _announcementRepositoryMock
                .Setup(r => r.GetUnreadAnnouncementCountForUserAsync(userId, null, null))
                .ReturnsAsync(count);

            // Act
            var result = await _service.GetUnreadAnnouncementCountForUserAsync(userId);

            // Assert
            Assert.AreEqual(count, result);
            _announcementRepositoryMock.Verify(r => r.GetUnreadAnnouncementCountForUserAsync(userId, null, null), Times.Once);
        }

        [TestMethod]
        public async Task GetUnreadAnnouncementCountForUserAsync_WithScopeAndTypes_FiltersCorrectly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var scope = "RESIDENTS";
            var includeTypes = new List<string> { "MAINTENANCE_REMINDER" };
            var count = 3;
            _announcementRepositoryMock
                .Setup(r => r.GetUnreadAnnouncementCountForUserAsync(userId, scope, includeTypes))
                .ReturnsAsync(count);

            // Act
            var result = await _service.GetUnreadAnnouncementCountForUserAsync(userId, scope, includeTypes);

            // Assert
            Assert.AreEqual(count, result);
            _announcementRepositoryMock.Verify(r => r.GetUnreadAnnouncementCountForUserAsync(userId, scope, includeTypes), Times.Once);
        }

        [TestMethod]
        public async Task GetUnreadAnnouncementCountForUserAsync_Exception_ThrowsException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var exception = new Exception("Database error");
            _announcementRepositoryMock
                .Setup(r => r.GetUnreadAnnouncementCountForUserAsync(userId, null, null))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.GetUnreadAnnouncementCountForUserAsync(userId));
        }

        #endregion

        #region MarkAnnouncementAsReadAsync Tests

        [TestMethod]
        public async Task MarkAnnouncementAsReadAsync_Success_ReturnsTrue()
        {
            // Arrange
            var announcementId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            _announcementRepositoryMock
                .Setup(r => r.MarkAnnouncementAsReadAsync(announcementId, userId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.MarkAnnouncementAsReadAsync(announcementId, userId);

            // Assert
            Assert.IsTrue(result);
            _announcementRepositoryMock.Verify(r => r.MarkAnnouncementAsReadAsync(announcementId, userId), Times.Once);
        }

        [TestMethod]
        public async Task MarkAnnouncementAsReadAsync_AnnouncementNotFound_ReturnsFalse()
        {
            // Arrange
            var announcementId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            _announcementRepositoryMock
                .Setup(r => r.MarkAnnouncementAsReadAsync(announcementId, userId))
                .ReturnsAsync(false);

            // Act
            var result = await _service.MarkAnnouncementAsReadAsync(announcementId, userId);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MarkAnnouncementAsReadAsync_Exception_ThrowsException()
        {
            // Arrange
            var announcementId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var exception = new Exception("Database error");
            _announcementRepositoryMock
                .Setup(r => r.MarkAnnouncementAsReadAsync(announcementId, userId))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.MarkAnnouncementAsReadAsync(announcementId, userId));
        }

        #endregion
    }
}
