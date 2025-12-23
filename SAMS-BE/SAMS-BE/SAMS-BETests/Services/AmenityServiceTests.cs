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

namespace SAMS_BE.Services.Tests
{
    [TestClass]
    public class AmenityServiceTests
    {
        private Mock<IAmenityRepository> _amenityRepositoryMock = null!;
        private Mock<IAmenityPackageRepository> _packageRepositoryMock = null!;
        private Mock<IAssetRepository> _assetRepositoryMock = null!;
        private Mock<IAssetMaintenanceScheduleRepository> _scheduleRepositoryMock = null!;
        private Mock<ILogger<AmenityService>> _loggerMock = null!;
        private AmenityService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _amenityRepositoryMock = new Mock<IAmenityRepository>();
            _packageRepositoryMock = new Mock<IAmenityPackageRepository>();
            _assetRepositoryMock = new Mock<IAssetRepository>();
            _scheduleRepositoryMock = new Mock<IAssetMaintenanceScheduleRepository>();
            _loggerMock = new Mock<ILogger<AmenityService>>();

            _service = new AmenityService(
                _amenityRepositoryMock.Object,
                _packageRepositoryMock.Object,
                _assetRepositoryMock.Object,
                _scheduleRepositoryMock.Object,
                _loggerMock.Object);
        }

        #region Helper Methods

        private Amenity CreateTestAmenity(Guid? amenityId = null, string? code = null, string? name = null, 
            string? status = null, bool hasMonthlyPackage = true, Guid? assetId = null, string? location = null)
        {
            return new Amenity
            {
                AmenityId = amenityId ?? Guid.NewGuid(),
                Code = code ?? "AMENITY-001",
                Name = name ?? "Swimming Pool",
                CategoryName = "Recreation",
                Location = location ?? "Building A - Floor 1",
                HasMonthlyPackage = hasMonthlyPackage,
                RequiresFaceVerification = false,
                FeeType = "Paid",
                Status = status ?? "ACTIVE",
                AssetId = assetId,
                IsDelete = false,
                AmenityPackages = new List<AmenityPackage>()
            };
        }

        private AmenityPackage CreateTestPackage(Guid? packageId = null, Guid? amenityId = null, 
            string? name = null, int price = 100000)
        {
            return new AmenityPackage
            {
                PackageId = packageId ?? Guid.NewGuid(),
                AmenityId = amenityId ?? Guid.NewGuid(),
                Name = name ?? "1 Month Package",
                MonthCount = 1,
                DurationDays = 30,
                PeriodUnit = "Day",
                Price = price,
                Description = "Test package",
                Status = "ACTIVE"
            };
        }

        private Asset CreateTestAsset(Guid? assetId = null, string? code = null, string? name = null)
        {
            return new Asset
            {
                AssetId = assetId ?? Guid.NewGuid(),
                CategoryId = Guid.NewGuid(),
                Code = code ?? "ASSET-001",
                Name = name ?? "Swimming Pool Asset",
                Location = "Building A - Floor 1",
                Status = "ACTIVE",
                MaintenanceFrequency = 30,
                IsDelete = false
            };
        }

        private AssetCategory CreateTestAssetCategory(Guid? categoryId = null, string? code = null)
        {
            return new AssetCategory
            {
                CategoryId = categoryId ?? Guid.NewGuid(),
                Code = code ?? "AMENITY",
                Name = "Amenity Category",
                MaintenanceFrequency = 30
            };
        }

