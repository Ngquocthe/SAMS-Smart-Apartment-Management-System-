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
    public class AmenityPackageServiceTests
    {
        private Mock<IAmenityPackageRepository> _packageRepositoryMock = null!;
        private Mock<IAmenityRepository> _amenityRepositoryMock = null!;
        private Mock<ILogger<AmenityPackageService>> _loggerMock = null!;
        private AmenityPackageService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _packageRepositoryMock = new Mock<IAmenityPackageRepository>();
            _amenityRepositoryMock = new Mock<IAmenityRepository>();
            _loggerMock = new Mock<ILogger<AmenityPackageService>>();

            _service = new AmenityPackageService(
                _packageRepositoryMock.Object,
                _amenityRepositoryMock.Object,
                _loggerMock.Object);
        }

        #region Helper Methods

        private AmenityPackage CreateTestPackage(
            Guid? packageId = null,
            Guid? amenityId = null,
            string? name = null,
            int monthCount = 1,
            int? durationDays = null,
            string? periodUnit = "Month",
            int price = 100000,
            string? status = "ACTIVE")
        {
            return new AmenityPackage
            {
                PackageId = packageId ?? Guid.NewGuid(),
                AmenityId = amenityId ?? Guid.NewGuid(),
                Name = name ?? "1 Month Package",
                MonthCount = monthCount,
                DurationDays = durationDays,
                PeriodUnit = periodUnit,
                Price = price,
                Description = "Test package",
                Status = status ?? "ACTIVE"
            };
        }

        private Amenity CreateTestAmenity(Guid? amenityId = null, string? name = null)
        {
            return new Amenity
            {
                AmenityId = amenityId ?? Guid.NewGuid(),
                Code = "AMENITY-001",
                Name = name ?? "Swimming Pool",
                Status = "ACTIVE",
                FeeType = "Paid",
                HasMonthlyPackage = true,
                IsDelete = false
            };
        }

        #endregion

        #region Constructor Test

        [TestMethod]
        public void AmenityPackageServiceTest()
        {
            // Arrange & Act
            var service = new AmenityPackageService(
                _packageRepositoryMock.Object,
                _amenityRepositoryMock.Object,
                _loggerMock.Object);

            // Assert
            Assert.IsNotNull(service);
        }

        #endregion

        #region GetAllPackagesAsync Tests

        [TestMethod]
        public async Task GetAllPackagesAsync_Success_ReturnsPackageDtos()
        {
            // Arrange
            var packages = new List<AmenityPackage>
            {
                CreateTestPackage(name: "1 Month Package"),
                CreateTestPackage(name: "3 Month Package")
            };

            _packageRepositoryMock
                .Setup(r => r.GetAllPackagesAsync())
                .ReturnsAsync(packages);

            // Act
            var result = await _service.GetAllPackagesAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
            _packageRepositoryMock.Verify(r => r.GetAllPackagesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task GetAllPackagesAsync_EmptyList_ReturnsEmptyList()
        {
            // Arrange
            _packageRepositoryMock
                .Setup(r => r.GetAllPackagesAsync())
                .ReturnsAsync(new List<AmenityPackage>());

            // Act
            var result = await _service.GetAllPackagesAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public async Task GetAllPackagesAsync_Exception_ThrowsAndLogs()
        {
            // Arrange
            var exception = new Exception("Database error");
            _packageRepositoryMock
                .Setup(r => r.GetAllPackagesAsync())
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.GetAllPackagesAsync());
        }

        #endregion

        #region GetPackageByIdAsync Tests

        [TestMethod]
        public async Task GetPackageByIdAsync_PackageExists_ReturnsPackageDto()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var package = CreateTestPackage(packageId, name: "1 Month Package");

            _packageRepositoryMock
                .Setup(r => r.GetPackageByIdAsync(packageId))
                .ReturnsAsync(package);

            // Act
            var result = await _service.GetPackageByIdAsync(packageId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(packageId, result.PackageId);
            Assert.AreEqual("1 Month Package", result.Name);
            _packageRepositoryMock.Verify(r => r.GetPackageByIdAsync(packageId), Times.Once);
        }

        [TestMethod]
        public async Task GetPackageByIdAsync_PackageNotFound_ReturnsNull()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            _packageRepositoryMock
                .Setup(r => r.GetPackageByIdAsync(packageId))
                .ReturnsAsync((AmenityPackage?)null);

            // Act
            var result = await _service.GetPackageByIdAsync(packageId);

            // Assert
            Assert.IsNull(result);
            _packageRepositoryMock.Verify(r => r.GetPackageByIdAsync(packageId), Times.Once);
        }

        [TestMethod]
        public async Task GetPackageByIdAsync_Exception_ThrowsAndLogs()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var exception = new Exception("Database error");
            _packageRepositoryMock
                .Setup(r => r.GetPackageByIdAsync(packageId))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.GetPackageByIdAsync(packageId));
        }

        #endregion

        #region GetPackagesByAmenityIdAsync Tests

        [TestMethod]
        public async Task GetPackagesByAmenityIdAsync_Success_ReturnsPackageDtos()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            var packages = new List<AmenityPackage>
            {
                CreateTestPackage(amenityId: amenityId, name: "1 Month Package"),
                CreateTestPackage(amenityId: amenityId, name: "3 Month Package")
            };

            _packageRepositoryMock
                .Setup(r => r.GetPackagesByAmenityIdAsync(amenityId))
                .ReturnsAsync(packages);

            // Act
            var result = await _service.GetPackagesByAmenityIdAsync(amenityId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.All(p => p.AmenityId == amenityId));
            _packageRepositoryMock.Verify(r => r.GetPackagesByAmenityIdAsync(amenityId), Times.Once);
        }

        [TestMethod]
        public async Task GetPackagesByAmenityIdAsync_Exception_ThrowsAndLogs()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            var exception = new Exception("Database error");
            _packageRepositoryMock
                .Setup(r => r.GetPackagesByAmenityIdAsync(amenityId))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.GetPackagesByAmenityIdAsync(amenityId));
        }

        #endregion

        #region GetPackagesByStatusAsync Tests

        [TestMethod]
        public async Task GetPackagesByStatusAsync_Success_ReturnsPackagesWithStatus()
        {
            // Arrange
            var status = "ACTIVE";
            var packages = new List<AmenityPackage>
            {
                CreateTestPackage(status: "ACTIVE"),
                CreateTestPackage(status: "ACTIVE")
            };

            _packageRepositoryMock
                .Setup(r => r.GetPackagesByStatusAsync(status))
                .ReturnsAsync(packages);

            // Act
            var result = await _service.GetPackagesByStatusAsync(status);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.All(p => p.Status == status));
            _packageRepositoryMock.Verify(r => r.GetPackagesByStatusAsync(status), Times.Once);
        }

        #endregion

        #region GetActivePackagesByAmenityIdAsync Tests

        [TestMethod]
        public async Task GetActivePackagesByAmenityIdAsync_Success_ReturnsOnlyActivePackages()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            var packages = new List<AmenityPackage>
            {
                CreateTestPackage(amenityId: amenityId, status: "ACTIVE"),
                CreateTestPackage(amenityId: amenityId, status: "INACTIVE"),
                CreateTestPackage(amenityId: amenityId, status: "ACTIVE")
            };

            _packageRepositoryMock
                .Setup(r => r.GetPackagesByAmenityIdAsync(amenityId))
                .ReturnsAsync(packages);

            // Act
            var result = await _service.GetActivePackagesByAmenityIdAsync(amenityId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.All(p => p.Status == "ACTIVE"));
            _packageRepositoryMock.Verify(r => r.GetPackagesByAmenityIdAsync(amenityId), Times.Once);
        }

        [TestMethod]
        public async Task GetActivePackagesByAmenityIdAsync_NoActivePackages_ReturnsEmptyList()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            var packages = new List<AmenityPackage>
            {
                CreateTestPackage(amenityId: amenityId, status: "INACTIVE")
            };

            _packageRepositoryMock
                .Setup(r => r.GetPackagesByAmenityIdAsync(amenityId))
                .ReturnsAsync(packages);

            // Act
            var result = await _service.GetActivePackagesByAmenityIdAsync(amenityId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        #endregion

        #region CreatePackageAsync Tests

        [TestMethod]
        public async Task CreatePackageAsync_Success_MonthPackage_ReturnsPackageDto()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            var amenity = CreateTestAmenity(amenityId);
            var createDto = new CreateAmenityPackageDto
            {
                AmenityId = amenityId,
                Name = "1 Month Package",
                MonthCount = 1,
                DurationDays = null,
                PeriodUnit = "Month",
                Price = 100000,
                Description = "Test package",
                Status = "ACTIVE"
            };

            var package = CreateTestPackage(amenityId: amenityId);

            _amenityRepositoryMock
                .Setup(r => r.GetAmenityByIdAsync(amenityId))
                .ReturnsAsync(amenity);

            _packageRepositoryMock
                .Setup(r => r.GetPackagesByAmenityIdAsync(amenityId))
                .ReturnsAsync(new List<AmenityPackage>());

            _packageRepositoryMock
                .Setup(r => r.CreatePackageAsync(It.IsAny<AmenityPackage>()))
                .ReturnsAsync(package);

            // Act
            var result = await _service.CreatePackageAsync(createDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(package.PackageId, result.PackageId);
            _amenityRepositoryMock.Verify(r => r.GetAmenityByIdAsync(amenityId), Times.Once);
            _packageRepositoryMock.Verify(r => r.CreatePackageAsync(It.IsAny<AmenityPackage>()), Times.Once);
        }

        [TestMethod]
        public async Task CreatePackageAsync_Success_DayPackage_ReturnsPackageDto()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            var amenity = CreateTestAmenity(amenityId);
            var createDto = new CreateAmenityPackageDto
            {
                AmenityId = amenityId,
                Name = "7 Day Package",
                MonthCount = 0,
                DurationDays = 7,
                PeriodUnit = "Day",
                Price = 50000,
                Description = "Test package",
                Status = "ACTIVE"
            };

            var package = CreateTestPackage(amenityId: amenityId, periodUnit: "Day", durationDays: 7, monthCount: 0);

            _amenityRepositoryMock
                .Setup(r => r.GetAmenityByIdAsync(amenityId))
                .ReturnsAsync(amenity);

            _packageRepositoryMock
                .Setup(r => r.GetPackagesByAmenityIdAsync(amenityId))
                .ReturnsAsync(new List<AmenityPackage>());

            _packageRepositoryMock
                .Setup(r => r.CreatePackageAsync(It.IsAny<AmenityPackage>()))
                .ReturnsAsync(package);

            // Act
            var result = await _service.CreatePackageAsync(createDto);

            // Assert
            Assert.IsNotNull(result);
            _amenityRepositoryMock.Verify(r => r.GetAmenityByIdAsync(amenityId), Times.Once);
            _packageRepositoryMock.Verify(r => r.CreatePackageAsync(It.IsAny<AmenityPackage>()), Times.Once);
        }

        [TestMethod]
        public async Task CreatePackageAsync_AmenityNotFound_ThrowsArgumentException()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            var createDto = new CreateAmenityPackageDto
            {
                AmenityId = amenityId,
                Name = "1 Month Package",
                MonthCount = 1,
                PeriodUnit = "Month",
                Price = 100000,
                Status = "ACTIVE"
            };

            _amenityRepositoryMock
                .Setup(r => r.GetAmenityByIdAsync(amenityId))
                .ReturnsAsync((Amenity?)null);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await _service.CreatePackageAsync(createDto),
                $"Amenity with ID {amenityId} not found");
        }

        [TestMethod]
        public async Task CreatePackageAsync_DayPackage_InvalidDurationDays_ThrowsArgumentException()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            var amenity = CreateTestAmenity(amenityId);
            var createDto = new CreateAmenityPackageDto
            {
                AmenityId = amenityId,
                Name = "Day Package",
                MonthCount = 0,
                DurationDays = null, // Invalid: should have value for Day
                PeriodUnit = "Day",
                Price = 50000,
                Status = "ACTIVE"
            };

            _amenityRepositoryMock
                .Setup(r => r.GetAmenityByIdAsync(amenityId))
                .ReturnsAsync(amenity);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await _service.CreatePackageAsync(createDto),
                "DurationDays must be greater than 0 when PeriodUnit is 'Day'");
        }

        [TestMethod]
        public async Task CreatePackageAsync_DayPackage_InvalidMonthCount_ThrowsArgumentException()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            var amenity = CreateTestAmenity(amenityId);
            var createDto = new CreateAmenityPackageDto
            {
                AmenityId = amenityId,
                Name = "Day Package",
                MonthCount = 1, // Invalid: should be 0 for Day
                DurationDays = 7,
                PeriodUnit = "Day",
                Price = 50000,
                Status = "ACTIVE"
            };

            _amenityRepositoryMock
                .Setup(r => r.GetAmenityByIdAsync(amenityId))
                .ReturnsAsync(amenity);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await _service.CreatePackageAsync(createDto),
                "MonthCount should be 0 when PeriodUnit is 'Day'");
        }

        [TestMethod]
        public async Task CreatePackageAsync_MonthPackage_InvalidMonthCount_ThrowsArgumentException()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            var amenity = CreateTestAmenity(amenityId);
            var createDto = new CreateAmenityPackageDto
            {
                AmenityId = amenityId,
                Name = "Month Package",
                MonthCount = 0, // Invalid: should be > 0 for Month
                PeriodUnit = "Month",
                Price = 100000,
                Status = "ACTIVE"
            };

            _amenityRepositoryMock
                .Setup(r => r.GetAmenityByIdAsync(amenityId))
                .ReturnsAsync(amenity);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await _service.CreatePackageAsync(createDto),
                "MonthCount must be greater than 0 when PeriodUnit is 'Month' or null");
        }

        [TestMethod]
        public async Task CreatePackageAsync_MonthPackage_InvalidDurationDays_ThrowsArgumentException()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            var amenity = CreateTestAmenity(amenityId);
            var createDto = new CreateAmenityPackageDto
            {
                AmenityId = amenityId,
                Name = "Month Package",
                MonthCount = 1,
                DurationDays = 30, // Invalid: should be null or 0 for Month
                PeriodUnit = "Month",
                Price = 100000,
                Status = "ACTIVE"
            };

            _amenityRepositoryMock
                .Setup(r => r.GetAmenityByIdAsync(amenityId))
                .ReturnsAsync(amenity);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await _service.CreatePackageAsync(createDto),
                "DurationDays should be null or 0 when PeriodUnit is 'Month'");
        }

        [TestMethod]
        public async Task CreatePackageAsync_InvalidPeriodUnit_ThrowsArgumentException()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            var amenity = CreateTestAmenity(amenityId);
            var createDto = new CreateAmenityPackageDto
            {
                AmenityId = amenityId,
                Name = "Package",
                MonthCount = 1,
                PeriodUnit = "Invalid", // Invalid period unit
                Price = 100000,
                Status = "ACTIVE"
            };

            _amenityRepositoryMock
                .Setup(r => r.GetAmenityByIdAsync(amenityId))
                .ReturnsAsync(amenity);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await _service.CreatePackageAsync(createDto),
                "PeriodUnit must be either 'Day' or 'Month'");
        }

        [TestMethod]
        public async Task CreatePackageAsync_DuplicateDayPackage_ThrowsArgumentException()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            var amenity = CreateTestAmenity(amenityId);
            var existingPackage = CreateTestPackage(amenityId: amenityId, periodUnit: "Day", durationDays: 7, monthCount: 0);
            var createDto = new CreateAmenityPackageDto
            {
                AmenityId = amenityId,
                Name = "7 Day Package",
                MonthCount = 0,
                DurationDays = 7,
                PeriodUnit = "Day",
                Price = 50000,
                Status = "ACTIVE"
            };

            _amenityRepositoryMock
                .Setup(r => r.GetAmenityByIdAsync(amenityId))
                .ReturnsAsync(amenity);

            _packageRepositoryMock
                .Setup(r => r.GetPackagesByAmenityIdAsync(amenityId))
                .ReturnsAsync(new List<AmenityPackage> { existingPackage });

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await _service.CreatePackageAsync(createDto),
                "Đã tồn tại gói 7 ngày cho tiện ích này");
        }

        [TestMethod]
        public async Task CreatePackageAsync_DuplicateMonthPackage_ThrowsArgumentException()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            var amenity = CreateTestAmenity(amenityId);
            var existingPackage = CreateTestPackage(amenityId: amenityId, periodUnit: "Month", monthCount: 1);
            var createDto = new CreateAmenityPackageDto
            {
                AmenityId = amenityId,
                Name = "1 Month Package",
                MonthCount = 1,
                PeriodUnit = "Month",
                Price = 100000,
                Status = "ACTIVE"
            };

            _amenityRepositoryMock
                .Setup(r => r.GetAmenityByIdAsync(amenityId))
                .ReturnsAsync(amenity);

            _packageRepositoryMock
                .Setup(r => r.GetPackagesByAmenityIdAsync(amenityId))
                .ReturnsAsync(new List<AmenityPackage> { existingPackage });

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await _service.CreatePackageAsync(createDto),
                "Đã tồn tại gói 1 tháng cho tiện ích này");
        }

        [TestMethod]
        public async Task CreatePackageAsync_MonthPackagePriceTooLow_ThrowsArgumentException()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            var amenity = CreateTestAmenity(amenityId);
            var dailyPackage = CreateTestPackage(amenityId: amenityId, periodUnit: "Day", durationDays: 1, price: 100000);
            var createDto = new CreateAmenityPackageDto
            {
                AmenityId = amenityId,
                Name = "1 Month Package",
                MonthCount = 1,
                PeriodUnit = "Month",
                Price = 50000, // Lower than daily package price
                Status = "ACTIVE"
            };

            _amenityRepositoryMock
                .Setup(r => r.GetAmenityByIdAsync(amenityId))
                .ReturnsAsync(amenity);

            _packageRepositoryMock
                .Setup(r => r.GetPackagesByAmenityIdAsync(amenityId))
                .ReturnsAsync(new List<AmenityPackage> { dailyPackage });

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await _service.CreatePackageAsync(createDto),
                "Monthly package price (50000 VNĐ) must be higher than the highest daily package price (100000 VNĐ)");
        }

        [TestMethod]
        public async Task CreatePackageAsync_Exception_ThrowsAndLogs()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            var amenity = CreateTestAmenity(amenityId);
            var createDto = new CreateAmenityPackageDto
            {
                AmenityId = amenityId,
                Name = "1 Month Package",
                MonthCount = 1,
                PeriodUnit = "Month",
                Price = 100000,
                Status = "ACTIVE"
            };

            var exception = new Exception("Database error");

            _amenityRepositoryMock
                .Setup(r => r.GetAmenityByIdAsync(amenityId))
                .ReturnsAsync(amenity);

            _packageRepositoryMock
                .Setup(r => r.GetPackagesByAmenityIdAsync(amenityId))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.CreatePackageAsync(createDto));
        }

        #endregion

        #region UpdatePackageAsync Tests

        [TestMethod]
        public async Task UpdatePackageAsync_Success_ReturnsUpdatedPackageDto()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var amenityId = Guid.NewGuid();
            var existingPackage = CreateTestPackage(packageId, amenityId, name: "1 Month Package");
            var updateDto = new UpdateAmenityPackageDto
            {
                Name = "Updated Package",
                MonthCount = 2,
                PeriodUnit = "Month",
                Price = 200000,
                Description = "Updated description",
                Status = "ACTIVE"
            };

            var updatedPackage = CreateTestPackage(packageId, amenityId, name: "Updated Package", monthCount: 2, price: 200000);

            _packageRepositoryMock
                .Setup(r => r.GetPackageByIdAsync(packageId))
                .ReturnsAsync(existingPackage);

            _packageRepositoryMock
                .Setup(r => r.GetPackagesByAmenityIdAsync(amenityId))
                .ReturnsAsync(new List<AmenityPackage>());

            _packageRepositoryMock
                .Setup(r => r.UpdatePackageAsync(It.IsAny<AmenityPackage>()))
                .ReturnsAsync(updatedPackage);

            // Act
            var result = await _service.UpdatePackageAsync(updateDto, packageId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(packageId, result.PackageId);
            Assert.AreEqual("Updated Package", result.Name);
            _packageRepositoryMock.Verify(r => r.GetPackageByIdAsync(packageId), Times.Once);
            _packageRepositoryMock.Verify(r => r.UpdatePackageAsync(It.IsAny<AmenityPackage>()), Times.Once);
        }

        [TestMethod]
        public async Task UpdatePackageAsync_PackageNotFound_ReturnsNull()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var updateDto = new UpdateAmenityPackageDto
            {
                Name = "Updated Package",
                MonthCount = 1,
                PeriodUnit = "Month",
                Price = 100000,
                Status = "ACTIVE"
            };

            _packageRepositoryMock
                .Setup(r => r.GetPackageByIdAsync(packageId))
                .ReturnsAsync((AmenityPackage?)null);

            // Act
            var result = await _service.UpdatePackageAsync(updateDto, packageId);

            // Assert
            Assert.IsNull(result);
            _packageRepositoryMock.Verify(r => r.GetPackageByIdAsync(packageId), Times.Once);
            _packageRepositoryMock.Verify(r => r.UpdatePackageAsync(It.IsAny<AmenityPackage>()), Times.Never);
        }

        [TestMethod]
        public async Task UpdatePackageAsync_InvalidMonthCount_ThrowsArgumentException()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var amenityId = Guid.NewGuid();
            var existingPackage = CreateTestPackage(packageId, amenityId);
            var updateDto = new UpdateAmenityPackageDto
            {
                Name = "Package",
                MonthCount = 0, // Invalid
                PeriodUnit = "Month",
                Price = 100000,
                Status = "ACTIVE"
            };

            _packageRepositoryMock
                .Setup(r => r.GetPackageByIdAsync(packageId))
                .ReturnsAsync(existingPackage);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await _service.UpdatePackageAsync(updateDto, packageId));
        }

        [TestMethod]
        public async Task UpdatePackageAsync_MonthPackagePriceTooLow_ThrowsArgumentException()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var amenityId = Guid.NewGuid();
            var existingPackage = CreateTestPackage(packageId, amenityId, periodUnit: "Month");
            var dailyPackage = CreateTestPackage(amenityId: amenityId, periodUnit: "Day", durationDays: 1, price: 100000);
            var updateDto = new UpdateAmenityPackageDto
            {
                Name = "Package",
                MonthCount = 1,
                PeriodUnit = "Month",
                Price = 50000, // Lower than daily package
                Status = "ACTIVE"
            };

            _packageRepositoryMock
                .Setup(r => r.GetPackageByIdAsync(packageId))
                .ReturnsAsync(existingPackage);

            _packageRepositoryMock
                .Setup(r => r.GetPackagesByAmenityIdAsync(amenityId))
                .ReturnsAsync(new List<AmenityPackage> { dailyPackage });

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await _service.UpdatePackageAsync(updateDto, packageId),
                "Monthly package price (50000 VNĐ) must be higher than the highest daily package price (100000 VNĐ)");
        }

        [TestMethod]
        public async Task UpdatePackageAsync_Exception_ThrowsAndLogs()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var amenityId = Guid.NewGuid();
            var existingPackage = CreateTestPackage(packageId, amenityId);
            var updateDto = new UpdateAmenityPackageDto
            {
                Name = "Package",
                MonthCount = 1,
                PeriodUnit = "Month",
                Price = 100000,
                Status = "ACTIVE"
            };

            var exception = new Exception("Database error");

            _packageRepositoryMock
                .Setup(r => r.GetPackageByIdAsync(packageId))
                .ReturnsAsync(existingPackage);

            _packageRepositoryMock
                .Setup(r => r.GetPackagesByAmenityIdAsync(amenityId))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.UpdatePackageAsync(updateDto, packageId));
        }

        #endregion

        #region DeletePackageAsync Tests

        [TestMethod]
        public async Task DeletePackageAsync_Success_ReturnsTrue()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            _packageRepositoryMock
                .Setup(r => r.DeletePackageAsync(packageId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeletePackageAsync(packageId);

            // Assert
            Assert.IsTrue(result);
            _packageRepositoryMock.Verify(r => r.DeletePackageAsync(packageId), Times.Once);
        }

        [TestMethod]
        public async Task DeletePackageAsync_PackageNotFound_ReturnsFalse()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            _packageRepositoryMock
                .Setup(r => r.DeletePackageAsync(packageId))
                .ReturnsAsync(false);

            // Act
            var result = await _service.DeletePackageAsync(packageId);

            // Assert
            Assert.IsFalse(result);
            _packageRepositoryMock.Verify(r => r.DeletePackageAsync(packageId), Times.Once);
        }

        [TestMethod]
        public async Task DeletePackageAsync_Exception_ThrowsAndLogs()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var exception = new Exception("Database error");
            _packageRepositoryMock
                .Setup(r => r.DeletePackageAsync(packageId))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.DeletePackageAsync(packageId));
        }

        #endregion
    }
}
