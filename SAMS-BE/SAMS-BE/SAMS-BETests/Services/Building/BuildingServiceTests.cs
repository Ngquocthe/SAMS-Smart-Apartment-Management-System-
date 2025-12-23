using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SAMS_BE.DTOs.Request.Building;
using SAMS_BE.Helpers;
using SAMS_BE.Infrastructure.Persistence.Global.Models;
using SAMS_BE.Interfaces.IRepository.Building;
using SAMS_BE.Interfaces.IRepository.GlobalAdmin;
using SAMS_BE.Services.Building;
using SAMS_BE.Utils;
using System;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SAMS_BE.Services.Building.Tests
{
    [TestClass]
    public class BuildingServiceTests
    {
        private Mock<IBuildingRepository> _buildingRepoMock = null!;
        private Mock<IScriptRepository> _scriptRepoMock = null!;
        private Mock<IFileStorageHelper> _fileStorageMock = null!;
        private Mock<IHttpContextAccessor> _httpMock = null!;

        private BuildingService _service = null!;
        private readonly Guid _userId = Guid.NewGuid();

        #region Setup

        [TestInitialize]
        public void Setup()
        {
            _buildingRepoMock = new Mock<IBuildingRepository>();
            _scriptRepoMock = new Mock<IScriptRepository>();
            _fileStorageMock = new Mock<IFileStorageHelper>();
            _httpMock = new Mock<IHttpContextAccessor>();

            var context = new DefaultHttpContext();
            context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("sub", _userId.ToString())
            }));

            _httpMock.Setup(x => x.HttpContext).Returns(context);

            _service = new BuildingService(
                _buildingRepoMock.Object,
                _scriptRepoMock.Object,
                _httpMock.Object,
                _fileStorageMock.Object
            );
        }

        private CreateBuildingRequest CreateValidRequest(bool withAvatar = false)
        {
            return new CreateBuildingRequest
            {
                Code = "HN-GREENPARK",
                BuildingName = "Green Park Tower",
                Description = "Test building",
                TotalAreaM2 = 1000,
                Latitude = (decimal?)21.0,
                Longitude = (decimal?)105.0,
                OpeningDate = DateTime.UtcNow,
                Avatar = withAvatar ? CreateFakeFormFile(1024) : null
            };
        }

        private IFormFile CreateFakeFormFile(long size)
        {
            var content = new byte[size];
            return new FormFile(new MemoryStream(content), 0, size, "avatar", "avatar.png");
        }

        #endregion

        #region Validation Tests

        [TestMethod]
        public async Task CreateTenantAsync_NullRequest_ThrowsArgumentNullException()
        {
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(
                () => _service.CreateTenantAsync(null!, CancellationToken.None));
        }

        [TestMethod]
        public async Task CreateTenantAsync_EmptyCode_ThrowsException()
        {
            var req = CreateValidRequest();
            req.Code = " ";

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _service.CreateTenantAsync(req, CancellationToken.None));
        }

        [TestMethod]
        public async Task CreateTenantAsync_CodeTooLong_ThrowsException()
        {
            var req = CreateValidRequest();
            req.Code = new string('A', 31);

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _service.CreateTenantAsync(req, CancellationToken.None));
        }

        [TestMethod]
        public async Task CreateTenantAsync_InvalidCodeFormat_ThrowsException()
        {
            var req = CreateValidRequest();
            req.Code = "ABC 123";

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _service.CreateTenantAsync(req, CancellationToken.None));
        }

        [TestMethod]
        public async Task CreateTenantAsync_CodeAlreadyExists_ThrowsException()
        {
            var req = CreateValidRequest();

            _buildingRepoMock
                .Setup(r => r.checkExistBuilding(req))
                .ReturnsAsync(true);

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _service.CreateTenantAsync(req, CancellationToken.None));
        }

        [TestMethod]
        public async Task CreateTenantAsync_EmptyBuildingName_ThrowsException()
        {
            var req = CreateValidRequest();
            req.BuildingName = "";

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _service.CreateTenantAsync(req, CancellationToken.None));
        }

        [TestMethod]
        public async Task CreateTenantAsync_NegativeTotalArea_ThrowsException()
        {
            var req = CreateValidRequest();
            req.TotalAreaM2 = -1;

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _service.CreateTenantAsync(req, CancellationToken.None));
        }

        [TestMethod]
        public async Task CreateTenantAsync_AvatarTooLarge_ThrowsException()
        {
            var req = CreateValidRequest(true);
            req.Avatar = CreateFakeFormFile(6 * 1024 * 1024);

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _service.CreateTenantAsync(req, CancellationToken.None));
        }

        #endregion

        #region Script Execution Tests

        [TestMethod]
        public async Task CreateTenantAsync_ScriptExecutionFails_ThrowsWrappedException()
        {
            var req = CreateValidRequest();

            _scriptRepoMock
                .Setup(r => r.ExecuteSqlScriptAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("SQL error"));

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _service.CreateTenantAsync(req, CancellationToken.None));
        }

        #endregion

        #region Avatar Tests

        [TestMethod]
        public async Task CreateTenantAsync_WithAvatar_SavesFile()
        {
            // Arrange
            var req = CreateValidRequest(withAvatar: true);

            var storedFile = new SAMS_BE.Models.File
            {
                StoragePath = "path/avatar.png"
            };

            _fileStorageMock
                .Setup(s => s.SaveAsync(
                    It.IsAny<IFormFile>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(storedFile);

            // Act
            await _service.CreateTenantAsync(req, CancellationToken.None);

            // Assert
            _fileStorageMock.Verify(
                s => s.SaveAsync(
                    It.IsAny<IFormFile>(),
                    It.Is<string>(p => p.Contains("avatarBuildings")),
                    It.Is<string>(u => !string.IsNullOrWhiteSpace(u))),
                Times.Once);
        }

        [TestMethod]
        public async Task CreateTenantAsync_NoAvatar_DoesNotSaveFile()
        {
            var req = CreateValidRequest(false);

            await _service.CreateTenantAsync(req, CancellationToken.None);

            _fileStorageMock.Verify(
                s => s.SaveAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        #endregion

        #region Success Case

        [TestMethod]
        public async Task CreateTenantAsync_ValidRequest_CreatesBuildingSuccessfully()
        {
            // Arrange
            var req = CreateValidRequest(withAvatar: true);

            _fileStorageMock
                .Setup(s => s.SaveAsync(
                    It.IsAny<IFormFile>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new SAMS_BE.Models.File
                {
                    StoragePath = "avatar.png"
                });

            building? savedBuilding = null;

            _buildingRepoMock
                .Setup(r => r.SaveBuilding(It.IsAny<building>(), It.IsAny<CancellationToken>()))
                .Callback<building, CancellationToken>((b, _) => savedBuilding = b)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateTenantAsync(req, CancellationToken.None);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(req.Code, result.code);
            Assert.AreEqual(req.BuildingName, result.building_name);
            Assert.AreEqual(_userId, result.created_by);
            Assert.AreEqual("avatar.png", result.image_url);

            Assert.IsNotNull(savedBuilding);

            _fileStorageMock.Verify(
                s => s.SaveAsync(
                    It.IsAny<IFormFile>(),
                    It.Is<string>(p => p.Contains("avatarBuildings")),
                    It.Is<string>(u => u == _userId.ToString())),
                Times.Once);

            _scriptRepoMock.Verify(
                r => r.ExecuteSqlScriptAsync(It.IsAny<string>()),
                Times.Once);

            _buildingRepoMock.Verify(
                r => r.SaveBuilding(It.IsAny<building>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }


        #endregion

        [TestMethod]
        public async Task CreateTenantAsync_AvatarSavedButStoragePathNull_ImageUrlIsNull()
        {
            // Arrange
            var req = CreateValidRequest(withAvatar: true);

            var storedFile = new SAMS_BE.Models.File
            {
                StoragePath = null
            };

            _fileStorageMock
                .Setup(s => s.SaveAsync(
                    It.IsAny<IFormFile>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(storedFile);

            // Act
            var result = await _service.CreateTenantAsync(req, CancellationToken.None);

            // Assert
            Assert.IsNull(result.image_url);
        }

    }
}