        private AssetMaintenanceSchedule CreateTestMaintenanceSchedule(Guid? scheduleId = null, Guid? assetId = null)
        {
            return new AssetMaintenanceSchedule
            {
                ScheduleId = scheduleId ?? Guid.NewGuid(),
                AssetId = assetId ?? Guid.NewGuid(),
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
                StartTime = TimeOnly.FromDateTime(DateTime.UtcNow),
                EndTime = TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(2)),
                Status = "ACTIVE",
                ReminderDays = 1,
                CreatedAt = DateTime.UtcNow
            };
        }

        #endregion

        #region Constructor Test

        [TestMethod]
        public void AmenityServiceTest()
        {
            // Arrange & Act
            var service = new AmenityService(
                _amenityRepositoryMock.Object,
                _packageRepositoryMock.Object,
                _assetRepositoryMock.Object,
                _scheduleRepositoryMock.Object,
                _loggerMock.Object);

            // Assert
            Assert.IsNotNull(service);
        }

        #endregion

        #region GetAllAmenitiesAsync Tests

        [TestMethod]
        public async Task GetAllAmenitiesAsync_Success_ReturnsAmenityDtos()
        {
            // Arrange
            var amenities = new List<Amenity>
            {
                CreateTestAmenity(code: "AMENITY-001", name: "Swimming Pool"),
                CreateTestAmenity(code: "AMENITY-002", name: "Gym")
            };

            _amenityRepositoryMock
                .Setup(r => r.GetAllAmenitiesAsync())
                .ReturnsAsync(amenities);

            _scheduleRepositoryMock
                .Setup(r => r.GetActiveMaintenanceSchedulesByAssetIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new Dictionary<Guid, AssetMaintenanceSchedule>());

            // Act
            var result = await _service.GetAllAmenitiesAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
            _amenityRepositoryMock.Verify(r => r.GetAllAmenitiesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task GetAllAmenitiesAsync_EmptyList_ReturnsEmptyList()
        {
            // Arrange
            _amenityRepositoryMock
                .Setup(r => r.GetAllAmenitiesAsync())
                .ReturnsAsync(new List<Amenity>());

            // Act
            var result = await _service.GetAllAmenitiesAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public async Task GetAllAmenitiesAsync_Exception_ThrowsAndLogs()
        {
            // Arrange
            var exception = new Exception("Database error");
            _amenityRepositoryMock
                .Setup(r => r.GetAllAmenitiesAsync())
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.GetAllAmenitiesAsync());
            _amenityRepositoryMock.Verify(r => r.GetAllAmenitiesAsync(), Times.Once);
        }

        #endregion

        #region GetAmenityByIdAsync Tests

        [TestMethod]
        public async Task GetAmenityByIdAsync_AmenityExists_ReturnsAmenityDto()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            var amenity = CreateTestAmenity(amenityId, "AMENITY-001", "Swimming Pool");

            _amenityRepositoryMock
                .Setup(r => r.GetAmenityByIdAsync(amenityId))
                .ReturnsAsync(amenity);

            _scheduleRepositoryMock
                .Setup(r => r.GetActiveMaintenanceSchedulesByAssetIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new Dictionary<Guid, AssetMaintenanceSchedule>());

            // Act
            var result = await _service.GetAmenityByIdAsync(amenityId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(amenityId, result.AmenityId);
            Assert.AreEqual("AMENITY-001", result.Code);
            Assert.AreEqual("Swimming Pool", result.Name);
            _amenityRepositoryMock.Verify(r => r.GetAmenityByIdAsync(amenityId), Times.Once);
        }

        [TestMethod]
        public async Task GetAmenityByIdAsync_AmenityNotFound_ReturnsNull()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            _amenityRepositoryMock
                .Setup(r => r.GetAmenityByIdAsync(amenityId))
                .ReturnsAsync((Amenity?)null);

            // Act
            var result = await _service.GetAmenityByIdAsync(amenityId);

            // Assert
            Assert.IsNull(result);
            _amenityRepositoryMock.Verify(r => r.GetAmenityByIdAsync(amenityId), Times.Once);
        }

        [TestMethod]
        public async Task GetAmenityByIdAsync_Exception_ThrowsAndLogs()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            var exception = new Exception("Database error");
            _amenityRepositoryMock
                .Setup(r => r.GetAmenityByIdAsync(amenityId))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.GetAmenityByIdAsync(amenityId));
        }

        #endregion

        #region SearchAmenitiesAsync Tests

        [TestMethod]
        public async Task SearchAmenitiesAsync_Success_ReturnsMatchingAmenities()
        {
            // Arrange
            var searchTerm = "Pool";
            var amenities = new List<Amenity>
            {
                CreateTestAmenity(name: "Swimming Pool")
            };

            _amenityRepositoryMock
                .Setup(r => r.SearchAmenitiesAsync(searchTerm))
                .ReturnsAsync(amenities);

            _scheduleRepositoryMock
                .Setup(r => r.GetActiveMaintenanceSchedulesByAssetIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new Dictionary<Guid, AssetMaintenanceSchedule>());

            // Act
            var result = await _service.SearchAmenitiesAsync(searchTerm);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Any(a => a.Name.Contains(searchTerm)));
            _amenityRepositoryMock.Verify(r => r.SearchAmenitiesAsync(searchTerm), Times.Once);
        }

        [TestMethod]
        public async Task SearchAmenitiesAsync_NoMatches_ReturnsEmptyList()
        {
            // Arrange
            var searchTerm = "NonExistent";
            _amenityRepositoryMock
                .Setup(r => r.SearchAmenitiesAsync(searchTerm))
                .ReturnsAsync(new List<Amenity>());

            // Act
            var result = await _service.SearchAmenitiesAsync(searchTerm);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        #endregion

        #region GetAmenitiesByStatusAsync Tests

        [TestMethod]
        public async Task GetAmenitiesByStatusAsync_Success_ReturnsAmenitiesWithStatus()
        {
            // Arrange
            var status = "ACTIVE";
            var amenities = new List<Amenity>
            {
                CreateTestAmenity(status: "ACTIVE"),
                CreateTestAmenity(status: "ACTIVE")
            };

            _amenityRepositoryMock
                .Setup(r => r.GetAmenitiesByStatusAsync(status))
                .ReturnsAsync(amenities);

            _scheduleRepositoryMock
                .Setup(r => r.GetActiveMaintenanceSchedulesByAssetIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new Dictionary<Guid, AssetMaintenanceSchedule>());

            // Act
            var result = await _service.GetAmenitiesByStatusAsync(status);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.All(a => a.Status == status));
            _amenityRepositoryMock.Verify(r => r.GetAmenitiesByStatusAsync(status), Times.Once);
        }

        #endregion

        #region GetAmenitiesByLocationAsync Tests

        [TestMethod]
        public async Task GetAmenitiesByLocationAsync_Success_ReturnsAmenitiesAtLocation()
        {
            // Arrange
            var location = "Building A";
            var amenities = new List<Amenity>
            {
                CreateTestAmenity(location: "Building A - Floor 1")
            };

            _amenityRepositoryMock
                .Setup(r => r.GetAmenitiesByLocationAsync(location))
                .ReturnsAsync(amenities);

            _scheduleRepositoryMock
                .Setup(r => r.GetActiveMaintenanceSchedulesByAssetIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new Dictionary<Guid, AssetMaintenanceSchedule>());

            // Act
            var result = await _service.GetAmenitiesByLocationAsync(location);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.All(a => a.Location != null && a.Location.Contains(location)));
            _amenityRepositoryMock.Verify(r => r.GetAmenitiesByLocationAsync(location), Times.Once);
        }

        #endregion

        #region GetAmenitiesByCategoryAsync Tests

        [TestMethod]
        public async Task GetAmenitiesByCategoryAsync_Success_ReturnsAmenitiesInCategory()
        {
            // Arrange
            var categoryName = "Recreation";
            var amenities = new List<Amenity>
            {
                CreateTestAmenity()
            };

            _amenityRepositoryMock
                .Setup(r => r.GetAmenitiesByCategoryAsync(categoryName))
                .ReturnsAsync(amenities);

            _scheduleRepositoryMock
                .Setup(r => r.GetActiveMaintenanceSchedulesByAssetIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new Dictionary<Guid, AssetMaintenanceSchedule>());

            // Act
            var result = await _service.GetAmenitiesByCategoryAsync(categoryName);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            _amenityRepositoryMock.Verify(r => r.GetAmenitiesByCategoryAsync(categoryName), Times.Once);
        }

        #endregion

        #region GetAmenitiesByPriceRangeAsync Tests

        [TestMethod]
        public async Task GetAmenitiesByPriceRangeAsync_Success_ReturnsAmenitiesInPriceRange()
        {
            // Arrange
            var minPrice = 50000;
            var maxPrice = 200000;
            var amenities = new List<Amenity>
            {
                CreateTestAmenity()
            };

            _amenityRepositoryMock
                .Setup(r => r.GetAmenitiesByPriceRangeAsync(minPrice, maxPrice))
                .ReturnsAsync(amenities);

            _scheduleRepositoryMock
                .Setup(r => r.GetActiveMaintenanceSchedulesByAssetIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new Dictionary<Guid, AssetMaintenanceSchedule>());

            // Act
            var result = await _service.GetAmenitiesByPriceRangeAsync(minPrice, maxPrice);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            _amenityRepositoryMock.Verify(r => r.GetAmenitiesByPriceRangeAsync(minPrice, maxPrice), Times.Once);
        }

        #endregion

        #region GetAmenityCountAsync Tests

        [TestMethod]
        public async Task GetAmenityCountAsync_Success_ReturnsCount()
        {
            // Arrange
            var expectedCount = 10;
            _amenityRepositoryMock
                .Setup(r => r.GetAmenityCountAsync())
                .ReturnsAsync(expectedCount);

            // Act
            var result = await _service.GetAmenityCountAsync();

            // Assert
            Assert.AreEqual(expectedCount, result);
            _amenityRepositoryMock.Verify(r => r.GetAmenityCountAsync(), Times.Once);
        }

        #endregion

        #region GetAmenityCountByStatusAsync Tests

        [TestMethod]
        public async Task GetAmenityCountByStatusAsync_Success_ReturnsCount()
        {
            // Arrange
            var status = "ACTIVE";
            var expectedCount = 5;
            _amenityRepositoryMock
                .Setup(r => r.GetAmenityCountByStatusAsync(status))
                .ReturnsAsync(expectedCount);

            // Act
            var result = await _service.GetAmenityCountByStatusAsync(status);

            // Assert
            Assert.AreEqual(expectedCount, result);
            _amenityRepositoryMock.Verify(r => r.GetAmenityCountByStatusAsync(status), Times.Once);
        }

        #endregion

        #region GetAvailableAmenitiesAsync Tests

        [TestMethod]
        public async Task GetAvailableAmenitiesAsync_Success_ReturnsActiveAmenities()
        {
            // Arrange
            var amenities = new List<Amenity>
            {
                CreateTestAmenity(status: "ACTIVE")
            };

            _amenityRepositoryMock
                .Setup(r => r.GetAmenitiesByStatusAsync("Active"))
                .ReturnsAsync(amenities);

            _scheduleRepositoryMock
                .Setup(r => r.GetActiveMaintenanceSchedulesByAssetIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new Dictionary<Guid, AssetMaintenanceSchedule>());

            // Act
            var result = await _service.GetAvailableAmenitiesAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            _amenityRepositoryMock.Verify(r => r.GetAmenitiesByStatusAsync("Active"), Times.Once);
        }

        #endregion

        #region GetAmenitiesRequiringBookingAsync Tests

        [TestMethod]
        public async Task GetAmenitiesRequiringBookingAsync_Success_ReturnsAmenitiesWithMonthlyPackage()
        {
            // Arrange
            var amenities = new List<Amenity>
            {
                CreateTestAmenity(hasMonthlyPackage: true, status: "ACTIVE"),
                CreateTestAmenity(hasMonthlyPackage: false, status: "ACTIVE")
            };

            _amenityRepositoryMock
                .Setup(r => r.GetAllAmenitiesAsync())
                .ReturnsAsync(amenities);

            _scheduleRepositoryMock
                .Setup(r => r.GetActiveMaintenanceSchedulesByAssetIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new Dictionary<Guid, AssetMaintenanceSchedule>());

            // Act
            var result = await _service.GetAmenitiesRequiringBookingAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.All(a => a.HasMonthlyPackage && a.Status == "ACTIVE"));
        }

        #endregion

        #region CreateAmenityAsync Tests

        [TestMethod]
        public async Task CreateAmenityAsync_Success_WithoutPackages_ReturnsAmenityDto()
        {
            // Arrange
            var createDto = new CreateAmenityDto
            {
                Code = "AMENITY-001",
                Name = "Swimming Pool",
                CategoryName = "Recreation",
                Location = "Building A - Floor 1",
                HasMonthlyPackage = true,
                RequiresFaceVerification = false,
                FeeType = "Paid",
                Status = "ACTIVE"
            };

            var amenity = CreateTestAmenity();
            var category = CreateTestAssetCategory(code: "AMENITY");

            _assetRepositoryMock
                .Setup(r => r.GetAssetByCodeAsync(createDto.Code))
                .ReturnsAsync((Asset?)null);

            _assetRepositoryMock
                .Setup(r => r.GetCategoryByCodeAsync("AMENITY"))
                .ReturnsAsync(category);

            _assetRepositoryMock
                .Setup(r => r.CreateAssetAsync(It.IsAny<Asset>()))
                .ReturnsAsync(CreateTestAsset());

            _amenityRepositoryMock
                .Setup(r => r.CreateAmenityAsync(It.IsAny<Amenity>()))
                .ReturnsAsync(amenity);

            _scheduleRepositoryMock
                .Setup(r => r.GetActiveMaintenanceSchedulesByAssetIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new Dictionary<Guid, AssetMaintenanceSchedule>());

            // Act
            var result = await _service.CreateAmenityAsync(createDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(amenity.AmenityId, result.AmenityId);
            _amenityRepositoryMock.Verify(r => r.CreateAmenityAsync(It.IsAny<Amenity>()), Times.Once);
        }

        [TestMethod]
        public async Task CreateAmenityAsync_Success_WithPackages_ReturnsAmenityDtoWithPackages()
        {
            // Arrange
            var createDto = new CreateAmenityDto
            {
                Code = "AMENITY-001",
                Name = "Swimming Pool",
                CategoryName = "Recreation",
                Location = "Building A - Floor 1",
                HasMonthlyPackage = true,
                RequiresFaceVerification = false,
                FeeType = "Paid",
                Status = "ACTIVE",
                Packages = new List<CreateAmenityPackageInlineDto>
                {
                    new CreateAmenityPackageInlineDto
                    {
                        Name = "1 Month Package",
                        MonthCount = 1,
                        DurationDays = 30,
                        PeriodUnit = "Day",
                        Price = 100000,
                        Description = "Test package",
                        Status = "ACTIVE"
                    }
                }
            };

            var amenity = CreateTestAmenity();
            var category = CreateTestAssetCategory(code: "AMENITY");

            _assetRepositoryMock
                .Setup(r => r.GetAssetByCodeAsync(createDto.Code))
                .ReturnsAsync((Asset?)null);

            _assetRepositoryMock
                .Setup(r => r.GetCategoryByCodeAsync("AMENITY"))
                .ReturnsAsync(category);

            _assetRepositoryMock
                .Setup(r => r.CreateAssetAsync(It.IsAny<Asset>()))
                .ReturnsAsync(CreateTestAsset());

            _amenityRepositoryMock
                .Setup(r => r.CreateAmenityAsync(It.IsAny<Amenity>()))
                .ReturnsAsync(amenity);

            _amenityRepositoryMock
                .Setup(r => r.GetAmenityByIdAsync(amenity.AmenityId))
                .ReturnsAsync(amenity);

            _packageRepositoryMock
                .Setup(r => r.CreatePackageAsync(It.IsAny<AmenityPackage>()))
                .ReturnsAsync((AmenityPackage p) => p);

            _scheduleRepositoryMock
                .Setup(r => r.GetActiveMaintenanceSchedulesByAssetIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new Dictionary<Guid, AssetMaintenanceSchedule>());

            // Act
            var result = await _service.CreateAmenityAsync(createDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(amenity.AmenityId, result.AmenityId);
            _amenityRepositoryMock.Verify(r => r.CreateAmenityAsync(It.IsAny<Amenity>()), Times.Once);
            _packageRepositoryMock.Verify(r => r.CreatePackageAsync(It.IsAny<AmenityPackage>()), Times.Once);
        }

        [TestMethod]
        public async Task CreateAmenityAsync_WithExistingAssetId_Success()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var createDto = new CreateAmenityDto
            {
                Code = "AMENITY-001",
                Name = "Swimming Pool",
                AssetId = assetId,
                Status = "ACTIVE",
                FeeType = "Paid"
            };

            var existingAsset = CreateTestAsset(assetId, "ASSET-001", "Existing Asset");
            var amenity = CreateTestAmenity(assetId: assetId);

            _assetRepositoryMock
                .Setup(r => r.GetAssetByIdAsync(assetId))
                .ReturnsAsync(existingAsset);

            _assetRepositoryMock
                .Setup(r => r.UpdateAssetAsync(It.IsAny<Asset>()))
                .ReturnsAsync(existingAsset);

            _amenityRepositoryMock
                .Setup(r => r.CreateAmenityAsync(It.IsAny<Amenity>()))
                .ReturnsAsync(amenity);

            _scheduleRepositoryMock
                .Setup(r => r.GetActiveMaintenanceSchedulesByAssetIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new Dictionary<Guid, AssetMaintenanceSchedule>());

            // Act
            var result = await _service.CreateAmenityAsync(createDto);

            // Assert
            Assert.IsNotNull(result);
            _assetRepositoryMock.Verify(r => r.GetAssetByIdAsync(assetId), Times.Once);
        }

        [TestMethod]
        public async Task CreateAmenityAsync_InvalidAssetId_ThrowsException()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var createDto = new CreateAmenityDto
            {
                Code = "AMENITY-001",
                Name = "Swimming Pool",
                AssetId = assetId,
                Status = "ACTIVE",
                FeeType = "Paid"
            };

            _assetRepositoryMock
                .Setup(r => r.GetAssetByIdAsync(assetId))
                .ReturnsAsync((Asset?)null);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.CreateAmenityAsync(createDto));
        }

        #endregion

        #region UpdateAmenityAsync Tests

        [TestMethod]
        public async Task UpdateAmenityAsync_AmenityNotFound_ReturnsNull()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            var updateDto = new UpdateAmenityDto
            {
                Code = "AMENITY-001",
                Name = "Swimming Pool",
                Status = "ACTIVE",
                FeeType = "Paid"
            };

            _amenityRepositoryMock
                .Setup(r => r.GetAmenityByIdAsync(amenityId))
                .ReturnsAsync((Amenity?)null);

            // Act
            var result = await _service.UpdateAmenityAsync(updateDto, amenityId);

            // Assert
            Assert.IsNull(result);
            _amenityRepositoryMock.Verify(r => r.GetAmenityByIdAsync(amenityId), Times.Once);
            _amenityRepositoryMock.Verify(r => r.UpdateAmenityAsync(It.IsAny<Amenity>()), Times.Never);
        }

        [TestMethod]
        public async Task UpdateAmenityAsync_Exception_ThrowsAndLogs()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            var updateDto = new UpdateAmenityDto
            {
                Code = "AMENITY-001",
                Name = "Swimming Pool",
                Status = "ACTIVE",
                FeeType = "Paid"
            };

            var exception = new Exception("Database error");
            _amenityRepositoryMock
                .Setup(r => r.GetAmenityByIdAsync(amenityId))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.UpdateAmenityAsync(updateDto, amenityId));
        }

        #endregion

        #region DeleteAmenityAsync Tests

        [TestMethod]
        public async Task DeleteAmenityAsync_Success_ReturnsTrue()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            _amenityRepositoryMock
                .Setup(r => r.DeleteAmenityAsync(amenityId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteAmenityAsync(amenityId);

            // Assert
            Assert.IsTrue(result);
            _amenityRepositoryMock.Verify(r => r.DeleteAmenityAsync(amenityId), Times.Once);
        }

        [TestMethod]
        public async Task DeleteAmenityAsync_AmenityNotFound_ReturnsFalse()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            _amenityRepositoryMock
                .Setup(r => r.DeleteAmenityAsync(amenityId))
                .ReturnsAsync(false);

            // Act
            var result = await _service.DeleteAmenityAsync(amenityId);

            // Assert
            Assert.IsFalse(result);
            _amenityRepositoryMock.Verify(r => r.DeleteAmenityAsync(amenityId), Times.Once);
        }

        [TestMethod]
        public async Task DeleteAmenityAsync_Exception_ThrowsAndLogs()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            var exception = new Exception("Database error");
            _amenityRepositoryMock
                .Setup(r => r.DeleteAmenityAsync(amenityId))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.DeleteAmenityAsync(amenityId));
        }

        #endregion

        #region Maintenance Mapping Tests

        [TestMethod]
        public async Task GetAllAmenitiesAsync_WithMaintenanceSchedule_SetsMaintenanceFields()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var amenity = CreateTestAmenity(assetId: assetId);
            var schedule = CreateTestMaintenanceSchedule(assetId: assetId);

            _amenityRepositoryMock
                .Setup(r => r.GetAllAmenitiesAsync())
                .ReturnsAsync(new List<Amenity> { amenity });

            _scheduleRepositoryMock
                .Setup(r => r.GetActiveMaintenanceSchedulesByAssetIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new Dictionary<Guid, AssetMaintenanceSchedule> { { assetId, schedule } });

            // Act
            var result = await _service.GetAllAmenitiesAsync();

            // Assert
            Assert.IsNotNull(result);
            var amenityDto = result.First();
            Assert.IsTrue(amenityDto.IsUnderMaintenance);
            Assert.IsNotNull(amenityDto.MaintenanceStart);
            Assert.IsNotNull(amenityDto.MaintenanceEnd);
        }

        #endregion
    }
}
