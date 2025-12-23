using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SAMS_BE.DTOs;
using SAMS_BE.Helpers;
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
    public class AccessCardServiceTests
    {
        private Mock<IAccessCardRepository> _accessCardRepositoryMock = null!;
        private Mock<IApartmentRepository> _apartmentRepositoryMock = null!;
        private Mock<ILogger<AccessCardService>> _loggerMock = null!;
        private Mock<CardHistoryHelper> _cardHistoryHelperMock = null!;
        private BuildingManagementContext _context = null!;
        private AccessCardService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _accessCardRepositoryMock = new Mock<IAccessCardRepository>();
            _apartmentRepositoryMock = new Mock<IApartmentRepository>();
            _loggerMock = new Mock<ILogger<AccessCardService>>();
            _cardHistoryHelperMock = new Mock<CardHistoryHelper>(
                Mock.Of<SAMS_BE.Interfaces.IService.ICardHistoryService>(),
                Mock.Of<ILogger<CardHistoryHelper>>(),
                CreateInMemoryContext("test"));

            var options = new DbContextOptionsBuilder<BuildingManagementContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new BuildingManagementContext(options, Mock.Of<SAMS_BE.Tenant.ITenantContextAccessor>());

            _service = new AccessCardService(
                _accessCardRepositoryMock.Object,
                _apartmentRepositoryMock.Object,
                _loggerMock.Object,
                _context,
                _cardHistoryHelperMock.Object);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context?.Dispose();
        }

        private BuildingManagementContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<BuildingManagementContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new BuildingManagementContext(options, Mock.Of<SAMS_BE.Tenant.ITenantContextAccessor>());
        }

        #region Helper Methods

        private AccessCard CreateTestAccessCard(Guid? cardId = null, string? cardNumber = null, Guid? userId = null, Guid? apartmentId = null, string? status = null)
        {
            return new AccessCard
            {
                CardId = cardId ?? Guid.NewGuid(),
                CardNumber = cardNumber ?? "CARD-A1001-01",
                Status = status ?? "ACTIVE",
                IssuedToUserId = userId,
                IssuedToApartmentId = apartmentId,
                IssuedDate = DateTime.UtcNow,
                ExpiredDate = null,
                CreatedAt = DateTime.UtcNow,
                IsDelete = false,
                IssuedToUser = userId.HasValue ? new User
                {
                    UserId = userId.Value,
                    FirstName = "John",
                    LastName = "Doe",
                    Username = "johndoe",
                    Email = "john@example.com",
                    Phone = "1234567890"
                } : null,
                IssuedToApartment = apartmentId.HasValue ? new Apartment
                {
                    ApartmentId = apartmentId.Value,
                    Number = "A1001",
                    FloorId = Guid.NewGuid(),
                    Status = "ACTIVE"
                } : null,
                AccessCardCapabilities = new List<AccessCardCapability>()
            };
        }

        private User CreateTestUser(Guid? userId = null)
        {
            return new User
            {
                UserId = userId ?? Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                Username = "johndoe",
                Email = "john@example.com",
                Phone = "1234567890"
            };
        }

        private Apartment CreateTestApartment(Guid? apartmentId = null, string? number = null)
        {
            return new Apartment
            {
                ApartmentId = apartmentId ?? Guid.NewGuid(),
                Number = number ?? "A1001",
                FloorId = Guid.NewGuid(),
                Status = "ACTIVE"
            };
        }

        private AccessCardType CreateTestCardType(Guid? cardTypeId = null, string? code = null, string? name = null)
        {
            return new AccessCardType
            {
                CardTypeId = cardTypeId ?? Guid.NewGuid(),
                Code = code ?? "ELEVATOR",
                Name = name ?? "Thang máy",
                Description = "Quyền sử dụng thang máy",
                IsActive = true,
                IsDelete = false
            };
        }

        #endregion

        #region GetAccessCardsWithDetailsAsync Tests

        [TestMethod]
        public async Task GetAccessCardsWithDetailsAsync_Success_ReturnsAccessCardDtos()
        {
            // Arrange
            var cards = new List<AccessCard>
            {
                CreateTestAccessCard(cardNumber: "CARD-A1001-01"),
                CreateTestAccessCard(cardNumber: "CARD-A1002-01")
            };

            _accessCardRepositoryMock
                .Setup(r => r.GetAccessCardsWithDetailsAsync())
                .ReturnsAsync(cards);

            // Act
            var result = await _service.GetAccessCardsWithDetailsAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
            _accessCardRepositoryMock.Verify(r => r.GetAccessCardsWithDetailsAsync(), Times.Once);
        }

        [TestMethod]
        public async Task GetAccessCardsWithDetailsAsync_Exception_ThrowsAndLogs()
        {
            // Arrange
            var exception = new Exception("Database error");
            _accessCardRepositoryMock
                .Setup(r => r.GetAccessCardsWithDetailsAsync())
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.GetAccessCardsWithDetailsAsync());
            _accessCardRepositoryMock.Verify(r => r.GetAccessCardsWithDetailsAsync(), Times.Once);
        }

        #endregion

        #region GetAccessCardWithDetailsByIdAsync Tests

        [TestMethod]
        public async Task GetAccessCardWithDetailsByIdAsync_CardExists_ReturnsAccessCardDto()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var card = CreateTestAccessCard(cardId, "CARD-A1001-01");

            _accessCardRepositoryMock
                .Setup(r => r.GetAccessCardWithDetailsByIdAsync(cardId))
                .ReturnsAsync(card);

            // Act
            var result = await _service.GetAccessCardWithDetailsByIdAsync(cardId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(cardId, result.CardId);
            Assert.AreEqual("CARD-A1001-01", result.CardNumber);
            _accessCardRepositoryMock.Verify(r => r.GetAccessCardWithDetailsByIdAsync(cardId), Times.Once);
        }

        [TestMethod]
        public async Task GetAccessCardWithDetailsByIdAsync_CardNotFound_ReturnsNull()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            _accessCardRepositoryMock
                .Setup(r => r.GetAccessCardWithDetailsByIdAsync(cardId))
                .ReturnsAsync((AccessCard?)null);

            // Act
            var result = await _service.GetAccessCardWithDetailsByIdAsync(cardId);

            // Assert
            Assert.IsNull(result);
            _accessCardRepositoryMock.Verify(r => r.GetAccessCardWithDetailsByIdAsync(cardId), Times.Once);
        }

        [TestMethod]
        public async Task GetAccessCardWithDetailsByIdAsync_Exception_ThrowsAndLogs()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var exception = new Exception("Database error");
            _accessCardRepositoryMock
                .Setup(r => r.GetAccessCardWithDetailsByIdAsync(cardId))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.GetAccessCardWithDetailsByIdAsync(cardId));
            _accessCardRepositoryMock.Verify(r => r.GetAccessCardWithDetailsByIdAsync(cardId), Times.Once);
        }

        #endregion

        #region GetAccessCardsByUserIdAsync Tests

        [TestMethod]
        public async Task GetAccessCardsByUserIdAsync_Success_ReturnsAccessCardDtos()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var cards = new List<AccessCard>
            {
                CreateTestAccessCard(userId: userId, cardNumber: "CARD-A1001-01"),
                CreateTestAccessCard(userId: userId, cardNumber: "CARD-A1001-02")
            };

            _accessCardRepositoryMock
                .Setup(r => r.GetAccessCardsByUserIdAsync(userId))
                .ReturnsAsync(cards);

            // Act
            var result = await _service.GetAccessCardsByUserIdAsync(userId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
            _accessCardRepositoryMock.Verify(r => r.GetAccessCardsByUserIdAsync(userId), Times.Once);
        }

        [TestMethod]
        public async Task GetAccessCardsByUserIdAsync_Exception_ThrowsAndLogs()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var exception = new Exception("Database error");
            _accessCardRepositoryMock
                .Setup(r => r.GetAccessCardsByUserIdAsync(userId))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.GetAccessCardsByUserIdAsync(userId));
        }

        #endregion

        #region GetAccessCardsByApartmentIdAsync Tests

        [TestMethod]
        public async Task GetAccessCardsByApartmentIdAsync_Success_ReturnsAccessCardDtos()
        {
            // Arrange
            var apartmentId = Guid.NewGuid();
            var cards = new List<AccessCard>
            {
                CreateTestAccessCard(apartmentId: apartmentId, cardNumber: "CARD-A1001-01")
            };

            _accessCardRepositoryMock
                .Setup(r => r.GetAccessCardsByApartmentIdAsync(apartmentId))
                .ReturnsAsync(cards);

            // Act
            var result = await _service.GetAccessCardsByApartmentIdAsync(apartmentId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            _accessCardRepositoryMock.Verify(r => r.GetAccessCardsByApartmentIdAsync(apartmentId), Times.Once);
        }

        [TestMethod]
        public async Task GetAccessCardsByApartmentIdAsync_Exception_ThrowsAndLogs()
        {
            // Arrange
            var apartmentId = Guid.NewGuid();
            var exception = new Exception("Database error");
            _accessCardRepositoryMock
                .Setup(r => r.GetAccessCardsByApartmentIdAsync(apartmentId))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.GetAccessCardsByApartmentIdAsync(apartmentId));
        }

        #endregion

        #region GetAccessCardsByStatusAsync Tests

        [TestMethod]
        public async Task GetAccessCardsByStatusAsync_Success_ReturnsAccessCardDtos()
        {
            // Arrange
            var status = "ACTIVE";
            var cards = new List<AccessCard>
            {
                CreateTestAccessCard(cardNumber: "CARD-A1001-01", status: status)
            };

            _accessCardRepositoryMock
                .Setup(r => r.GetAccessCardsByStatusAsync(status))
                .ReturnsAsync(cards);

            // Act
            var result = await _service.GetAccessCardsByStatusAsync(status);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            _accessCardRepositoryMock.Verify(r => r.GetAccessCardsByStatusAsync(status), Times.Once);
        }

        [TestMethod]
        public async Task GetAccessCardsByStatusAsync_Exception_ThrowsAndLogs()
        {
            // Arrange
            var status = "ACTIVE";
            var exception = new Exception("Database error");
            _accessCardRepositoryMock
                .Setup(r => r.GetAccessCardsByStatusAsync(status))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.GetAccessCardsByStatusAsync(status));
        }

        #endregion

        #region GetAccessCardsByCardTypeAsync Tests

        [TestMethod]
        public async Task GetAccessCardsByCardTypeAsync_Success_ReturnsAccessCardDtos()
        {
            // Arrange
            var cardTypeId = Guid.NewGuid();
            var cards = new List<AccessCard>
            {
                CreateTestAccessCard(cardNumber: "CARD-A1001-01")
            };

            _accessCardRepositoryMock
                .Setup(r => r.GetAccessCardsByCardTypeAsync(cardTypeId))
                .ReturnsAsync(cards);

            // Act
            var result = await _service.GetAccessCardsByCardTypeAsync(cardTypeId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            _accessCardRepositoryMock.Verify(r => r.GetAccessCardsByCardTypeAsync(cardTypeId), Times.Once);
        }

        [TestMethod]
        public async Task GetAccessCardsByCardTypeAsync_Exception_ThrowsAndLogs()
        {
            // Arrange
            var cardTypeId = Guid.NewGuid();
            var exception = new Exception("Database error");
            _accessCardRepositoryMock
                .Setup(r => r.GetAccessCardsByCardTypeAsync(cardTypeId))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.GetAccessCardsByCardTypeAsync(cardTypeId));
        }

        #endregion

        #region IsCardNumberExistsAsync Tests

        [TestMethod]
        public async Task IsCardNumberExistsAsync_CardExists_ReturnsTrue()
        {
            // Arrange
            var cardNumber = "CARD-A1001-01";
            _accessCardRepositoryMock
                .Setup(r => r.IsCardNumberExistsAsync(cardNumber, null))
                .ReturnsAsync(true);

            // Act
            var result = await _service.IsCardNumberExistsAsync(cardNumber);

            // Assert
            Assert.IsTrue(result);
            _accessCardRepositoryMock.Verify(r => r.IsCardNumberExistsAsync(cardNumber, null), Times.Once);
        }

        [TestMethod]
        public async Task IsCardNumberExistsAsync_CardNotExists_ReturnsFalse()
        {
            // Arrange
            var cardNumber = "CARD-A1001-01";
            _accessCardRepositoryMock
                .Setup(r => r.IsCardNumberExistsAsync(cardNumber, null))
                .ReturnsAsync(false);

            // Act
            var result = await _service.IsCardNumberExistsAsync(cardNumber);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task IsCardNumberExistsAsync_WithExcludeId_CardExists_ReturnsTrue()
        {
            // Arrange
            var cardNumber = "CARD-A1001-01";
            var excludeId = Guid.NewGuid();
            _accessCardRepositoryMock
                .Setup(r => r.IsCardNumberExistsAsync(cardNumber, excludeId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.IsCardNumberExistsAsync(cardNumber, excludeId);

            // Assert
            Assert.IsTrue(result);
            _accessCardRepositoryMock.Verify(r => r.IsCardNumberExistsAsync(cardNumber, excludeId), Times.Once);
        }

        [TestMethod]
        public async Task IsCardNumberExistsAsync_Exception_ThrowsAndLogs()
        {
            // Arrange
            var cardNumber = "CARD-A1001-01";
            var exception = new Exception("Database error");
            _accessCardRepositoryMock
                .Setup(r => r.IsCardNumberExistsAsync(cardNumber, null))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.IsCardNumberExistsAsync(cardNumber));
        }

        #endregion

        #region CreateAccessCardAsync Tests

        [TestMethod]
        public async Task CreateAccessCardAsync_EmptyCardNumber_ThrowsArgumentException()
        {
            // Arrange
            var createDto = new CreateAccessCardDto
            {
                CardNumber = "",
                Status = "ACTIVE",
                CardTypeIds = new List<Guid>()
            };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.CreateAccessCardAsync(createDto));
        }

        [TestMethod]
        public async Task CreateAccessCardAsync_NullCardNumber_ThrowsArgumentException()
        {
            // Arrange
            var createDto = new CreateAccessCardDto
            {
                CardNumber = null!,
                Status = "ACTIVE",
                CardTypeIds = new List<Guid>()
            };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.CreateAccessCardAsync(createDto));
        }

        [TestMethod]
        public async Task CreateAccessCardAsync_InvalidFormat_ThrowsArgumentException()
        {
            // Arrange
            var createDto = new CreateAccessCardDto
            {
                CardNumber = "INVALID-FORMAT",
                Status = "ACTIVE",
                CardTypeIds = new List<Guid>()
            };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.CreateAccessCardAsync(createDto));
        }

        [TestMethod]
        public async Task CreateAccessCardAsync_LowercaseCard_ThrowsArgumentException()
        {
            // Arrange
            var createDto = new CreateAccessCardDto
            {
                CardNumber = "card-A1001-01",
                Status = "ACTIVE",
                CardTypeIds = new List<Guid>()
            };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.CreateAccessCardAsync(createDto));
        }

        [TestMethod]
        public async Task CreateAccessCardAsync_InvalidApartmentCode_ThrowsArgumentException()
        {
            // Arrange
            var createDto = new CreateAccessCardDto
            {
                CardNumber = "CARD-a1001-01", // lowercase apartment code
                Status = "ACTIVE",
                CardTypeIds = new List<Guid>()
            };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.CreateAccessCardAsync(createDto));
        }

        [TestMethod]
        public async Task CreateAccessCardAsync_InvalidSequenceNumber_ThrowsArgumentException()
        {
            // Arrange
            var createDto = new CreateAccessCardDto
            {
                CardNumber = "CARD-A1001-0", // sequence number too short
                Status = "ACTIVE",
                CardTypeIds = new List<Guid>()
            };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.CreateAccessCardAsync(createDto));
        }

        [TestMethod]
        public async Task CreateAccessCardAsync_CardNumberExists_ThrowsInvalidOperationException()
        {
            // Arrange
            var createDto = new CreateAccessCardDto
            {
                CardNumber = "CARD-A1001-01",
                Status = "ACTIVE",
                CardTypeIds = new List<Guid>()
            };

            _accessCardRepositoryMock
                .Setup(r => r.IsCardNumberExistsAsync(createDto.CardNumber, null))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await _service.CreateAccessCardAsync(createDto));
        }

        
        [TestMethod]
        public async Task CreateAccessCardAsync_ApartmentNumberNotFound_ThrowsInvalidOperationException()
        {
            // Arrange
            var apartmentNumber = "A9999";
            var createDto = new CreateAccessCardDto
            {
                CardNumber = "CARD-A1001-01",
                Status = "ACTIVE",
                IssuedToApartmentNumber = apartmentNumber,
                CardTypeIds = new List<Guid>()
            };

            _accessCardRepositoryMock
                .Setup(r => r.IsCardNumberExistsAsync(createDto.CardNumber, null))
                .ReturnsAsync(false);

            _apartmentRepositoryMock
                .Setup(r => r.GetApartmentByNumberAsync(apartmentNumber))
                .ReturnsAsync((Apartment?)null);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await _service.CreateAccessCardAsync(createDto));
        }

        [TestMethod]
        public async Task CreateAccessCardAsync_Exception_ThrowsAndLogs()
        {
            // Arrange
            var createDto = new CreateAccessCardDto
            {
                CardNumber = "CARD-A1001-01",
                Status = "ACTIVE",
                CardTypeIds = new List<Guid>()
            };

            var exception = new Exception("Database error");
            _accessCardRepositoryMock
                .Setup(r => r.IsCardNumberExistsAsync(createDto.CardNumber, null))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.CreateAccessCardAsync(createDto));
        }

        #endregion

        #region UpdateAccessCardAsync Tests

        [TestMethod]
        public async Task UpdateAccessCardAsync_CardNotFound_ReturnsNull()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var updateDto = new UpdateAccessCardDto
            {
                Status = "INACTIVE"
            };

            _accessCardRepositoryMock
                .Setup(r => r.GetAccessCardWithDetailsByIdAsync(cardId))
                .ReturnsAsync((AccessCard?)null);

            // Act
            var result = await _service.UpdateAccessCardAsync(cardId, updateDto);

            // Assert
            Assert.IsNull(result);
        }

     

       

        [TestMethod]
        public async Task UpdateAccessCardAsync_UpdateCardNumber_InvalidFormat_ThrowsArgumentException()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var existingCard = CreateTestAccessCard(cardId, "CARD-A1001-01");

            var updateDto = new UpdateAccessCardDto
            {
                CardNumber = "INVALID-FORMAT"
            };

            _accessCardRepositoryMock
                .Setup(r => r.GetAccessCardWithDetailsByIdAsync(cardId))
                .ReturnsAsync(existingCard);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.UpdateAccessCardAsync(cardId, updateDto));
        }

        [TestMethod]
        public async Task UpdateAccessCardAsync_UpdateCardNumber_AlreadyExists_ThrowsInvalidOperationException()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var existingCard = CreateTestAccessCard(cardId, "CARD-A1001-01");

            var updateDto = new UpdateAccessCardDto
            {
                CardNumber = "CARD-A1001-02"
            };

            _accessCardRepositoryMock
                .Setup(r => r.GetAccessCardWithDetailsByIdAsync(cardId))
                .ReturnsAsync(existingCard);

            _accessCardRepositoryMock
                .Setup(r => r.IsCardNumberExistsAsync("CARD-A1001-02", cardId))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await _service.UpdateAccessCardAsync(cardId, updateDto));
        }

       

      


        [TestMethod]
        public async Task UpdateAccessCardAsync_UpdateApartmentNumber_NotFound_ThrowsInvalidOperationException()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var existingCard = CreateTestAccessCard(cardId, "CARD-A1001-01");

            var updateDto = new UpdateAccessCardDto
            {
                IssuedToApartmentNumber = "A9999"
            };

            _accessCardRepositoryMock
                .Setup(r => r.GetAccessCardWithDetailsByIdAsync(cardId))
                .ReturnsAsync(existingCard);

            _apartmentRepositoryMock
                .Setup(r => r.GetApartmentByNumberAsync("A9999"))
                .ReturnsAsync((Apartment?)null);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await _service.UpdateAccessCardAsync(cardId, updateDto));
        }
      

        [TestMethod]
        public async Task UpdateAccessCardAsync_UpdateReturnsNull_ReturnsNull()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var existingCard = CreateTestAccessCard(cardId, "CARD-A1001-01");

            var updateDto = new UpdateAccessCardDto
            {
                Status = "INACTIVE"
            };

            _accessCardRepositoryMock
                .Setup(r => r.GetAccessCardWithDetailsByIdAsync(cardId))
                .ReturnsAsync(existingCard);

            _accessCardRepositoryMock
                .Setup(r => r.UpdateAccessCardAsync(cardId, It.IsAny<AccessCard>(), It.IsAny<List<Guid>?>()))
                .ReturnsAsync((AccessCard?)null);

            // Act
            var result = await _service.UpdateAccessCardAsync(cardId, updateDto);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task UpdateAccessCardAsync_Exception_ThrowsAndLogs()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var exception = new Exception("Database error");
            var updateDto = new UpdateAccessCardDto();

            _accessCardRepositoryMock
                .Setup(r => r.GetAccessCardWithDetailsByIdAsync(cardId))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.UpdateAccessCardAsync(cardId, updateDto));
        }

        #endregion

        #region SoftDeleteAccessCardAsync Tests

        [TestMethod]
        public async Task SoftDeleteAccessCardAsync_Success_ReturnsTrue()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            _accessCardRepositoryMock
                .Setup(r => r.SoftDeleteAccessCardAsync(cardId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.SoftDeleteAccessCardAsync(cardId);

            // Assert
            Assert.IsTrue(result);
            _accessCardRepositoryMock.Verify(r => r.SoftDeleteAccessCardAsync(cardId), Times.Once);
        }

        [TestMethod]
        public async Task SoftDeleteAccessCardAsync_Failure_ReturnsFalse()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            _accessCardRepositoryMock
                .Setup(r => r.SoftDeleteAccessCardAsync(cardId))
                .ReturnsAsync(false);

            // Act
            var result = await _service.SoftDeleteAccessCardAsync(cardId);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task SoftDeleteAccessCardAsync_Exception_ThrowsAndLogs()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var exception = new Exception("Database error");
            _accessCardRepositoryMock
                .Setup(r => r.SoftDeleteAccessCardAsync(cardId))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.SoftDeleteAccessCardAsync(cardId));
        }

        #endregion

        #region GetCardTypesAsync Tests

        [TestMethod]
        public async Task GetCardTypesAsync_Success_ReturnsCardTypes()
        {
            // Arrange
            var cardType1 = CreateTestCardType(code: "ELEVATOR", name: "Thang máy");
            var cardType2 = CreateTestCardType(code: "PARKING", name: "Bãi đỗ xe");

            _context.AccessCardTypes.AddRange(cardType1, cardType2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetCardTypesAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
        }


        #endregion

        #region GetCardCapabilitiesAsync Tests

        [TestMethod]
        public async Task GetCardCapabilitiesAsync_Success_ReturnsCapabilities()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var cardType = CreateTestCardType();
            var card = CreateTestAccessCard(cardId);
            var capability = new AccessCardCapability
            {
                CardCapabilityId = Guid.NewGuid(),
                CardId = cardId,
                CardTypeId = cardType.CardTypeId,
                CardType = cardType,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.AccessCardTypes.Add(cardType);
            _context.AccessCards.Add(card);
            _context.AccessCardCapabilities.Add(capability);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetCardCapabilitiesAsync(cardId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
        }


        #endregion

        #region UpdateCardCapabilitiesAsync Tests

        [TestMethod]
        public async Task UpdateCardCapabilitiesAsync_CardNotFound_ReturnsFalse()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var cardTypeIds = new List<Guid> { Guid.NewGuid() };

            // Act
            var result = await _service.UpdateCardCapabilitiesAsync(cardId, cardTypeIds);

            // Assert
            Assert.IsFalse(result);
        }

        


        

        
        #endregion
    }
}
