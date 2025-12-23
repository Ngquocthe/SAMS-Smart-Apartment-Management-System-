using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IRepository;
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
    public class CardHistoryServiceTests
    {
        private Mock<ICardHistoryRepository> _repoMock = null!;
        private Mock<ILogger<CardHistoryService>> _loggerMock = null!;
        private BuildingManagementContext _db = null!;
        private CardHistoryService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _repoMock = new Mock<ICardHistoryRepository>();
            _loggerMock = new Mock<ILogger<CardHistoryService>>();

            var options = new DbContextOptionsBuilder<BuildingManagementContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _db = new BuildingManagementContext(options, new TenantContextAccessor());
            _service = new CardHistoryService(_repoMock.Object, _loggerMock.Object, _db);
        }

        private static CardHistory CreateCardHistory(
            Guid? id = null,
            string? createdBy = "testuser")
        {
            return new CardHistory
            {
                CardHistoryId = id ?? Guid.NewGuid(),
                CardId = Guid.NewGuid(),
                EventCode = "CARD_CREATED",
                EventTimeUtc = DateTime.UtcNow,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                IsDelete = false
            };
        }

        private async Task SeedUserAsync(string username, string email, string firstName = "Test", string lastName = "User")
        {
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Username = username,
                Email = email,
                Phone = "0123456789",
                FirstName = firstName,
                LastName = lastName
            };
            await _db.Users.AddAsync(user);
            await _db.SaveChangesAsync();
        }

        #region GetCardHistoriesAsync

        [TestMethod]
        public async Task GetCardHistoriesAsync_PopulatesCreatedByName_FromUser()
        {
            // Arrange
            var history = CreateCardHistory(createdBy: "test@example.com");
            _repoMock
                .Setup(r => r.GetCardHistoriesAsync(It.IsAny<CardHistoryQueryDto>()))
                .ReturnsAsync(new List<CardHistory> { history });

            await SeedUserAsync("testuser", "test@example.com", "Nguyen", "A");

            var query = new CardHistoryQueryDto();

            // Act
            var result = (await _service.GetCardHistoriesAsync(query)).ToList();

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Nguyen A", result[0].CreatedByName);
        }

        [TestMethod]
        public async Task GetCardHistoriesAsync_CreatedBySystem_UsesDefaultName_WhenNoUserFound()
        {
            // Arrange
            var history = CreateCardHistory(createdBy: "System");
            _repoMock
                .Setup(r => r.GetCardHistoriesAsync(It.IsAny<CardHistoryQueryDto>()))
                .ReturnsAsync(new List<CardHistory> { history });

            var query = new CardHistoryQueryDto();

            // Act
            var result = (await _service.GetCardHistoriesAsync(query)).ToList();

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("building management", result[0].CreatedByName);
        }

        #endregion

        #region GetCardHistoryByIdAsync

        [TestMethod]
        public async Task GetCardHistoryByIdAsync_NotFound_ReturnsNull()
        {
            _repoMock
                .Setup(r => r.GetCardHistoryByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((CardHistory?)null);

            var result = await _service.GetCardHistoryByIdAsync(Guid.NewGuid());

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetCardHistoryByIdAsync_Found_PopulatesCreatedByName()
        {
            var history = CreateCardHistory(createdBy: "testuser");
            _repoMock
                .Setup(r => r.GetCardHistoryByIdAsync(history.CardHistoryId))
                .ReturnsAsync(history);

            await SeedUserAsync("testuser", "test@example.com", "Nguyen", "B");

            var result = await _service.GetCardHistoryByIdAsync(history.CardHistoryId);

            Assert.IsNotNull(result);
            Assert.AreEqual("Nguyen B", result!.CreatedByName);
        }

        #endregion

        #region Simple passthrough methods

        [TestMethod]
        public async Task GetCardHistoriesByCardIdAsync_CallsRepositoryAndPopulatesNames()
        {
            var cardId = Guid.NewGuid();
            var history = CreateCardHistory(createdBy: "test@example.com");

            _repoMock
                .Setup(r => r.GetCardHistoriesByCardIdAsync(cardId))
                .ReturnsAsync(new List<CardHistory> { history });

            await SeedUserAsync("testuser", "test@example.com", "Nguyen", "C");

            var result = (await _service.GetCardHistoriesByCardIdAsync(cardId)).ToList();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Nguyen C", result[0].CreatedByName);
        }

        [TestMethod]
        public async Task GetCardHistoriesByUserIdAsync_CallsRepository()
        {
            var userId = Guid.NewGuid();
            var history = CreateCardHistory();

            _repoMock
                .Setup(r => r.GetCardHistoriesByUserIdAsync(userId))
                .ReturnsAsync(new List<CardHistory> { history });

            var result = (await _service.GetCardHistoriesByUserIdAsync(userId)).ToList();

            Assert.AreEqual(1, result.Count);
            _repoMock.Verify(r => r.GetCardHistoriesByUserIdAsync(userId), Times.Once);
        }

        [TestMethod]
        public async Task GetCardHistoriesByApartmentIdAsync_CallsRepository()
        {
            var apartmentId = Guid.NewGuid();
            var history = CreateCardHistory();

            _repoMock
                .Setup(r => r.GetCardHistoriesByApartmentIdAsync(apartmentId))
                .ReturnsAsync(new List<CardHistory> { history });

            var result = (await _service.GetCardHistoriesByApartmentIdAsync(apartmentId)).ToList();

            Assert.AreEqual(1, result.Count);
            _repoMock.Verify(r => r.GetCardHistoriesByApartmentIdAsync(apartmentId), Times.Once);
        }

        [TestMethod]
        public async Task GetCardHistoriesByFieldNameAsync_CallsRepository()
        {
            var history = CreateCardHistory();

            _repoMock
                .Setup(r => r.GetCardHistoriesByFieldNameAsync("Status"))
                .ReturnsAsync(new List<CardHistory> { history });

            var result = (await _service.GetCardHistoriesByFieldNameAsync("Status")).ToList();

            Assert.AreEqual(1, result.Count);
            _repoMock.Verify(r => r.GetCardHistoriesByFieldNameAsync("Status"), Times.Once);
        }

        [TestMethod]
        public async Task GetCardHistoriesByEventCodeAsync_CallsRepository()
        {
            var history = CreateCardHistory();

            _repoMock
                .Setup(r => r.GetCardHistoriesByEventCodeAsync("CARD_CREATED"))
                .ReturnsAsync(new List<CardHistory> { history });

            var result = (await _service.GetCardHistoriesByEventCodeAsync("CARD_CREATED")).ToList();

            Assert.AreEqual(1, result.Count);
            _repoMock.Verify(r => r.GetCardHistoriesByEventCodeAsync("CARD_CREATED"), Times.Once);
        }

        [TestMethod]
        public async Task GetCardHistoriesByDateRangeAsync_CallsRepository()
        {
            var from = DateTime.UtcNow.AddDays(-1);
            var to = DateTime.UtcNow;
            var history = CreateCardHistory();

            _repoMock
                .Setup(r => r.GetCardHistoriesByDateRangeAsync(from, to))
                .ReturnsAsync(new List<CardHistory> { history });

            var result = (await _service.GetCardHistoriesByDateRangeAsync(from, to)).ToList();

            Assert.AreEqual(1, result.Count);
            _repoMock.Verify(r => r.GetCardHistoriesByDateRangeAsync(from, to), Times.Once);
        }

        #endregion

        #region Create / Update / Delete

        [TestMethod]
        public async Task CreateCardHistoryAsync_CallsRepositoryAndPopulatesName()
        {
            var dto = new CreateCardHistoryDto
            {
                CardId = Guid.NewGuid(),
                EventCode = "CARD_CREATED",
                CreatedBy = "test@example.com"
            };

            var entity = CreateCardHistory(createdBy: dto.CreatedBy);
            _repoMock
                .Setup(r => r.CreateCardHistoryAsync(dto))
                .ReturnsAsync(entity);

            await SeedUserAsync("testuser", "test@example.com", "Nguyen", "D");

            var result = await _service.CreateCardHistoryAsync(dto);

            Assert.AreEqual("Nguyen D", result.CreatedByName);
        }

        [TestMethod]
        public async Task UpdateCardHistoryAsync_DelegatesToRepository()
        {
            var id = Guid.NewGuid();
            var dto = new CreateCardHistoryDto { CardId = Guid.NewGuid(), EventCode = "CARD_CREATED" };

            _repoMock
                .Setup(r => r.UpdateCardHistoryAsync(id, dto))
                .ReturnsAsync(true);

            var result = await _service.UpdateCardHistoryAsync(id, dto);

            Assert.IsTrue(result);
            _repoMock.Verify(r => r.UpdateCardHistoryAsync(id, dto), Times.Once);
        }

        [TestMethod]
        public async Task SoftDeleteCardHistoryAsync_DelegatesToRepository()
        {
            var id = Guid.NewGuid();

            _repoMock
                .Setup(r => r.SoftDeleteCardHistoryAsync(id))
                .ReturnsAsync(true);

            var result = await _service.SoftDeleteCardHistoryAsync(id);

            Assert.IsTrue(result);
            _repoMock.Verify(r => r.SoftDeleteCardHistoryAsync(id), Times.Once);
        }

        #endregion

        #region Paged and Summary

        [TestMethod]
        public async Task GetPagedCardHistoriesAsync_MapsPagedResultAndPopulatesNames()
        {
            var query = new CardHistoryQueryDto();
            var history = CreateCardHistory(createdBy: "test@example.com");

            var paged = new PagedResult<CardHistory>
            {
                Items = new List<CardHistory> { history },
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 10,
                TotalPages = 1,
                TotalItems = 1
            };

            _repoMock
                .Setup(r => r.GetPagedCardHistoriesAsync(query))
                .ReturnsAsync(paged);

            await SeedUserAsync("testuser", "test@example.com", "Nguyen", "E");

            var result = await _service.GetPagedCardHistoriesAsync(query);

            Assert.AreEqual(1, result.Items.Count());
            Assert.AreEqual(1, result.TotalCount);
            Assert.AreEqual("Nguyen E", result.Items.First().CreatedByName);
        }

        [TestMethod]
        public async Task GetCardAccessSummaryAsync_DelegatesToRepository()
        {
            var cardId = Guid.NewGuid();
            var summary = new List<CardAccessSummaryDto>
            {
                new CardAccessSummaryDto
                {
                    CardId = cardId,
                    CardNumber = "123",
                    TotalAccess = 1
                }
            };

            _repoMock
                .Setup(r => r.GetCardAccessSummaryAsync(cardId, null, null))
                .ReturnsAsync(summary);

            var result = (await _service.GetCardAccessSummaryAsync(cardId)).ToList();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(cardId, result[0].CardId);
        }

        [TestMethod]
        public async Task GetRecentCardAccessAsync_BuildsQueryAndLimitsResults()
        {
            var cardId = Guid.NewGuid();
            var h1 = CreateCardHistory(createdBy: "System");
            var h2 = CreateCardHistory(createdBy: "System");

            _repoMock
                .Setup(r => r.GetCardHistoriesAsync(It.IsAny<CardHistoryQueryDto>()))
                .ReturnsAsync(new List<CardHistory> { h1, h2 });

            var result = (await _service.GetRecentCardAccessAsync(cardId, limit: 1)).ToList();

            Assert.AreEqual(1, result.Count);
            _repoMock.Verify(r => r.GetCardHistoriesAsync(It.Is<CardHistoryQueryDto>(q => q.CardId == cardId)), Times.Once);
        }

        [TestMethod]
        public async Task GetAccessStatisticsAsync_ComputesCorrectCounts()
        {
            var from = DateTime.UtcNow.AddDays(-1);
            var to = DateTime.UtcNow;

            var histories = new List<CardHistory>
            {
                new CardHistory { CardId = Guid.NewGuid(), EventCode = "ALLOW_ACCESS", FieldName = "Door1" },
                new CardHistory { CardId = Guid.NewGuid(), EventCode = "DENIED_ACCESS", FieldName = "Door1" },
                new CardHistory { CardId = Guid.NewGuid(), EventCode = "ALLOW_ACCESS", FieldName = "Door2" }
            };

            _repoMock
                .Setup(r => r.GetCardHistoriesByDateRangeAsync(from, to))
                .ReturnsAsync(histories);

            var stats = await _service.GetAccessStatisticsAsync(from, to);

            Assert.AreEqual(3, stats["TotalAccess"]);
            Assert.AreEqual(2, stats["SuccessfulAccess"]);
            Assert.AreEqual(1, stats["FailedAccess"]);
            Assert.AreEqual(3, stats["UniqueCards"]);
            Assert.AreEqual(2, stats["UniqueAreas"]);
            Assert.AreEqual(2, stats["Event_ALLOW_ACCESS"]);
            Assert.AreEqual(1, stats["Event_DENIED_ACCESS"]);
            Assert.AreEqual(2, stats["Field_Door1"]);
            Assert.AreEqual(1, stats["Field_Door2"]);
        }

        #endregion
    }
}