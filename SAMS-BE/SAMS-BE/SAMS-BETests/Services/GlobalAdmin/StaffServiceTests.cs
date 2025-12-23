using Moq;
using SAMS_BE.DTOs.Response.Staff;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Interfaces.IRepository.GlobalAdmin;
using SAMS_BE.Interfaces.IService.Keycloak;
using SAMS_BE.Interfaces.IMail;
using SAMS_BE.Helpers;
using Microsoft.AspNetCore.Hosting;
using SAMS_BE.DTOs.Request.Staff;
using SAMS_BE.DTOs.Request.Keycloak;
using Microsoft.AspNetCore.Http;
using SAMS_BE.Infrastructure.Persistence.Global.Models;
using SAMS_BE.Models;
using SAMS_BE.Interfaces.IService.IBuilding;


namespace SAMS_BE.Services.GlobalAdmin.Tests
{
    [TestClass]
    public class StaffServiceTests
    {
        private Mock<IStaffRepository> _repoMock = null!;
        private Mock<IKeycloakRoleService> _kcRoleMock = null!;
        private Mock<IFileStorageHelper> _storageMock = null!;
        private Mock<IUserRepository> _userRepoMock = null!;
        private Mock<IAdminUserRepository> _adminUserRepoMock = null!;
        private Mock<IWebHostEnvironment> _envMock = null!;
        private Mock<IEmailSender> _emailSenderMock = null!;
        private Mock<IBuildingService> _buildingServiceMock = null!;

        private StaffService _service = null!;

        #region Setup

        [TestInitialize]
        public void Setup()
        {
            _repoMock = new Mock<IStaffRepository>();
            _kcRoleMock = new Mock<IKeycloakRoleService>();
            _storageMock = new Mock<IFileStorageHelper>();
            _userRepoMock = new Mock<IUserRepository>();
            _adminUserRepoMock = new Mock<IAdminUserRepository>();
            _envMock = new Mock<IWebHostEnvironment>();
            _emailSenderMock = new Mock<IEmailSender>();
            _buildingServiceMock = new Mock<IBuildingService>();

            _service = new StaffService(
                _repoMock.Object,
                _kcRoleMock.Object,
                _storageMock.Object,
                _userRepoMock.Object,
                _adminUserRepoMock.Object,
                _envMock.Object,
                _emailSenderMock.Object,
                _buildingServiceMock.Object
            );
        }


        private StaffQuery CreateQuery(int page = 1, int pageSize = 20)
        {
            return new StaffQuery
            {
                Page = page,
                PageSize = pageSize
            };
        }

        private List<StaffListItemDto> CreateStaffList(int count = 2)
        {
            var list = new List<StaffListItemDto>();
            for (int i = 0; i < count; i++)
            {
                list.Add(new StaffListItemDto
                {
                    StaffCode = Guid.NewGuid(),
                    FullName = $"Staff {i + 1}"
                });
            }
            return list;
        }

        #endregion

        #region CreateAsync Helpers

        private const string SCHEMA = "test";

        private StaffCreateRequest CreateValidStaffRequest()
        {
            return new StaffCreateRequest
            {
                Username = "staff01",
                Email = "staff01@test.com",
                Phone = "0912345678",
                FirstName = "John",
                LastName = "Doe",
                AccessRoles = new List<string> { "STAFF" }
            };
        }

        private StaffCreateRequest CreateStaffRequest_NoRole()
        {
            return new StaffCreateRequest
            {
                Username = "staff01",
                Email = "staff01@test.com",
                FirstName = "Test",
                LastName = "User",
                Phone = "0912345678",

                AccessRoles = new List<string>(),
            };
        }


        private void MockKeycloakCreateSuccess()
        {
            _kcRoleMock
                .Setup(k => k.CreateUserAsync(
                    It.IsAny<KeycloakUserCreateDto>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid.NewGuid().ToString(), "Temp@123"));
        }

        private SAMS_BE.Models.File StoredFile(string? path = "file.png")
        {
            return new SAMS_BE.Models.File
            {
                StoragePath = path
            };
        }

