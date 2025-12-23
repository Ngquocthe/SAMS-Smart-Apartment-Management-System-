using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Models;
using SAMS_BE.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAMS_BETests.Services
{
    [TestClass]
    public class AssetServiceTests
    {
        private Mock<IAssetRepository> _assetRepositoryMock = null!;
        private Mock<ILogger<AssetService>> _loggerMock = null!;
        private Mock<IAssetMaintenanceScheduleService> _maintenanceScheduleServiceMock = null!;
        private AssetService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _assetRepositoryMock = new Mock<IAssetRepository>();
            _loggerMock = new Mock<ILogger<AssetService>>();
            _maintenanceScheduleServiceMock = new Mock<IAssetMaintenanceScheduleService>();

            _service = new AssetService(
                _assetRepositoryMock.Object,
                _loggerMock.Object,
                _maintenanceScheduleServiceMock.Object);
        }

        #region Helper Methods

        private Asset CreateTestAsset(
            Guid? assetId = null,
            Guid? categoryId = null,
            string? code = null,
            string? name = null,
            string? status = "ACTIVE",
            Guid? apartmentId = null,
            Guid? blockId = null,
            string? location = null,
            int? maintenanceFrequency = null)
        {
            return new Asset
            {
                AssetId = assetId ?? Guid.NewGuid(),
                CategoryId = categoryId ?? Guid.NewGuid(),
                Code = code ?? "ASSET-001",
                Name = name ?? "Test Asset",
                Status = status ?? "ACTIVE",
                ApartmentId = apartmentId,
                BlockId = blockId,
                Location = location,
                MaintenanceFrequency = maintenanceFrequency,
                PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow),
                WarrantyExpire = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
                IsDelete = false,
                Category = new AssetCategory
                {
                    CategoryId = categoryId ?? Guid.NewGuid(),
                    Code = "CATEGORY-001",
                    Name = "Test Category"
                }
            };
        }

        private AssetCategory CreateTestCategory(Guid? categoryId = null, string? code = null, string? name = null)
        {
            return new AssetCategory
            {
                CategoryId = categoryId ?? Guid.NewGuid(),
                Code = code ?? "CATEGORY-001",
                Name = name ?? "Test Category",
                MaintenanceFrequency = 30,
                DefaultReminderDays = 3
            };
        }

        #endregion

        #region Constructor Test

        [TestMethod]
        public void AssetServiceTest()
        {
            // Arrange & Act
            var service = new AssetService(
                _assetRepositoryMock.Object,
                _loggerMock.Object,
                _maintenanceScheduleServiceMock.Object);

            // Assert
            Assert.IsNotNull(service);
        }

        #endregion

        #region GetAllAssetsAsync Tests

        [TestMethod]
        public async Task GetAllAssetsAsync_Success_ReturnsAssetDtos()
        {
            // Arrange
            var assets = new List<Asset>
            {
                CreateTestAsset(name: "Asset 1"),
                CreateTestAsset(name: "Asset 2")
            };

            _assetRepositoryMock
                .Setup(r => r.GetAllAssetsAsync())
                .ReturnsAsync(assets);

            // Act
            var result = await _service.GetAllAssetsAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
            _assetRepositoryMock.Verify(r => r.GetAllAssetsAsync(), Times.Once);
        }

        [TestMethod]
        public async Task GetAllAssetsAsync_EmptyList_ReturnsEmptyList()
        {
            // Arrange
            _assetRepositoryMock
                .Setup(r => r.GetAllAssetsAsync())
                .ReturnsAsync(new List<Asset>());

            // Act
            var result = await _service.GetAllAssetsAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public async Task GetAllAssetsAsync_Exception_ThrowsAndLogs()
        {
            // Arrange
            var exception = new Exception("Database error");
            _assetRepositoryMock
                .Setup(r => r.GetAllAssetsAsync())
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.GetAllAssetsAsync());
        }

        #endregion

        #region GetAssetByIdAsync Tests

        [TestMethod]
        public async Task GetAssetByIdAsync_AssetExists_ReturnsAssetDto()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var asset = CreateTestAsset(assetId, name: "Test Asset");

            _assetRepositoryMock
                .Setup(r => r.GetAssetByIdAsync(assetId))
                .ReturnsAsync(asset);

            // Act
            var result = await _service.GetAssetByIdAsync(assetId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(assetId, result.AssetId);
            Assert.AreEqual("Test Asset", result.Name);
            _assetRepositoryMock.Verify(r => r.GetAssetByIdAsync(assetId), Times.Once);
        }

        [TestMethod]
        public async Task GetAssetByIdAsync_AssetNotFound_ReturnsNull()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            _assetRepositoryMock
                .Setup(r => r.GetAssetByIdAsync(assetId))
                .ReturnsAsync((Asset?)null);

            // Act
            var result = await _service.GetAssetByIdAsync(assetId);

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region SearchAssetsAsync Tests

        [TestMethod]
        public async Task SearchAssetsAsync_Success_ReturnsMatchingAssets()
        {
            // Arrange
            var searchTerm = "Test";
            var assets = new List<Asset>
            {
                CreateTestAsset(name: "Test Asset")
            };

            _assetRepositoryMock
                .Setup(r => r.SearchAssetsAsync(searchTerm))
                .ReturnsAsync(assets);

            // Act
            var result = await _service.SearchAssetsAsync(searchTerm);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
        }

        #endregion

        #region GetAssetsByStatusAsync Tests

        [TestMethod]
        public async Task GetAssetsByStatusAsync_Success_ReturnsAssetsWithStatus()
        {
            // Arrange
            var status = "ACTIVE";
            var assets = new List<Asset>
            {
                CreateTestAsset(status: "ACTIVE")
            };

            _assetRepositoryMock
                .Setup(r => r.GetAssetsByStatusAsync(status))
                .ReturnsAsync(assets);

            // Act
            var result = await _service.GetAssetsByStatusAsync(status);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.All(a => a.Status == status));
        }

        #endregion

        #region GetAssetsByLocationAsync Tests

        [TestMethod]
        public async Task GetAssetsByLocationAsync_Success_ReturnsAssetsAtLocation()
        {
            // Arrange
            var location = "Building A";
            var assets = new List<Asset>
            {
                CreateTestAsset(location: "Building A - Floor 1")
            };

            _assetRepositoryMock
                .Setup(r => r.GetAssetsByLocationAsync(location))
                .ReturnsAsync(assets);

            // Act
            var result = await _service.GetAssetsByLocationAsync(location);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
        }

        #endregion

        #region GetAssetsByCategoryAsync Tests

        [TestMethod]
        public async Task GetAssetsByCategoryAsync_Success_ReturnsAssetsInCategory()
        {
            // Arrange
            var categoryId = Guid.NewGuid();
            var assets = new List<Asset>
            {
                CreateTestAsset(categoryId: categoryId)
            };

            _assetRepositoryMock
                .Setup(r => r.GetAssetsByCategoryAsync(categoryId))
                .ReturnsAsync(assets);

            // Act
            var result = await _service.GetAssetsByCategoryAsync(categoryId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
        }

        #endregion

        #region GetAssetsByApartmentAsync Tests

        [TestMethod]
        public async Task GetAssetsByApartmentAsync_Success_ReturnsAssetsInApartment()
        {
            // Arrange
            var apartmentId = Guid.NewGuid();
            var assets = new List<Asset>
            {
                CreateTestAsset(apartmentId: apartmentId)
            };

            _assetRepositoryMock
                .Setup(r => r.GetAssetsByApartmentAsync(apartmentId))
                .ReturnsAsync(assets);

            // Act
            var result = await _service.GetAssetsByApartmentAsync(apartmentId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
        }

        #endregion

        #region GetAssetsByBlockAsync Tests

        [TestMethod]
        public async Task GetAssetsByBlockAsync_Success_ReturnsAssetsInBlock()
        {
            // Arrange
            var blockId = Guid.NewGuid();
            var assets = new List<Asset>
            {
                CreateTestAsset(blockId: blockId)
            };

            _assetRepositoryMock
                .Setup(r => r.GetAssetsByBlockAsync(blockId))
                .ReturnsAsync(assets);

            // Act
            var result = await _service.GetAssetsByBlockAsync(blockId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
        }

        #endregion

        #region GetAssetsWithExpiredWarrantyAsync Tests

        [TestMethod]
        public async Task GetAssetsWithExpiredWarrantyAsync_Success_ReturnsAssets()
        {
            // Arrange
            var assets = new List<Asset>
            {
                CreateTestAsset()
            };

            _assetRepositoryMock
                .Setup(r => r.GetAssetsWithExpiredWarrantyAsync())
                .ReturnsAsync(assets);

            // Act
            var result = await _service.GetAssetsWithExpiredWarrantyAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
        }

        #endregion

        #region GetAssetsWithWarrantyExpiringInDaysAsync Tests

        [TestMethod]
        public async Task GetAssetsWithWarrantyExpiringInDaysAsync_Success_ReturnsAssets()
        {
            // Arrange
            var days = 30;
            var assets = new List<Asset>
            {
                CreateTestAsset()
            };

            _assetRepositoryMock
                .Setup(r => r.GetAssetsWithWarrantyExpiringInDaysAsync(days))
                .ReturnsAsync(assets);

            // Act
            var result = await _service.GetAssetsWithWarrantyExpiringInDaysAsync(days);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
        }

        #endregion

        #region GetAssetCountAsync Tests

        [TestMethod]
        public async Task GetAssetCountAsync_Success_ReturnsCount()
        {
            // Arrange
            var expectedCount = 10;
            _assetRepositoryMock
                .Setup(r => r.GetAssetCountAsync())
                .ReturnsAsync(expectedCount);

            // Act
            var result = await _service.GetAssetCountAsync();

            // Assert
            Assert.AreEqual(expectedCount, result);
        }

        #endregion

        #region GetAssetCountByStatusAsync Tests

        [TestMethod]
        public async Task GetAssetCountByStatusAsync_Success_ReturnsCount()
        {
            // Arrange
            var status = "ACTIVE";
            var expectedCount = 5;
            _assetRepositoryMock
                .Setup(r => r.GetAssetCountByStatusAsync(status))
                .ReturnsAsync(expectedCount);

            // Act
            var result = await _service.GetAssetCountByStatusAsync(status);

            // Assert
            Assert.AreEqual(expectedCount, result);
        }

        #endregion

        #region GetAssetCountByCategoryAsync Tests

        [TestMethod]
        public async Task GetAssetCountByCategoryAsync_Success_ReturnsCount()
        {
            // Arrange
            var categoryId = Guid.NewGuid();
            var expectedCount = 3;
            _assetRepositoryMock
                .Setup(r => r.GetAssetCountByCategoryAsync(categoryId))
                .ReturnsAsync(expectedCount);

            // Act
            var result = await _service.GetAssetCountByCategoryAsync(categoryId);

            // Assert
            Assert.AreEqual(expectedCount, result);
        }

        #endregion

        #region GetAllCategoriesAsync Tests

        [TestMethod]
        public async Task GetAllCategoriesAsync_Success_ReturnsCategoryDtos()
        {
            // Arrange
            var categories = new List<AssetCategory>
            {
                CreateTestCategory(name: "Category 1"),
                CreateTestCategory(name: "Category 2")
            };

            _assetRepositoryMock
                .Setup(r => r.GetAllCategoriesAsync())
                .ReturnsAsync(categories);

            // Act
            var result = await _service.GetAllCategoriesAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
        }

        #endregion

        #region GetCategoryByIdAsync Tests

        [TestMethod]
        public async Task GetCategoryByIdAsync_CategoryExists_ReturnsCategoryDto()
        {
            // Arrange
            var categoryId = Guid.NewGuid();
            var category = CreateTestCategory(categoryId, name: "Test Category");

            _assetRepositoryMock
                .Setup(r => r.GetCategoryByIdAsync(categoryId))
                .ReturnsAsync(category);

            // Act
            var result = await _service.GetCategoryByIdAsync(categoryId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(categoryId, result.CategoryId);
        }

        [TestMethod]
        public async Task GetCategoryByIdAsync_CategoryNotFound_ReturnsNull()
        {
            // Arrange
            var categoryId = Guid.NewGuid();
            _assetRepositoryMock
                .Setup(r => r.GetCategoryByIdAsync(categoryId))
                .ReturnsAsync((AssetCategory?)null);

            // Act
            var result = await _service.GetCategoryByIdAsync(categoryId);

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region GetActiveAssetsAsync Tests

        [TestMethod]
        public async Task GetActiveAssetsAsync_Success_ReturnsActiveAssets()
        {
            // Arrange
            var assets = new List<Asset>
            {
                CreateTestAsset(status: "ACTIVE")
            };

            _assetRepositoryMock
                .Setup(r => r.GetAssetsByStatusAsync("ACTIVE"))
                .ReturnsAsync(assets);

            // Act
            var result = await _service.GetActiveAssetsAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.All(a => a.Status == "ACTIVE"));
        }

        #endregion

        #region CreateAssetAsync Tests
        [TestMethod]
        public async Task CreateAssetAsync_CodeAlreadyExists_ThrowsInvalidOperationException()
        {
            // Arrange
            var categoryId = Guid.NewGuid();
            var existingAsset = CreateTestAsset(code: "ASSET-001");
            var createDto = new CreateAssetDto
            {
                CategoryId = categoryId.ToString(),
                Code = "ASSET-001",
                Name = "Test Asset"
            };

            _assetRepositoryMock
                .Setup(r => r.GetAssetByCodeAsync(createDto.Code))
                .ReturnsAsync(existingAsset);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await _service.CreateAssetAsync(createDto),
                $"Asset with code '{createDto.Code}' already exists");
        }
        #endregion

        #region UpdateAssetAsync Tests

        [TestMethod]
        public async Task UpdateAssetAsync_AssetNotFound_ReturnsNull()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var updateDto = new UpdateAssetDto
            {
                CategoryId = Guid.NewGuid().ToString(),
                Code = "ASSET-001",
                Name = "Test Asset",
                Status = "ACTIVE"
            };

            _assetRepositoryMock
                .Setup(r => r.GetAssetByIdAsync(assetId))
                .ReturnsAsync((Asset?)null);

            // Act
            var result = await _service.UpdateAssetAsync(updateDto, assetId);

            // Assert
            Assert.IsNull(result);
            _assetRepositoryMock.Verify(r => r.UpdateAssetAsync(It.IsAny<Asset>()), Times.Never);
        }

        [TestMethod]
        public async Task UpdateAssetAsync_CodeAlreadyExists_ThrowsInvalidOperationException()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var otherAssetId = Guid.NewGuid();
            var existingAsset = CreateTestAsset(assetId, code: "ASSET-001");
            var assetWithSameCode = CreateTestAsset(otherAssetId, code: "ASSET-002");
            var updateDto = new UpdateAssetDto
            {
                CategoryId = Guid.NewGuid().ToString(),
                Code = "ASSET-002", // Same code as other asset
                Name = "Test Asset",
                Status = "ACTIVE"
            };

            _assetRepositoryMock
                .Setup(r => r.GetAssetByIdAsync(assetId))
                .ReturnsAsync(existingAsset);

            _assetRepositoryMock
                .Setup(r => r.GetAssetByCodeAsync(updateDto.Code))
                .ReturnsAsync(assetWithSameCode);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await _service.UpdateAssetAsync(updateDto, assetId),
                $"Asset with code '{updateDto.Code}' already exists");
        }

        [TestMethod]
        public async Task UpdateAssetAsync_CategoryNotFound_ThrowsInvalidOperationException()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var existingAsset = CreateTestAsset(assetId, categoryId: Guid.NewGuid());
            var updateDto = new UpdateAssetDto
            {
                CategoryId = categoryId.ToString(), // Different category
                Code = "ASSET-001",
                Name = "Test Asset",
                Status = "ACTIVE"
            };

            _assetRepositoryMock
                .Setup(r => r.GetAssetByIdAsync(assetId))
                .ReturnsAsync(existingAsset);

            _assetRepositoryMock
                .Setup(r => r.GetAssetByCodeAsync(updateDto.Code))
                .ReturnsAsync((Asset?)null);

            _assetRepositoryMock
                .Setup(r => r.GetCategoryByIdAsync(categoryId))
                .ReturnsAsync((AssetCategory?)null);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await _service.UpdateAssetAsync(updateDto, assetId),
                $"Category with ID '{categoryId}' not found");
        }

        #endregion

        #region DeleteAssetAsync Tests

        [TestMethod]
        public async Task DeleteAssetAsync_Success_ReturnsTrue()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            _assetRepositoryMock
                .Setup(r => r.DeleteAssetAsync(assetId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteAssetAsync(assetId);

            // Assert
            Assert.IsTrue(result);
            _assetRepositoryMock.Verify(r => r.DeleteAssetAsync(assetId), Times.Once);
        }

        [TestMethod]
        public async Task DeleteAssetAsync_AssetNotFound_ReturnsFalse()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            _assetRepositoryMock
                .Setup(r => r.DeleteAssetAsync(assetId))
                .ReturnsAsync(false);

            // Act
            var result = await _service.DeleteAssetAsync(assetId);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task DeleteAssetAsync_Exception_ThrowsAndLogs()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var exception = new Exception("Database error");
            _assetRepositoryMock
                .Setup(r => r.DeleteAssetAsync(assetId))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.DeleteAssetAsync(assetId));
        }

        #endregion
    }
}