        #endregion


        #region SearchAsync Tests

        [TestMethod]
        public async Task SearchAsync_ValidQuery_ReturnsPagedResult()
        {
            // Arrange
            var schema = "test";
            var query = CreateQuery(page: 2, pageSize: 20);
            var staffs = CreateStaffList(3);

            _repoMock
                .Setup(r => r.SearchAsync(schema, query, It.IsAny<CancellationToken>()))
                .ReturnsAsync((staffs, 10));

            // Act
            var result = await _service.SearchAsync(schema, query, CancellationToken.None);

            // Assert
            Assert.AreEqual(3, result.Items.Count);
            Assert.AreEqual(10, result.Total);
            Assert.AreEqual(2, result.Page);
            Assert.AreEqual(20, result.PageSize);

            _repoMock.Verify(
                r => r.SearchAsync(schema, query, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task SearchAsync_PageZero_NormalizedToOne()
        {
            var schema = "test";
            var query = CreateQuery(page: 0, pageSize: 20);

            _repoMock
                .Setup(r => r.SearchAsync(schema, query, It.IsAny<CancellationToken>()))
                .ReturnsAsync((CreateStaffList(), 5));

            var result = await _service.SearchAsync(schema, query, CancellationToken.None);

            Assert.AreEqual(1, result.Page);
        }

        [TestMethod]
        public async Task SearchAsync_PageNegative_NormalizedToOne()
        {
            var schema = "test";
            var query = CreateQuery(page: -5, pageSize: 20);

            _repoMock
                .Setup(r => r.SearchAsync(schema, query, It.IsAny<CancellationToken>()))
                .ReturnsAsync((CreateStaffList(), 5));

            var result = await _service.SearchAsync(schema, query, CancellationToken.None);

            Assert.AreEqual(1, result.Page);
        }

        [TestMethod]
        public async Task SearchAsync_PageSizeZero_NormalizedToOne()
        {
            var schema = "test";
            var query = CreateQuery(page: 1, pageSize: 0);

            _repoMock
                .Setup(r => r.SearchAsync(schema, query, It.IsAny<CancellationToken>()))
                .ReturnsAsync((CreateStaffList(), 5));

            var result = await _service.SearchAsync(schema, query, CancellationToken.None);

            Assert.AreEqual(1, result.PageSize);
        }

        [TestMethod]
        public async Task SearchAsync_PageSizeTooLarge_ClampedTo200()
        {
            var schema = "test";
            var query = CreateQuery(page: 1, pageSize: 500);

            _repoMock
                .Setup(r => r.SearchAsync(schema, query, It.IsAny<CancellationToken>()))
                .ReturnsAsync((CreateStaffList(), 5));

            var result = await _service.SearchAsync(schema, query, CancellationToken.None);

            Assert.AreEqual(200, result.PageSize);
        }

        [TestMethod]
        public async Task SearchAsync_EmptyResult_ReturnsEmptyList()
        {
            var schema = "test";
            var query = CreateQuery();

            _repoMock
                .Setup(r => r.SearchAsync(schema, query, It.IsAny<CancellationToken>()))
                .ReturnsAsync((new List<StaffListItemDto>(), 0));

            var result = await _service.SearchAsync(schema, query, CancellationToken.None);

            Assert.IsNotNull(result.Items);
            Assert.AreEqual(0, result.Items.Count);
            Assert.AreEqual(0, result.Total);
        }

        [TestMethod]
        public async Task SearchAsync_RepositoryThrows_ExceptionBubblesUp()
        {
            var schema = "test";
            var query = CreateQuery();

            _repoMock
                .Setup(r => r.SearchAsync(schema, query, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("DB error"));

            await Assert.ThrowsExceptionAsync<Exception>(
                () => _service.SearchAsync(schema, query, CancellationToken.None));
        }

        #endregion

        #region CreateAsync Validation Tests

        [TestMethod]
        public async Task CreateAsync_UsernameExistsInBuilding_Throws()
        {
            var req = CreateValidStaffRequest();

            _userRepoMock
                .Setup(r => r.ExistsUsernameAsync(SCHEMA, req.Username!, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _service.CreateAsync(SCHEMA, req, CancellationToken.None));
        }

        [TestMethod]
        public async Task CreateAsync_UsernameExistsInGlobalAdmin_Throws()
        {
            var req = CreateValidStaffRequest();

            _adminUserRepoMock
                .Setup(r => r.ExistsUsernameAsync(req.Username!, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _service.CreateAsync(SCHEMA, req, CancellationToken.None));
        }

        [TestMethod]
        public async Task CreateAsync_UsernameExistsInKeycloak_Throws()
        {
            var req = CreateValidStaffRequest();

            _kcRoleMock
                .Setup(k => k.FindUserIdByUsernameAsync(req.Username!, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Guid.NewGuid().ToString());

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _service.CreateAsync(SCHEMA, req, CancellationToken.None));
        }

        [TestMethod]
        public async Task CreateAsync_EmailExistsInBuilding_Throws()
        {
            var req = CreateValidStaffRequest();

            _userRepoMock
                .Setup(r => r.ExistsEmailAsync(SCHEMA, req.Email!, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _service.CreateAsync(SCHEMA, req, CancellationToken.None));
        }

        [TestMethod]
        public async Task CreateAsync_EmailExistsInGlobalAdmin_Throws()
        {
            var req = CreateValidStaffRequest();

            _adminUserRepoMock
                .Setup(r => r.ExistsEmailAsync(req.Email!, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _service.CreateAsync(SCHEMA, req, CancellationToken.None));
        }

        [TestMethod]
        public async Task CreateAsync_EmailExistsInKeycloak_Throws()
        {
            var req = CreateValidStaffRequest();

            _kcRoleMock
                .Setup(k => k.FindUserIdByEmailAsync(req.Email!, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Guid.NewGuid().ToString());

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _service.CreateAsync(SCHEMA, req, CancellationToken.None));
        }

        [TestMethod]
        public async Task CreateAsync_InvalidPhone_Throws()
        {
            var req = CreateValidStaffRequest();
            req.Phone = "123";

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _service.CreateAsync(SCHEMA, req, CancellationToken.None));
        }

        [TestMethod]
        public async Task CreateAsync_PhoneExists_Throws()
        {
            var req = CreateValidStaffRequest();

            _userRepoMock
                .Setup(r => r.ExistsPhoneAsync(SCHEMA, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _service.CreateAsync(SCHEMA, req, CancellationToken.None));
        }

        [TestMethod]
        public async Task CreateAsync_TaxCodeExists_Throws()
        {
            var req = CreateValidStaffRequest();
            req.TaxCode = "TAX001";

            _repoMock
                .Setup(r => r.ExistsTaxCodeAsync(SCHEMA, req.TaxCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _service.CreateAsync(SCHEMA, req, CancellationToken.None));
        }

        [TestMethod]
        public async Task CreateAsync_SocialInsuranceExists_Throws()
        {
            var req = CreateValidStaffRequest();
            req.SocialInsuranceNo = "SI001";

            _repoMock
                .Setup(r => r.ExistsSocialInsuranceNoAsync(SCHEMA, req.SocialInsuranceNo, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _service.CreateAsync(SCHEMA, req, CancellationToken.None));
        }

        #endregion

        #region CreateAsync File Upload Tests

        #endregion

        #region CreateAsync Keycloak & Persistence Tests

        [TestMethod]
        public async Task CreateAsync_KeycloakUserIdInvalid_Throws()
        {
            var req = CreateValidStaffRequest();

            _kcRoleMock
                .Setup(k => k.CreateUserAsync(
                    It.IsAny<KeycloakUserCreateDto>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(("invalid-guid", "Temp@123"));

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _service.CreateAsync(SCHEMA, req, CancellationToken.None));
        }
        #endregion

    }
}
