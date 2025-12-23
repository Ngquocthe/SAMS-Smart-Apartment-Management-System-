using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SAMS_BE.DTOs;
using SAMS_BE.Helpers;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Models;
using SAMS_BE.Repositories;
using SAMS_BE.Services;
using SAMS_BE.Tenant;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileEntity = SAMS_BE.Models.File;

namespace SAMS_BE.Services.Tests
{
    [TestClass]
    public class TicketServiceTests
    {
        private Mock<ITicketRepository> _ticketRepositoryMock = null!;
        private Mock<IUserRepository> _userRepositoryMock = null!;
        private Mock<IFileStorageHelper> _fileStorageHelperMock = null!;
        private Mock<BuildingManagementContext> _contextMock = null!;
        private Mock<ILogger<TicketService>> _loggerMock = null!;
        private TicketService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _ticketRepositoryMock = new Mock<ITicketRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _fileStorageHelperMock = new Mock<IFileStorageHelper>();
            _contextMock = new Mock<BuildingManagementContext>();
            _loggerMock = new Mock<ILogger<TicketService>>();
            _service = new TicketService(_ticketRepositoryMock.Object, _userRepositoryMock.Object, _fileStorageHelperMock.Object, _contextMock.Object, _loggerMock.Object);
        }

        #region Helper Methods

        private Ticket CreateTestTicket(Guid? ticketId = null, string? status = null, string? priority = null, string scope = "Tòa nhà", Guid? apartmentId = null)
        {
            return new Ticket
            {
                TicketId = ticketId ?? Guid.NewGuid(),
                Subject = "Test Ticket",
                Description = "Test Description",
                Category = "An ninh",
                Priority = priority ?? "Bình thường",
                Status = status ?? "Mới tạo",
                Scope = scope,
                ApartmentId = apartmentId,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = Guid.NewGuid()
            };
        }

        #endregion

        #region CreateAsync Tests



        // UTCID01: Subject empty
        // Precondition: Can connect with server
        // Input: Subject = "" (empty), Description = "Test Description" (valid), Category = "Hóa đơn" (valid), Priority = "Bình thường" (valid), Scope = "Tòa nhà" (valid)
        // Expected: ArgumentException thrown with message "Tiêu đề là bắt buộc."
        // Exception: ArgumentException - Loại exception: ArgumentException
        // Result Type: A (Abnormal) - Loại kết quả: Bất thường
        // Log message: None - Không có log
        [TestMethod]
        public async Task CreateAsync_SubjectEmpty_ThrowsArgumentException()
        {
            // Arrange - Precondition: Can connect with server
            var dto = new CreateTicketDto
            {
                Subject = "",
                Description = "Test Description",
                Category = "Hóa đơn",
                Priority = "Bình thường",
                Scope = "Tòa nhà"
            };

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.CreateAsync(dto));
            Assert.AreEqual("Tiêu đề là bắt buộc.", ex.Message);
        }

        // UTCID02: Subject whitespace only
        // Precondition: Can connect with server
        // Input: Subject = "   " (whitespace), Description = "Test Description" (valid), Category = "Khiếu nại" (valid), Priority = "Bình thường" (valid), Scope = "Tòa nhà" (valid)
        // Expected: ArgumentException thrown with message "Tiêu đề là bắt buộc."
        // Exception: ArgumentException - Loại exception: ArgumentException
        // Result Type: A (Abnormal) - Loại kết quả: Bất thường
        // Log message: None - Không có log
        [TestMethod]
        public async Task CreateAsync_SubjectWhitespace_ThrowsArgumentException()
        {
            // Arrange - Precondition: Can connect with server
            var dto = new CreateTicketDto
            {
                Subject = "   ",
                Description = "Test Description",
                Category = "Khiếu nại",
                Priority = "Bình thường",
                Scope = "Tòa nhà"
            };

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.CreateAsync(dto));
            Assert.AreEqual("Tiêu đề là bắt buộc.", ex.Message);
        }

        //  UTCID03: Scope = Theo căn hộ nhưng không có ApartmentId
        // Precondition: Can connect with server
        // Input: Subject = "Test Subject" (valid), Description = "Test Description" (valid), Category = "An ninh" (valid), Priority = "Bình thường" (valid), Scope = "Theo căn hộ", ApartmentId = null
        // Expected: ArgumentException thrown with message chứa "ApartmentId là bắt buộc"
        // Exception: ArgumentException
        // Result Type: A (Abnormal)
        // Log message: None
        [TestMethod]
        public async Task CreateAsync_ScopeApartmentWithoutApartmentId_ThrowsArgumentException()
        {
            var dto = new CreateTicketDto
            {
                Subject = "Test Subject",
                Description = "Test Description",
                Category = "An ninh",
                Priority = "Bình thường",
                Scope = "Theo căn hộ",
                ApartmentId = null
            };

            var ex = await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.CreateAsync(dto));
            Assert.IsTrue(ex.Message.Contains("Căn hộ là bắt buộc") || ex.Message.Contains("ApartmentId là bắt buộc"));
        }

        // UTCID04: Scope = Theo căn hộ với ApartmentId hợp lệ
        // Precondition: Can connect with server
        // Input: Subject = "Test Subject" (valid), Description = "Test Description" (valid), Category = "An ninh" (valid), Priority = "Bình thường" (valid), Scope = "Theo căn hộ", ApartmentId = {guid}
        // Expected: TicketDto trả về, Scope = "Theo căn hộ", ApartmentId được set
        // Exception: None (Success)
        // Result Type: N (Normal)
        // Log message: None
        [TestMethod]
        public async Task CreateAsync_ScopeApartmentWithApartmentId_Success()
        {
            var apartmentId = Guid.NewGuid();
            var dto = new CreateTicketDto
            {
                Subject = "Test Subject",
                Description = "Test Description",
                Category = "An ninh",
                Priority = "Bình thường",
                Scope = "Theo căn hộ",
                ApartmentId = apartmentId
            };

            _ticketRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Ticket>()))
                .ReturnsAsync((Ticket t) => t);

            var result = await _service.CreateAsync(dto);

            Assert.IsNotNull(result);
            Assert.AreEqual("Theo căn hộ", result.Scope);
            Assert.AreEqual(apartmentId, result.ApartmentId);
        }

        // UTCID05: Subject quá dài (256 chars) - Boundary test
        // Precondition: Can connect with server
        // Input: Subject = 256 chars (exceeds max 255), Category = "Vệ sinh" (valid), Priority = "Bình thường" (valid)
        // Expected: ArgumentException thrown (validation at model binding layer) or service validation
        // Exception: ArgumentException - Loại exception: ArgumentException
        // Result Type: B (Boundary) - Loại kết quả: Biên
        // Log message: None - Không có log
        [TestMethod]
        public async Task CreateAsync_SubjectTooLong_256Chars_ThrowsException()
        {
            // Arrange - Precondition: Can connect with server
            var dto = new CreateTicketDto
            {
                Subject = new string('A', 256), // 256 chars - vượt quá 255 (boundary)
                Description = "Test Description",
                Category = "Vệ sinh",
                Priority = "Bình thường",
                Scope = "Tòa nhà"
            };

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.CreateAsync(dto));
            // Cho phép thông báo chi tiết hoặc fallback message, ưu tiên tiếng Việt trên UI
            Assert.IsTrue(ex.Message.Contains("Tiêu đề không được vượt quá 255 ký tự") || ex.Message.Contains("Tiêu đề"));
        }

        // UTCID05B: Subject boundary - exactly 255 chars (valid)
        // Precondition: Can connect with server
        // Input: Subject = 255 chars (max valid length), Category = "Bãi đỗ xe" (valid), Priority = "Bình thường" (valid)
        // Expected: TicketDto returned successfully
        // Exception: None(Success) - Không có exception (Thành công)
        // Result Type: B (Boundary) - Loại kết quả: Biên
        // Log message: None - Không có log
        [TestMethod]
        public async Task CreateAsync_SubjectBoundary_255Chars_Success()
        {
            // Arrange - Precondition: Can connect with server
            var dto = new CreateTicketDto
            {
                Subject = new string('A', 255), // 255 chars - exactly max length (boundary)
                Description = "Test Description",
                Category = "Bãi đỗ xe",
                Priority = "Bình thường",
                Scope = "Tòa nhà"
            };

            _ticketRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Ticket>()))
                .ReturnsAsync((Ticket t) => t);

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(255, result.Subject.Length);
            _ticketRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Ticket>()), Times.Once);
        }

        // UTCID06: Subject boundary - 1 char (invalid, dưới tối thiểu 3 ký tự)
        // Precondition: Can connect with server
        // Input: Subject = "A" (1 char - invalid), Category = "Tiện ích" (valid), Priority = "Bình thường" (valid), Scope = "Tòa nhà"
        // Expected: ArgumentException thrown với message chứa "ít nhất 3 ký tự"
        // Exception: ArgumentException
        // Result Type: B (Boundary)
        // Log message: None
        [TestMethod]
        public async Task CreateAsync_SubjectBoundary_1Char_ThrowsArgumentException()
        {
            // Arrange - Precondition: Can connect with server
            var dto = new CreateTicketDto
            {
                Subject = "A", // 1 char - invalid now
                Description = "Test Description",
                Category = "Tiện ích",
                Priority = "Bình thường",
                Scope = "Tòa nhà"
            };

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.CreateAsync(dto));
            Assert.IsTrue(ex.Message.Contains("ít nhất 3 ký tự") || ex.Message.Contains("3 ký tự"));
        }

        // UTCID07: Category null
        // Precondition: Can connect with server
        // Input: Subject = "Test Subject" (valid), Category = null, Priority = "Bình thường" (valid)
        // Expected: ArgumentException thrown with message "Category là bắt buộc."
        // Exception: ArgumentException - Loại exception: ArgumentException
        // Result Type: A (Abnormal) - Loại kết quả: Bất thường
        // Log message: None - Không có log
        [TestMethod]
        public async Task CreateAsync_CategoryNull_ThrowsArgumentException()
        {
            // Arrange - Precondition: Can connect with server
            var dto = new CreateTicketDto
            {
                Subject = "Test Subject",
                Description = "Test Description",
                Category = null!,
                Priority = "Khẩn cấp",
                Scope = "Tòa nhà"
            };

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.CreateAsync(dto));
            Assert.AreEqual("Category là bắt buộc.", ex.Message);
        }



        // UTCID08: Category invalid
        // Precondition: Can connect with server
        // Input: Subject = "Test Subject" (valid), Category = "InvalidCategory" (invalid), Priority = "Khẩn cấp" (valid)
        // Expected: ArgumentException thrown with message containing "Category không hợp lệ"
        // Result Type: A (Abnormal)
        [TestMethod]
        public async Task CreateAsync_CategoryInvalid_ThrowsArgumentException()
        {
            // Arrange - Precondition: Can connect with server
            var dto = new CreateTicketDto
            {
                Subject = "Test Subject",
                Description = "Test Description",
                Category = "InvalidCategory",
                Priority = "Khẩn cấp"
            };

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.CreateAsync(dto));
            Assert.IsTrue(ex.Message.Contains("Category không hợp lệ"));
            Assert.IsTrue(ex.Message.Contains("Bảo trì, An ninh, Hóa đơn, Khiếu nại, Vệ sinh, Bãi đỗ xe, Tiện ích, Khác"));
        }



        // UTCID09: Priority null
        // Precondition: Can connect with server
        // Input: Subject = "Test Subject" (valid), Category = "An ninh" (valid), Priority = null
        // Expected: ArgumentException thrown with message containing "Priority không hợp lệ"
        // Result Type: A (Abnormal)
        [TestMethod]
        public async Task CreateAsync_PriorityNull_ThrowsArgumentException()
        {
            // Arrange - Precondition: Can connect with server
            var dto = new CreateTicketDto
            {
                Subject = "Test Subject",
                Description = "Test Description",
                Category = "An ninh",
                Priority = null!,
                Scope = "Tòa nhà"
            };

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.CreateAsync(dto));
            Assert.IsTrue(ex.Message.Contains("Priority không hợp lệ"));
            Assert.IsTrue(ex.Message.Contains("Thấp, Bình thường, Khẩn cấp"));
        }





        // UTCID10: Priority invalid
        // Precondition: Can connect with server
        // Input: Subject = "Test Subject" (valid), Category = "Tiện ích" (valid), Priority = "InvalidPriority" (invalid)
        // Expected: ArgumentException thrown with message containing "Priority không hợp lệ"
        [TestMethod]
        public async Task CreateAsync_PriorityInvalid_ThrowsArgumentException()
        {
            // Arrange - Precondition: Can connect with server
            var dto = new CreateTicketDto
            {
                Subject = "Test Subject",
                Description = "Test Description",
                Category = "Tiện ích",
                Priority = "InvalidPriority",
                Scope = "Tòa nhà"
            };

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.CreateAsync(dto));
            Assert.IsTrue(ex.Message.Contains("Priority không hợp lệ"));
            Assert.IsTrue(ex.Message.Contains("Thấp, Bình thường, Khẩn cấp"));
        }

        // UTCID11: Description null (valid case)
        // Precondition: Can connect with server
        // Input: Subject = "Test Subject" (valid), Description = null (optional), Category = "Bãi đỗ xe" (valid), Priority = "Bình thường" (valid)
        // Expected: TicketDto returned successfully, Description = null
        // Exception: None(Success)
        // Result Type: N (Normal)
        [TestMethod]
        public async Task CreateAsync_DescriptionNull_Success()
        {
            // Arrange - Precondition: Can connect with server
            var dto = new CreateTicketDto
            {
                Subject = "Test Subject",
                Description = null,
                Category = "Bãi đỗ xe",
                Priority = "Bình thường",
                Scope = "Tòa nhà"
            };

            _ticketRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Ticket>()))
                .ReturnsAsync((Ticket t) => t);

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(dto.Subject, result.Subject);
            Assert.IsNull(result.Description);
            Assert.AreEqual("Bãi đỗ xe", result.Category);
            Assert.AreEqual("Bình thường", result.Priority);
            _ticketRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Ticket>()), Times.Once);
        }

        // UTCID11: Description too long (> 4000 characters)
        // Precondition: Can connect with server
        // Input: Subject = "Test Subject" (valid), Description = 4001 characters string (invalid), Category = "An ninh" (valid), Priority = "Bình thường" (valid), Scope = "Tòa nhà" (valid)
        // Expected: ArgumentException thrown with message "Mô tả không được vượt quá 4000 ký tự."
        // Exception: ArgumentException
        // Result Type: A (Abnormal)
        // Log message: None
        [TestMethod]
        public async Task CreateAsync_DescriptionTooLong_4001Chars_ThrowsArgumentException()
        {
            // Arrange - Precondition: Can connect with server
            var dto = new CreateTicketDto
            {
                Subject = "Test Subject",
                Description = new string('A', 4001),
                Category = "An ninh",
                Priority = "Bình thường",
                Scope = "Tòa nhà"
            };

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.CreateAsync(dto));
            Assert.AreEqual("Mô tả không được vượt quá 4000 ký tự.", ex.Message);
        }

        // UTCID12: Description boundary - 4000 characters (maximum valid)
        // Precondition: Can connect with server
        // Input: Subject = "Test Subject" (valid), Description = 4000 characters string (valid), Category = "An ninh" (valid), Priority = "Bình thường" (valid), Scope = "Tòa nhà" (valid)
        // Expected: TicketDto returned successfully with Description = 4000 characters
        // Exception: None
        // Result Type: N (Normal)
        // Log message: None
        [TestMethod]
        public async Task CreateAsync_DescriptionBoundary_4000Chars_Success()
        {
            // Arrange - Precondition: Can connect with server
            var description = new string('A', 4000);
            var dto = new CreateTicketDto
            {
                Subject = "Test Subject",
                Description = description,
                Category = "An ninh",
                Priority = "Bình thường",
                Scope = "Tòa nhà"
            };

            _ticketRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Ticket>()))
                .ReturnsAsync((Ticket t) => t);

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(4000, result.Description?.Length);
            Assert.AreEqual(description, result.Description);
            _ticketRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Ticket>()), Times.Once);
        }

        // UTCID13: Valid data - Priority "Thấp"
        // Precondition: Can connect with server
        // Input: Subject = "Test Subject" (valid), Description = "Test Description" (valid), Category = "An ninh" (valid), Priority = "Thấp" (valid)
        // Expected: TicketDto returned successfully with Priority = "Thấp"
        [TestMethod]
        public async Task CreateAsync_ValidData_PriorityThap_Success()
        {
            // Arrange - Precondition: Can connect with server
            var dto = new CreateTicketDto
            {
                Subject = "Test Subject",
                Description = "Test Description",
                Category = "An ninh",
                Priority = "Thấp",
                Scope = "Tòa nhà"
            };

            _ticketRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Ticket>()))
                .ReturnsAsync((Ticket t) => t);

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Test Subject", result.Subject);
            Assert.AreEqual("Test Description", result.Description);
            Assert.AreEqual("An ninh", result.Category);
            Assert.AreEqual("Thấp", result.Priority);
            _ticketRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Ticket>()), Times.Once);
        }

        // UTCID13: Valid data - Priority "Khẩn cấp"
        [TestMethod]
        public async Task CreateAsync_ValidData_PriorityKhanCap_Success()
        {
            // Arrange
            var dto = new CreateTicketDto
            {
                Subject = "Test Subject",
                Description = "Test Description",
                Category = "Hóa đơn",
                Priority = "Khẩn cấp",
                Scope = "Tòa nhà"
            };

            _ticketRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Ticket>()))
                .ReturnsAsync((Ticket t) => t);

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Khẩn cấp", result.Priority);
            _ticketRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Ticket>()), Times.Once);
        }

        // UTCID14: Valid data - Category "Khiếu nại"
        [TestMethod]
        public async Task CreateAsync_ValidData_CategoryAnNinh_Success()
        {
            // Arrange
            var dto = new CreateTicketDto
            {
                Subject = "Test Subject",
                Description = "Test Description",
                Category = "Khiếu nại",
                Priority = "Bình thường",
                Scope = "Tòa nhà"
            };

            _ticketRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Ticket>()))
                .ReturnsAsync((Ticket t) => t);

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Khiếu nại", result.Category);
            _ticketRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Ticket>()), Times.Once);
        }



        // UTCID15: Repository throws DbUpdateException
        // Precondition: Repository throws DbUpdateException
        // Input: Subject = "Test Subject" (valid), Description = "Test Description" (valid), Category = "Tiện ích" (valid), Priority = "Bình thường" (valid)
        // Expected: DbUpdateException thrown
        // Result Type: A (Abnormal)
        [TestMethod]
        public async Task CreateAsync_RepositoryThrowsException_ThrowsException()
        {
            // Arrange - Precondition: Repository throws DbUpdateException
            var dto = new CreateTicketDto
            {
                Subject = "Test Subject",
                Description = "Test Description",
                Category = "Tiện ích",
                Priority = "Bình thường",
                Scope = "Tòa nhà"
            };

            _ticketRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Ticket>()))
                .ThrowsAsync(new DbUpdateException("Database error"));

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<DbUpdateException>(async () => await _service.CreateAsync(dto));
            Assert.IsNotNull(ex);
            Assert.IsTrue(ex.Message.Contains("Database error"));
        }

        // UTCID17A: Valid data - Complete success case
        // Precondition: Can connect with server
        // Input: Subject = "Test Ticket Subject" (valid), Description = "Test Description" (valid), Category = "Khác" (valid), Priority = "Bình thường" (valid)
        // Expected: TicketDto returned successfully with all fields matching input
        // Confirm Return: "Tạo Ticket thành công" (implicit success)
        // Exception: None(Success)
        // Result Type: N (Normal)
        [TestMethod]
        public async Task CreateAsync_ValidData_CompleteSuccess()
        {
            // Arrange - Precondition: Can connect with server
            var dto = new CreateTicketDto
            {
                Subject = "Test Ticket Subject",
                Description = "Test Description",
                Category = "Khác",
                Priority = "Bình thường",
                Scope = "Tòa nhà"
            };

            _ticketRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Ticket>()))
                .ReturnsAsync((Ticket t) => t);

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(dto.Subject, result.Subject);
            Assert.AreEqual(dto.Description, result.Description);
            Assert.AreEqual(dto.Category, result.Category);
            Assert.AreEqual(dto.Priority, result.Priority);
            Assert.IsNotNull(result.TicketId);
            Assert.AreEqual("Mới tạo", result.Status); // Default status
            _ticketRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Ticket>()), Times.Once);
        }





        #endregion

        #region GetAsync Tests

        // UTCID01: Ticket tồn tại - Success - Ticket tồn tại - Thành công
        // Precondition: Can connect with server, Ticket exists - Có thể kết nối với server, Ticket tồn tại
        // Input: TicketId = existing GUID - TicketId = GUID tồn tại
        // Expected: TicketDto returned successfully with matching TicketId - Trả về TicketDto thành công với TicketId khớp
        // Exception: None(Success) - Không có exception (Thành công)
        // Result Type: N (Normal) - Loại kết quả: Bình thường
        // Log message: None - Không có log message
        [TestMethod]
        public async Task GetAsync_TicketExists_ReturnsTicket()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var ticket = CreateTestTicket(ticketId);

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            _userRepositoryMock
                .Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new List<User>());

            // Act
            var result = await _service.GetAsync(ticketId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(ticketId, result.TicketId);
            _ticketRepositoryMock.Verify(r => r.GetByIdAsync(ticketId), Times.Once);
        }

        // UTCID02: Ticket không tồn tại - Ticket không tồn tại
        // Precondition: Can connect with server - Có thể kết nối với server
        // Input: TicketId = non-existent GUID - TicketId = GUID không tồn tại
        // Expected: Returns null - Trả về null
        // Exception: None - Không có exception
        // Result Type: N (Normal) - Loại kết quả: Bình thường
        // Log message: None - Không có log message
        [TestMethod]
        public async Task GetAsync_TicketNotFound_ReturnsNull()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync((Ticket?)null);

            // Act
            var result = await _service.GetAsync(ticketId);

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region UpdateAsync Tests


        // UTCID01: Valid data - Success
        // Precondition: Can connect with server, Ticket exists with Status = "Mới tạo"
        // Input: TicketId = existing ticket, Subject = "Updated Subject" (valid), Description = "Updated Description" (valid), Category = "An ninh" (valid), Priority = "Khẩn cấp" (valid)
        // Expected: TicketDto returned successfully with all fields updated
        // Exception: None
        // Result Type: N (Normal)
        // Log message: None
        [TestMethod]
        public async Task UpdateAsync_ValidData_Success()
        {
            // Arrange - Precondition: Can connect with server, Ticket exists with Status = "Mới tạo"
            var ticketId = Guid.NewGuid();
            var existingTicket = CreateTestTicket(ticketId, status: "Mới tạo");
            var updateDto = new UpdateTicketDto
            {
                TicketId = ticketId,
                Subject = "Updated Subject",
                Description = "Updated Description",
                Category = "An ninh",
                Priority = "Khẩn cấp"
            };

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(existingTicket);

            _ticketRepositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<Ticket>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateAsync(updateDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(updateDto.Subject, result.Subject);
            Assert.AreEqual(updateDto.Description, result.Description);
            Assert.AreEqual(updateDto.Category, result.Category);
            Assert.AreEqual(updateDto.Priority, result.Priority);
            _ticketRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Ticket>()), Times.Once);
        }

        // UTCID02: Ticket không tồn tại
        // Precondition: Can connect với server
        // Input: TicketId = non-existent GUID, Subject = "Updated Subject" (valid), Category = "Khiếu nại" (valid), Priority = "Bình thường" (valid)
        // Expected: Returns null (ticket not found)
        // Exception: None
        // Result Type: N (Normal)
        // Log message: None
        [TestMethod]
        public async Task UpdateAsync_TicketNotFound_ReturnsNull()
        {
            // Arrange - Precondition: Can connect with server
            var ticketId = Guid.NewGuid();
            var updateDto = new UpdateTicketDto
            {
                TicketId = ticketId,
                Subject = "Updated Subject",
                Category = "Khiếu nại",
                Priority = "Bình thường"
            };

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync((Ticket?)null);

            // Act
            var result = await _service.UpdateAsync(updateDto);

            // Assert
            Assert.IsNull(result);
            _ticketRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Ticket>()), Times.Never);
        }

        // UTCID03: Ticket đã đóng
        // Precondition: Can connect with server, Ticket exists with Status = "Đã đóng"
        // Input: TicketId = existing ticket (closed), Subject = "Updated Subject" (valid), Category = "Vệ sinh" (valid), Priority = "Bình thường" (valid)
        // Expected: InvalidOperationException thrown with message "Ticket đã đóng, không thể chỉnh sửa."
        // Exception: InvalidOperationException
        // Result Type: A (Abnormal)
        // Log message: None
        [TestMethod]
        public async Task UpdateAsync_TicketClosed_ThrowsInvalidOperationException()
        {
            // Arrange - Precondition: Can connect with server, Ticket exists with Status = "Đã đóng"
            var ticketId = Guid.NewGuid();
            var existingTicket = CreateTestTicket(ticketId, status: "Đã đóng");
            var updateDto = new UpdateTicketDto
            {
                TicketId = ticketId,
                Subject = "Updated Subject",
                Category = "Vệ sinh",
                Priority = "Bình thường"
            };

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(existingTicket);

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await _service.UpdateAsync(updateDto));
            Assert.AreEqual("Ticket đã đóng, không thể chỉnh sửa.", ex.Message);
        }




        // UTCID04: Cập nhật Priority + ExpectedCompletionAt (giống payload frontend)
        // Precondition: Can connect with server, ticket exists, đang mở
        // Input: Priority chuyển từ "Bình thường" -> "Khẩn cấp", ExpectedCompletionAt được truyền từ client, các trường khác giữ nguyên
        // Expected: Update thành công, Priority = "Khẩn cấp", ExpectedCompletionAt = giá trị client gửi
        // Exception: None(Success)
        // Result Type: N (Normal)
        // Log message: None

        // UTCID05: Repository throws exception
        // Precondition: Can connect with server, Ticket exists
        // Input: TicketId = existing ticket, Subject = "Updated Subject" (valid), Category = "Bãi đỗ xe" (valid), Priority = "Bình thường" (valid)
        // Expected: DbUpdateException thrown ("Database error")
        // Exception: DbUpdateException
        // Result Type: A (Abnormal)
        // Log message: None
        [TestMethod]
        public async Task UpdateAsync_RepositoryThrowsException_ThrowsException()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var existingTicket = CreateTestTicket(ticketId, status: "Mới tạo");
            var updateDto = new UpdateTicketDto
            {
                TicketId = ticketId,
                Subject = "Updated Subject",
                Category = "Bãi đỗ xe",
                Priority = "Bình thường"
            };

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(existingTicket);

            _ticketRepositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<Ticket>()))
                .ThrowsAsync(new DbUpdateException("Database error"));

            // Act & Assert
            await Assert.ThrowsExceptionAsync<DbUpdateException>(async () => await _service.UpdateAsync(updateDto));
        }

        // UTCID06: Subject null
        // Precondition: Can connect with server, Ticket exists with Status = "Mới tạo"
        // Input: TicketId = existing ticket, Subject = null, Category = "An ninh" (valid), Priority = "Bình thường" (valid)
        // Expected: ArgumentException thrown with message "Tiêu đề là bắt buộc."
        // Exception: ArgumentException
        // Result Type: A (Abnormal)
        // Log message: None
        [TestMethod]
        public async Task UpdateAsync_SubjectNull_ThrowsArgumentException()
        {
            // Arrange - Precondition: Can connect with server, Ticket exists with Status = "Mới tạo"
            var ticketId = Guid.NewGuid();
            var existingTicket = CreateTestTicket(ticketId, status: "Mới tạo");
            var updateDto = new UpdateTicketDto
            {
                TicketId = ticketId,
                Subject = null!,
                Category = "An ninh",
                Priority = "Bình thường"
            };

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(existingTicket);

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.UpdateAsync(updateDto));
            Assert.AreEqual("Tiêu đề là bắt buộc.", ex.Message);
        }













        // UTCID07: Category empty
        // Precondition: Can connect with server, Ticket exists with Status = "Mới tạo"
        // Input: TicketId = existing ticket, Subject = "Updated Subject" (valid), Category = "" (empty), Priority = "Bình thường" (valid)
        // Expected: ArgumentException thrown with message "Category là bắt buộc."
        // Exception: ArgumentException
        // Result Type: A (Abnormal)
        // Log message: None
        [TestMethod]
        public async Task UpdateAsync_CategoryEmpty_ThrowsArgumentException()
        {
            // Arrange - Precondition: Can connect with server, Ticket exists with Status = "Mới tạo"
            var ticketId = Guid.NewGuid();
            var existingTicket = CreateTestTicket(ticketId, status: "Mới tạo");
            var updateDto = new UpdateTicketDto
            {
                TicketId = ticketId,
                Subject = "Updated Subject",
                Category = "",
                Priority = "Bình thường"
            };

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(existingTicket);

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.UpdateAsync(updateDto));
            Assert.AreEqual("Category là bắt buộc.", ex.Message);
        }

        // UTCID21: Description too long (> 4000 characters)
        // Precondition: Can connect with server, Ticket exists with Status = "Mới tạo"
        // Input: TicketId = existing ticket, Subject = "Updated Subject" (valid), Description = 4001 characters string (invalid), Category = "An ninh" (valid), Priority = "Bình thường" (valid)
        // Expected: ArgumentException thrown with message "Mô tả không được vượt quá 4000 ký tự."
        // Exception: ArgumentException
        // Result Type: A (Abnormal)
        // Log message: None
        [TestMethod]
        public async Task UpdateAsync_DescriptionTooLong_4001Chars_ThrowsArgumentException()
        {
            // Arrange - Precondition: Can connect with server, Ticket exists with Status = "Mới tạo"
            var ticketId = Guid.NewGuid();
            var existingTicket = CreateTestTicket(ticketId, status: "Mới tạo");
            var updateDto = new UpdateTicketDto
            {
                TicketId = ticketId,
                Subject = "Updated Subject",
                Description = new string('A', 4001),
                Category = "An ninh",
                Priority = "Bình thường"
            };

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(existingTicket);

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.UpdateAsync(updateDto));
            Assert.AreEqual("Mô tả không được vượt quá 4000 ký tự.", ex.Message);
        }

        // UTCID22: Description boundary - 4000 characters (maximum valid)
        // Precondition: Can connect with server, Ticket exists with Status = "Mới tạo"
        // Input: TicketId = existing ticket, Subject = "Updated Subject" (valid), Description = 4000 characters string (valid), Category = "An ninh" (valid), Priority = "Bình thường" (valid)
        // Expected: TicketDto returned successfully with Description = 4000 characters
        // Exception: None
        // Result Type: N (Normal)
        // Log message: None
        [TestMethod]
        public async Task UpdateAsync_DescriptionBoundary_4000Chars_Success()
        {
            // Arrange - Precondition: Can connect with server, Ticket exists with Status = "Mới tạo"
            var ticketId = Guid.NewGuid();
            var existingTicket = CreateTestTicket(ticketId, status: "Mới tạo");
            var description = new string('A', 4000);
            var updateDto = new UpdateTicketDto
            {
                TicketId = ticketId,
                Subject = "Updated Subject",
                Description = description,
                Category = "An ninh",
                Priority = "Bình thường"
            };

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(existingTicket);

            _ticketRepositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<Ticket>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateAsync(updateDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(4000, result.Description?.Length);
            Assert.AreEqual(description, result.Description);
            _ticketRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Ticket>()), Times.Once);
        }








        #endregion

        #region DeleteAsync Tests

        // UTCID01: Ticket tồn tại - Success - Ticket tồn tại - Thành công
        // Precondition: Can connect with server, Ticket exists - Có thể kết nối với server, Ticket tồn tại
        // Input: TicketId = existing GUID - TicketId = GUID tồn tại
        // Expected: Returns true, DeleteAsync called once - Trả về true, DeleteAsync được gọi một lần
        // Exception: None(Success) - Không có exception (Thành công)
        // Result Type: N (Normal) - Loại kết quả: Bình thường
        // Log message: None - Không có log message
        [TestMethod]
        public async Task DeleteAsync_TicketExists_ReturnsTrue()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var ticket = CreateTestTicket(ticketId);

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            _ticketRepositoryMock
                .Setup(r => r.DeleteAsync(ticketId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.DeleteAsync(ticketId);

            // Assert
            Assert.IsTrue(result);
            _ticketRepositoryMock.Verify(r => r.DeleteAsync(ticketId), Times.Once);
        }

        // UTCID02: Ticket không tồn tại - Ticket không tồn tại
        // Precondition: Can connect with server - Có thể kết nối với server
        // Input: TicketId = non-existent GUID - TicketId = GUID không tồn tại
        // Expected: Returns false, DeleteAsync not called - Trả về false, DeleteAsync không được gọi
        // Exception: None - Không có exception
        // Result Type: N (Normal) - Loại kết quả: Bình thường
        // Log message: None - Không có log message
        [TestMethod]
        public async Task DeleteAsync_TicketNotFound_ReturnsFalse()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync((Ticket?)null);

            // Act
            var result = await _service.DeleteAsync(ticketId);

            // Assert
            Assert.IsFalse(result);
            _ticketRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }

        #endregion

        #region ChangeStatusAsync Tests



        // UTCID01: Ticket đã đóng
        // Precondition: Can connect with server, Ticket exists with Status = "Đã đóng"
        // Input: TicketId = existing ticket (closed), Status = "Đang xử lý" (valid)
        // Expected: InvalidOperationException thrown with message "Ticket đã đóng, không thể đổi trạng thái."
        // Exception: InvalidOperationException
        // Result Type: A (Abnormal)
        // Log message: None
        [TestMethod]
        public async Task ChangeStatusAsync_TicketClosed_ThrowsInvalidOperationException()
        {
            // Arrange - Precondition: Can connect with server, Ticket exists with Status = "Đã đóng"
            var ticketId = Guid.NewGuid();
            var existingTicket = CreateTestTicket(ticketId, status: "Đã đóng");
            var changeStatusDto = new ChangeTicketStatusDto
            {
                TicketId = ticketId,
                Status = "Đang xử lý"
            };

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(existingTicket);

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await _service.ChangeStatusAsync(changeStatusDto));
            Assert.AreEqual("Ticket đã đóng, không thể đổi trạng thái.", ex.Message);
        }

        // UTCID02: Status null
        // Precondition: Can connect with server, Ticket exists with Status = "Mới tạo"
        // Input: TicketId = existing ticket, Status = null
        // Expected: ArgumentException thrown with message "Status không hợp lệ..."
        // Exception: ArgumentException
        // Result Type: A (Abnormal)
        // Log message: None
        [TestMethod]
        public async Task ChangeStatusAsync_StatusNull_ThrowsArgumentException()
        {
            // Arrange - Precondition: Can connect with server, Ticket exists with Status = "Mới tạo"
            var ticketId = Guid.NewGuid();
            var existingTicket = CreateTestTicket(ticketId, status: "Mới tạo");
            var changeStatusDto = new ChangeTicketStatusDto
            {
                TicketId = ticketId,
                Status = null!
            };

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(existingTicket);

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.ChangeStatusAsync(changeStatusDto));
            Assert.IsTrue(ex.Message.Contains("Status không hợp lệ"));
        }

        // UTCID03: Status invalid
        // Precondition: Can connect with server, Ticket exists with Status = "Mới tạo"
        // Input: TicketId = existing ticket, Status = "Invalid Status"
        // Expected: ArgumentException thrown with message "Status không hợp lệ..."
        // Exception: ArgumentException
        // Result Type: A (Abnormal)
        // Log message: None
        [TestMethod]
        public async Task ChangeStatusAsync_StatusInvalid_ThrowsArgumentException()
        {
            // Arrange - Precondition: Can connect with server, Ticket exists with Status = "Mới tạo"
            var ticketId = Guid.NewGuid();
            var existingTicket = CreateTestTicket(ticketId, status: "Mới tạo");
            var changeStatusDto = new ChangeTicketStatusDto
            {
                TicketId = ticketId,
                Status = "Invalid Status"
            };

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(existingTicket);

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.ChangeStatusAsync(changeStatusDto));
            Assert.IsTrue(ex.Message.Contains("Status không hợp lệ"));
        }

        // UTCID04: Invalid transition - "Mới tạo" → "Đang xử lý" (phải qua "Đã tiếp nhận" trước)
        // Precondition: Can connect with server, Ticket exists with Status = "Mới tạo"
        // Input: TicketId = existing ticket, Status = "Đang xử lý"
        // Expected: InvalidOperationException thrown with message "Không thể chuyển từ \"Mới tạo\" sang \"Đang xử lý\"..."
        // Exception: InvalidOperationException
        // Result Type: A (Abnormal)
        // Log message: None
        [TestMethod]
        public async Task ChangeStatusAsync_InvalidTransition_MoiTaoToDangXuLy_ThrowsInvalidOperationException()
        {
            // Arrange - Precondition: Can connect with server, Ticket exists with Status = "Mới tạo"
            var ticketId = Guid.NewGuid();
            var existingTicket = CreateTestTicket(ticketId, status: "Mới tạo");
            var changeStatusDto = new ChangeTicketStatusDto
            {
                TicketId = ticketId,
                Status = "Đang xử lý"
            };

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(existingTicket);

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await _service.ChangeStatusAsync(changeStatusDto));
            Assert.IsTrue(ex.Message.Contains("Không thể chuyển từ \"Mới tạo\" sang \"Đang xử lý\""));
        }

        // UTCID05: Invalid transition - "Đã tiếp nhận" → "Hoàn thành" (phải qua "Đang xử lý" trước)
        // Precondition: Can connect with server, Ticket exists with Status = "Đã tiếp nhận"
        // Input: TicketId = existing ticket, Status = "Hoàn thành"
        // Expected: InvalidOperationException thrown with message "Không thể chuyển từ \"Đã tiếp nhận\" sang \"Hoàn thành\"..."
        // Exception: InvalidOperationException
        // Result Type: A (Abnormal)
        // Log message: None
        [TestMethod]
        public async Task ChangeStatusAsync_InvalidTransition_DaTiepNhanToHoanThanh_ThrowsInvalidOperationException()
        {
            // Arrange - Precondition: Can connect with server, Ticket exists with Status = "Đã tiếp nhận"
            var ticketId = Guid.NewGuid();
            var existingTicket = CreateTestTicket(ticketId, status: "Đã tiếp nhận");
            var changeStatusDto = new ChangeTicketStatusDto
            {
                TicketId = ticketId,
                Status = "Hoàn thành"
            };

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(existingTicket);

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await _service.ChangeStatusAsync(changeStatusDto));
            Assert.IsTrue(ex.Message.Contains("Không thể chuyển từ \"Đã tiếp nhận\" sang \"Hoàn thành\""));
        }

        // UTCID06: Invalid transition - "Đã tiếp nhận" → "Đã đóng" (phải qua "Đang xử lý" và "Hoàn thành" trước)
        // Precondition: Can connect with server, Ticket exists with Status = "Đã tiếp nhận"
        // Input: TicketId = existing ticket, Status = "Đã đóng"
        // Expected: InvalidOperationException thrown with message "Không thể chuyển từ \"Đã tiếp nhận\" sang \"Đã đóng\"..."
        // Exception: InvalidOperationException
        // Result Type: A (Abnormal)
        // Log message: None
        [TestMethod]
        public async Task ChangeStatusAsync_InvalidTransition_DaTiepNhanToDaDong_ThrowsInvalidOperationException()
        {
            // Arrange - Precondition: Can connect with server, Ticket exists with Status = "Đã tiếp nhận"
            var ticketId = Guid.NewGuid();
            var existingTicket = CreateTestTicket(ticketId, status: "Đã tiếp nhận");
            var changeStatusDto = new ChangeTicketStatusDto
            {
                TicketId = ticketId,
                Status = "Đã đóng"
            };

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(existingTicket);

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await _service.ChangeStatusAsync(changeStatusDto));
            Assert.IsTrue(ex.Message.Contains("Không thể chuyển từ \"Đã tiếp nhận\" sang \"Đã đóng\""));
        }

        // UTCID07: Valid transition - "Mới tạo" → "Đã tiếp nhận"
        // Precondition: Can connect with server, Ticket exists with Status = "Mới tạo"
        // Input: TicketId = existing ticket, Status = "Đã tiếp nhận", ChangedByUserId = valid GUID
        // Expected: TicketDto returned successfully with Status = "Đã tiếp nhận", system comment added
        // Exception: None
        // Result Type: N (Normal)
        // Log message: None
        [TestMethod]
        public async Task ChangeStatusAsync_ValidTransition_MoiTaoToDaTiepNhan_Success()
        {
            // Arrange - Precondition: Can connect with server, Ticket exists with Status = "Mới tạo"
            var ticketId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var existingTicket = CreateTestTicket(ticketId, status: "Mới tạo");
            var changeStatusDto = new ChangeTicketStatusDto
            {
                TicketId = ticketId,
                Status = "Đã tiếp nhận",
                ChangedByUserId = userId
            };

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(existingTicket);

            _ticketRepositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<Ticket>()))
                .Returns(Task.CompletedTask);

            _ticketRepositoryMock
                .Setup(r => r.AddCommentAsync(It.IsAny<TicketComment>()))
                .ReturnsAsync((TicketComment c) => c);

            // Act
            var result = await _service.ChangeStatusAsync(changeStatusDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Đã tiếp nhận", result.Status);
            _ticketRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Ticket>()), Times.Once);
            _ticketRepositoryMock.Verify(r => r.AddCommentAsync(It.Is<TicketComment>(c =>
                c.TicketId == ticketId &&
                c.Content.Contains("Mới tạo") &&
                c.Content.Contains("Đã tiếp nhận") &&
                c.CommentedBy == userId)), Times.Once);
        }

        // UTCID08: Valid transition - "Đã tiếp nhận" → "Đang xử lý"
        // Precondition: Can connect with server, Ticket exists with Status = "Đã tiếp nhận"
        // Input: TicketId = existing ticket, Status = "Đang xử lý", ChangedByUserId = valid GUID
        // Expected: TicketDto returned successfully with Status = "Đang xử lý", system comment added
        // Exception: None
        // Result Type: N (Normal)
        // Log message: None
        [TestMethod]
        public async Task ChangeStatusAsync_ValidTransition_DaTiepNhanToDangXuLy_Success()
        {
            // Arrange - Precondition: Can connect with server, Ticket exists with Status = "Đã tiếp nhận"
            var ticketId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var existingTicket = CreateTestTicket(ticketId, status: "Đã tiếp nhận");
            var changeStatusDto = new ChangeTicketStatusDto
            {
                TicketId = ticketId,
                Status = "Đang xử lý",
                ChangedByUserId = userId
            };

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(existingTicket);

            _ticketRepositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<Ticket>()))
                .Returns(Task.CompletedTask);

            _ticketRepositoryMock
                .Setup(r => r.AddCommentAsync(It.IsAny<TicketComment>()))
                .ReturnsAsync((TicketComment c) => c);

            // Act
            var result = await _service.ChangeStatusAsync(changeStatusDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Đang xử lý", result.Status);
            _ticketRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Ticket>()), Times.Once);
            _ticketRepositoryMock.Verify(r => r.AddCommentAsync(It.Is<TicketComment>(c =>
                c.TicketId == ticketId &&
                c.Content.Contains("Đã tiếp nhận") &&
                c.Content.Contains("Đang xử lý") &&
                c.CommentedBy == userId)), Times.Once);
        }

        // UTCID9: Valid transition - "Đang xử lý" → "Hoàn thành"
        // Precondition: Can connect with server, Ticket exists with Status = "Đang xử lý"
        // Input: TicketId = existing ticket, Status = "Hoàn thành", ChangedByUserId = valid GUID
        // Expected: TicketDto returned successfully with Status = "Hoàn thành", system comment added
        // Exception: None
        // Result Type: N (Normal)
        // Log message: None
        [TestMethod]
        public async Task ChangeStatusAsync_ValidTransition_DangXuLyToHoanThanh_Success()
        {
            // Arrange - Precondition: Can connect with server, Ticket exists with Status = "Đang xử lý"
            var ticketId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var existingTicket = CreateTestTicket(ticketId, status: "Đang xử lý");
            var changeStatusDto = new ChangeTicketStatusDto
            {
                TicketId = ticketId,
                Status = "Hoàn thành",
                ChangedByUserId = userId
            };

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(existingTicket);

            _ticketRepositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<Ticket>()))
                .Returns(Task.CompletedTask);

            _ticketRepositoryMock
                .Setup(r => r.AddCommentAsync(It.IsAny<TicketComment>()))
                .ReturnsAsync((TicketComment c) => c);

            // Act
            var result = await _service.ChangeStatusAsync(changeStatusDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Hoàn thành", result.Status);
            _ticketRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Ticket>()), Times.Once);
            _ticketRepositoryMock.Verify(r => r.AddCommentAsync(It.Is<TicketComment>(c =>
                c.TicketId == ticketId &&
                c.Content.Contains("Đang xử lý") &&
                c.Content.Contains("Hoàn thành") &&
                c.CommentedBy == userId)), Times.Once);
        }

        // UTCID10: Valid transition - "Hoàn thành" → "Đã đóng" (không có hóa đơn chưa thanh toán)
        // Precondition: Can connect with server, Ticket exists with Status = "Hoàn thành", không có hóa đơn chưa thanh toán
        // Input: TicketId = existing ticket, Status = "Đã đóng", ChangedByUserId = valid GUID
        // Expected: TicketDto returned successfully with Status = "Đã đóng", system comment added
        // Exception: None
        // Result Type: N (Normal)
        // Log message: None
        [TestMethod]
        public async Task ChangeStatusAsync_ValidTransition_HoanThanhToDaDong_NoUnpaidInvoices_Success()
        {
            // Arrange - Precondition: Can connect with server, Ticket exists with Status = "Hoàn thành", không có hóa đơn chưa thanh toán
            // Sử dụng in-memory database để test async EF Core operations
            var options = new DbContextOptionsBuilder<BuildingManagementContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var tenantAccessor = new TenantContextAccessor();
            using var db = new BuildingManagementContext(options, tenantAccessor);
            var ticketRepo = new TicketRepository(db);
            var userRepo = new Mock<IUserRepository>();
            // Setup mock để trả về empty list khi GetByIdsAsync được gọi
            userRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(new List<User>());
            // Setup thêm với List<Guid> để đảm bảo match
            userRepo.Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new List<User>());
            var fileStorageMock = new Mock<IFileStorageHelper>();
            var loggerMock = new Mock<ILogger<TicketService>>();
            var service = new TicketService(ticketRepo, userRepo.Object, fileStorageMock.Object, db, loggerMock.Object);

            var ticketId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var existingTicket = CreateTestTicket(ticketId, status: "Hoàn thành");
            db.Tickets.Add(existingTicket);
            await db.SaveChangesAsync();

            var changeStatusDto = new ChangeTicketStatusDto
            {
                TicketId = ticketId,
                Status = "Đã đóng",
                ChangedByUserId = userId
            };

            // Không có hóa đơn nào

            // Act
            var result = await service.ChangeStatusAsync(changeStatusDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Đã đóng", result.Status);
            var comments = await service.GetCommentsAsync(ticketId);
            Assert.IsTrue(comments.Any(c => c.Content.Contains("Hoàn thành") && c.Content.Contains("Đã đóng")));
        }

        // UTCID11: Invalid transition - "Hoàn thành" → "Đã đóng" (có hóa đơn chưa thanh toán)
        // Precondition: Can connect with server, Ticket exists with Status = "Hoàn thành", có hóa đơn chưa thanh toán
        // Input: TicketId = existing ticket, Status = "Đã đóng"
        // Expected: InvalidOperationException thrown with message "Không thể đóng ticket khi hóa đơn chưa được thanh toán."
        // Exception: InvalidOperationException
        // Result Type: A (Abnormal)
        // Log message: None

        // UTCID12: Valid transition - "Hoàn thành" → "Đã đóng" (có hóa đơn đã thanh toán)
        // Precondition: Can connect with server, Ticket exists with Status = "Hoàn thành", có hóa đơn đã thanh toán
        // Input: TicketId = existing ticket, Status = "Đã đóng", ChangedByUserId = valid GUID
        // Expected: TicketDto returned successfully with Status = "Đã đóng", system comment added
        // Exception: None
        // Result Type: N (Normal)
        // Log message: None
        [TestMethod]
        public async Task ChangeStatusAsync_ValidTransition_HoanThanhToDaDong_WithPaidInvoice_Success()
        {
            // Arrange - Precondition: Can connect with server, Ticket exists with Status = "Hoàn thành", có hóa đơn đã thanh toán
            // Sử dụng in-memory database để test async EF Core operations
            var options = new DbContextOptionsBuilder<BuildingManagementContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var tenantAccessor = new TenantContextAccessor();
            using var db = new BuildingManagementContext(options, tenantAccessor);
            var ticketRepo = new TicketRepository(db);
            var userRepo = new Mock<IUserRepository>();
            // Setup mock để trả về empty list khi GetByIdsAsync được gọi
            userRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(new List<User>());
            // Setup thêm với List<Guid> để đảm bảo match
            userRepo.Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new List<User>());
            var fileStorageMock = new Mock<IFileStorageHelper>();
            var loggerMock = new Mock<ILogger<TicketService>>();
            var service = new TicketService(ticketRepo, userRepo.Object, fileStorageMock.Object, db, loggerMock.Object);

            var ticketId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var existingTicket = CreateTestTicket(ticketId, status: "Hoàn thành");
            db.Tickets.Add(existingTicket);

            // Tạo hóa đơn đã thanh toán
            var paidInvoice = new Invoice
            {
                InvoiceId = Guid.NewGuid(),
                InvoiceNo = $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}",
                TicketId = ticketId,
                Status = "PAID", // Đã thanh toán
                ApartmentId = Guid.NewGuid(),
                IssueDate = DateOnly.FromDateTime(DateTime.UtcNow),
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
                SubtotalAmount = 100000,
                TaxAmount = 0,
                TotalAmount = 100000,
                CreatedAt = DateTime.UtcNow
            };
            db.Invoices.Add(paidInvoice);
            await db.SaveChangesAsync();

            var changeStatusDto = new ChangeTicketStatusDto
            {
                TicketId = ticketId,
                Status = "Đã đóng",
                ChangedByUserId = userId
            };

            // Act
            var result = await service.ChangeStatusAsync(changeStatusDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Đã đóng", result.Status);
            var comments = await service.GetCommentsAsync(ticketId);
            Assert.IsTrue(comments.Any(c => c.Content.Contains("Hoàn thành") && c.Content.Contains("Đã đóng")));
        }

        // UTCID14: Repository throws exception
        // Precondition: Can connect with server, Ticket exists
        // Input: TicketId = existing ticket, Status = "Đã tiếp nhận" (valid)
        // Expected: DbUpdateException thrown ("Database error")
        // Exception: DbUpdateException
        // Result Type: A (Abnormal)
        // Log message: None
        [TestMethod]
        public async Task ChangeStatusAsync_RepositoryThrowsException_ThrowsException()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var existingTicket = CreateTestTicket(ticketId, status: "Mới tạo");
            var changeStatusDto = new ChangeTicketStatusDto
            {
                TicketId = ticketId,
                Status = "Đã tiếp nhận"
            };

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(existingTicket);

            _ticketRepositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<Ticket>()))
                .ThrowsAsync(new DbUpdateException("Database error"));

            // Act & Assert
            await Assert.ThrowsExceptionAsync<DbUpdateException>(async () => await _service.ChangeStatusAsync(changeStatusDto));
        }

        #endregion

        #region AddCommentAsync Tests

        // UTCID01: Ticket không tồn tại
        // Precondition: Can connect with server
        // Input: TicketId = non-existent GUID, Content = "Test comment" (valid)
        // Expected: ArgumentException thrown with message "Ticket không tồn tại"
        [TestMethod]
        public async Task AddCommentAsync_TicketNotFound_ThrowsArgumentException()
        {
            // Arrange - Precondition: Can connect with server
            var ticketId = Guid.NewGuid();
            var commentDto = new CreateTicketCommentDto
            {
                TicketId = ticketId,
                Content = "Test comment"
            };

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync((Ticket?)null);

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.AddCommentAsync(commentDto));
            Assert.AreEqual("Ticket không tồn tại", ex.Message);
        }

        // UTCID02: Ticket đã đóng
        // Precondition: Can connect with server, Ticket exists with Status = "Đã đóng"
        // Input: TicketId = existing ticket (closed), Content = "Test comment" (valid)
        // Expected: InvalidOperationException thrown with message "Ticket đã đóng, không thể thêm bình luận."
        [TestMethod]
        public async Task AddCommentAsync_TicketClosed_ThrowsInvalidOperationException()
        {
            // Arrange - Precondition: Can connect with server, Ticket exists with Status = "Đã đóng"
            var ticketId = Guid.NewGuid();
            var ticket = CreateTestTicket(ticketId, status: "Đã đóng");
            var commentDto = new CreateTicketCommentDto
            {
                TicketId = ticketId,
                Content = "Test comment"
            };

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await _service.AddCommentAsync(commentDto));
            Assert.AreEqual("Ticket đã đóng, không thể thêm bình luận.", ex.Message);
        }

        // UTCID03: Content null
        // Precondition: Can connect with server, Ticket exists with Status = "Mới tạo"
        // Input: TicketId = existing ticket, Content = null
        // Expected: ArgumentException thrown with message "Content là bắt buộc."
        // Result Type: A (Abnormal)
        [TestMethod]
        public async Task AddCommentAsync_ContentNull_ThrowsArgumentException()
        {
            // Arrange - Precondition: Can connect with server, Ticket exists with Status = "Mới tạo"
            var ticketId = Guid.NewGuid();
            var ticket = CreateTestTicket(ticketId);
            var commentDto = new CreateTicketCommentDto
            {
                TicketId = ticketId,
                Content = null!
            };

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.AddCommentAsync(commentDto));
            Assert.AreEqual("Content là bắt buộc.", ex.Message);
        }



        // UTCID04: Content whitespace only
        // Precondition: Can connect with server, Ticket exists with Status = "Mới tạo"
        // Input: TicketId = existing ticket, Content = "   " (whitespace)
        // Expected: ArgumentException thrown with message "Content là bắt buộc."
        // Result Type: A (Abnormal)
        [TestMethod]
        public async Task AddCommentAsync_ContentWhitespace_ThrowsArgumentException()
        {
            // Arrange - Precondition: Can connect with server, Ticket exists with Status = "Mới tạo"
            var ticketId = Guid.NewGuid();
            var ticket = CreateTestTicket(ticketId);
            var commentDto = new CreateTicketCommentDto
            {
                TicketId = ticketId,
                Content = "   "
            };

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.AddCommentAsync(commentDto));
            Assert.AreEqual("Content là bắt buộc.", ex.Message);
        }

        // UTCID05: Valid data - Success
        // Precondition: Can connect with server, Ticket exists with Status = "Mới tạo"
        // Input: TicketId = existing ticket, Content = "Test comment" (valid)
        // Expected: TicketCommentDto returned successfully with Content = "Test comment"
        [TestMethod]
        public async Task AddCommentAsync_ValidData_Success()
        {
            // Arrange - Precondition: Can connect with server, Ticket exists with Status = "Mới tạo"
            var ticketId = Guid.NewGuid();
            var ticket = CreateTestTicket(ticketId);
            var commentDto = new CreateTicketCommentDto
            {
                TicketId = ticketId,
                Content = "Test comment"
            };

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            _ticketRepositoryMock
                .Setup(r => r.AddCommentAsync(It.IsAny<TicketComment>()))
                .ReturnsAsync((TicketComment c) => c);

            _userRepositoryMock
                .Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new List<User>());

            // Act
            var result = await _service.AddCommentAsync(commentDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(commentDto.Content, result.Content);
            Assert.AreEqual(ticketId, result.TicketId);
            _ticketRepositoryMock.Verify(r => r.AddCommentAsync(It.IsAny<TicketComment>()), Times.Once);
        }



        // UTCID06: Content too long (> 4000 characters)
        // Precondition: Can connect with server, Ticket exists with Status = "Mới tạo"
        // Input: TicketId = existing ticket, Content = 4001 characters string (invalid)
        // Expected: ArgumentException thrown with message "Nội dung bình luận không được vượt quá 4000 ký tự."
        // Exception: ArgumentException
        // Result Type: A (Abnormal)
        // Log message: None
        [TestMethod]
        public async Task AddCommentAsync_ContentTooLong_4001Chars_ThrowsArgumentException()
        {
            // Arrange - Precondition: Can connect with server, Ticket exists with Status = "Mới tạo"
            var ticketId = Guid.NewGuid();
            var ticket = CreateTestTicket(ticketId);
            var tooLongContent = new string('A', 4001);
            var commentDto = new CreateTicketCommentDto
            {
                TicketId = ticketId,
                Content = tooLongContent
            };

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.AddCommentAsync(commentDto));
            Assert.AreEqual("Nội dung bình luận không được vượt quá 4000 ký tự.", ex.Message);
            _ticketRepositoryMock.Verify(r => r.AddCommentAsync(It.IsAny<TicketComment>()), Times.Never);
        }



        // UTCID7: Repository throws exception
        [TestMethod]
        public async Task AddCommentAsync_RepositoryThrowsException_ThrowsException()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var ticket = CreateTestTicket(ticketId);
            var commentDto = new CreateTicketCommentDto
            {
                TicketId = ticketId,
                Content = "Test comment"
            };

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            _ticketRepositoryMock
                .Setup(r => r.AddCommentAsync(It.IsAny<TicketComment>()))
                .ThrowsAsync(new DbUpdateException("Database error"));

            // Act & Assert
            await Assert.ThrowsExceptionAsync<DbUpdateException>(async () => await _service.AddCommentAsync(commentDto));
        }

        #endregion

        #region AddAttachmentAsync Tests

        // UTCID01: Ticket không tồn tại
        // UTCID01: Ticket không tồn tại - Ticket không tồn tại
        // Precondition: Can connect with server - Có thể kết nối với server
        // Input: TicketId = non-existent GUID, FileId = valid GUID - TicketId = GUID không tồn tại, FileId = GUID hợp lệ
        // Expected: ArgumentException thrown with message containing "Ticket không tồn tại" - Ném ArgumentException với message chứa "Ticket không tồn tại"
        // Exception: ArgumentException - Loại exception: ArgumentException
        // Result Type: A (Abnormal) - Loại kết quả: Bất thường
        // Log message: None - Không có log message
        [TestMethod]
        public async Task AddAttachmentAsync_TicketNotFound_ThrowsArgumentException()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var attachmentDto = new CreateTicketAttachmentDto
            {
                TicketId = ticketId,
                FileId = Guid.NewGuid()
            };

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync((Ticket?)null);

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.AddAttachmentAsync(attachmentDto));
            Assert.IsTrue(ex.Message.Contains("Ticket không tồn tại"));
        }

        // UTCID02: Ticket đã đóng - Ticket đã đóng
        // Precondition: Can connect with server, Ticket exists with Status = "Đã đóng" - Có thể kết nối với server, Ticket tồn tại với Status = "Đã đóng"
        // Input: TicketId = existing ticket (closed), FileId = valid GUID - TicketId = ticket tồn tại (đã đóng), FileId = GUID hợp lệ
        // Expected: InvalidOperationException thrown with message containing "đã đóng" - Ném InvalidOperationException với message chứa "đã đóng"
        // Exception: InvalidOperationException - Loại exception: InvalidOperationException
        // Result Type: A (Abnormal) - Loại kết quả: Bất thường
        // Log message: None - Không có log message
        [TestMethod]
        public async Task AddAttachmentAsync_TicketClosed_ThrowsInvalidOperationException()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var ticket = CreateTestTicket(ticketId, status: "Đã đóng");
            var attachmentDto = new CreateTicketAttachmentDto
            {
                TicketId = ticketId,
                FileId = Guid.NewGuid()
            };

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await _service.AddAttachmentAsync(attachmentDto));
            Assert.IsTrue(ex.Message.Contains("đã đóng"));
        }

        // UTCID03: Valid data - Success - Dữ liệu hợp lệ - Thành công
        // Precondition: Can connect with server, Ticket exists with Status != "Đã đóng" - Có thể kết nối với server, Ticket tồn tại với Status != "Đã đóng"
        // Input: TicketId = existing ticket, FileId = valid GUID, Note = "Test note" - TicketId = ticket tồn tại, FileId = GUID hợp lệ, Note = "Test note"
        // Expected: TicketAttachmentDto returned successfully with FileId matching - Trả về TicketAttachmentDto thành công với FileId khớp
        // Exception: None(Success) - Không có exception (Thành công)
        // Result Type: N (Normal) - Loại kết quả: Bình thường
        // Log message: None - Không có log message
        [TestMethod]
        public async Task AddAttachmentAsync_ValidData_Success()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var fileId = Guid.NewGuid();
            var ticket = CreateTestTicket(ticketId);
            var attachmentDto = new CreateTicketAttachmentDto
            {
                TicketId = ticketId,
                FileId = fileId,
                Note = "Test note"
            };

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            var attachment = new TicketAttachment
            {
                AttachmentId = Guid.NewGuid(),
                TicketId = ticketId,
                FileId = fileId,
                Note = "Test note"
            };

            _ticketRepositoryMock
                .Setup(r => r.AddAttachmentAsync(It.IsAny<TicketAttachment>()))
                .ReturnsAsync(attachment);

            // Act
            var result = await _service.AddAttachmentAsync(attachmentDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(attachmentDto.FileId, result.FileId);
            _ticketRepositoryMock.Verify(r => r.AddAttachmentAsync(It.IsAny<TicketAttachment>()), Times.Once);
        }

        // UTCID04: Valid data - Note null - Dữ liệu hợp lệ - Note null
        // Precondition: Can connect with server, Ticket exists with Status != "Đã đóng" - Có thể kết nối với server, Ticket tồn tại với Status != "Đã đóng"
        // Input: TicketId = existing ticket, FileId = valid GUID, Note = null (optional) - TicketId = ticket tồn tại, FileId = GUID hợp lệ, Note = null (tùy chọn)
        // Expected: TicketAttachmentDto returned successfully with Note = null - Trả về TicketAttachmentDto thành công với Note = null
        // Exception: None(Success) - Không có exception (Thành công)
        // Result Type: N (Normal) - Loại kết quả: Bình thường
        // Log message: None - Không có log message
        [TestMethod]
        public async Task AddAttachmentAsync_ValidData_NoteNull_Success()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var fileId = Guid.NewGuid();
            var ticket = CreateTestTicket(ticketId);
            var attachmentDto = new CreateTicketAttachmentDto
            {
                TicketId = ticketId,
                FileId = fileId,
                Note = null
            };

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            var attachment = new TicketAttachment
            {
                AttachmentId = Guid.NewGuid(),
                TicketId = ticketId,
                FileId = fileId,
                Note = null
            };

            _ticketRepositoryMock
                .Setup(r => r.AddAttachmentAsync(It.IsAny<TicketAttachment>()))
                .ReturnsAsync(attachment);

            // Act
            var result = await _service.AddAttachmentAsync(attachmentDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(attachmentDto.FileId, result.FileId);
            _ticketRepositoryMock.Verify(r => r.AddAttachmentAsync(It.IsAny<TicketAttachment>()), Times.Once);
        }

        // UTCID05: Valid data - With UploadedBy - Dữ liệu hợp lệ - Có UploadedBy
        // Precondition: Can connect with server, Ticket exists with Status != "Đã đóng" - Có thể kết nối với server, Ticket tồn tại với Status != "Đã đóng"
        // Input: TicketId = existing ticket, FileId = valid GUID, UploadedBy = valid GUID, Note = "Test note" - TicketId = ticket tồn tại, FileId = GUID hợp lệ, UploadedBy = GUID hợp lệ, Note = "Test note"
        // Expected: TicketAttachmentDto returned successfully with UploadedBy set - Trả về TicketAttachmentDto thành công với UploadedBy được set
        // Exception: None(Success) - Không có exception (Thành công)
        // Result Type: N (Normal) - Loại kết quả: Bình thường
        // Log message: None - Không có log message
        [TestMethod]
        public async Task AddAttachmentAsync_ValidData_WithUploadedBy_Success()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var fileId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var ticket = CreateTestTicket(ticketId);
            var attachmentDto = new CreateTicketAttachmentDto
            {
                TicketId = ticketId,
                FileId = fileId,
                UploadedBy = userId,
                Note = "Test note"
            };

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            var attachment = new TicketAttachment
            {
                AttachmentId = Guid.NewGuid(),
                TicketId = ticketId,
                FileId = fileId,
                UploadedBy = userId,
                Note = "Test note"
            };

            _ticketRepositoryMock
                .Setup(r => r.AddAttachmentAsync(It.IsAny<TicketAttachment>()))
                .ReturnsAsync(attachment);

            // Act
            var result = await _service.AddAttachmentAsync(attachmentDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(attachmentDto.FileId, result.FileId);
            _ticketRepositoryMock.Verify(r => r.AddAttachmentAsync(It.IsAny<TicketAttachment>()), Times.Once);
        }

        // UTCID06: Repository throws exception - Repository ném exception
        // Precondition: Can connect with server, Ticket exists - Có thể kết nối với server, Ticket tồn tại
        // Input: TicketId = existing ticket, FileId = valid GUID - TicketId = ticket tồn tại, FileId = GUID hợp lệ
        // Expected: DbUpdateException thrown - Ném DbUpdateException
        // Exception: DbUpdateException - Loại exception: DbUpdateException
        // Result Type: A (Abnormal) - Loại kết quả: Bất thường
        // Log message: None - Không có log message
        [TestMethod]
        public async Task AddAttachmentAsync_RepositoryThrowsException_ThrowsException()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var fileId = Guid.NewGuid();
            var ticket = CreateTestTicket(ticketId);
            var attachmentDto = new CreateTicketAttachmentDto
            {
                TicketId = ticketId,
                FileId = fileId
            };

            _ticketRepositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            _ticketRepositoryMock
                .Setup(r => r.AddAttachmentAsync(It.IsAny<TicketAttachment>()))
                .ThrowsAsync(new DbUpdateException("Database error"));

            // Act & Assert
            await Assert.ThrowsExceptionAsync<DbUpdateException>(async () => await _service.AddAttachmentAsync(attachmentDto));
        }

        #endregion

        #region DeleteAttachmentAsync Tests

        // UTCID01: Attachment không tồn tại - Attachment không tồn tại
        // Precondition: Can connect with server - Có thể kết nối với server
        // Input: AttachmentId = non-existent GUID - AttachmentId = GUID không tồn tại
        // Expected: Returns false, DeleteAttachmentAsync not called - Trả về false, DeleteAttachmentAsync không được gọi
        // Exception: None - Không có exception
        // Result Type: N (Normal) - Loại kết quả: Bình thường
        // Log message: None - Không có log message
        [TestMethod]
        public async Task DeleteAttachmentAsync_AttachmentNotFound_ReturnsFalse()
        {
            // Arrange
            var attachmentId = Guid.NewGuid();
            _ticketRepositoryMock
                .Setup(r => r.GetAttachmentByIdAsync(attachmentId))
                .ReturnsAsync((TicketAttachment?)null);

            // Act
            var result = await _service.DeleteAttachmentAsync(attachmentId);

            // Assert
            Assert.IsFalse(result);
            _ticketRepositoryMock.Verify(r => r.DeleteAttachmentAsync(It.IsAny<Guid>()), Times.Never);
        }

        // UTCID02: Attachment tồn tại - Success - Attachment tồn tại - Thành công
        // Precondition: Can connect with server, Attachment exists - Có thể kết nối với server, Attachment tồn tại
        // Input: AttachmentId = existing GUID - AttachmentId = GUID tồn tại
        // Expected: Returns true, DeleteAttachmentAsync called once - Trả về true, DeleteAttachmentAsync được gọi một lần
        // Exception: None(Success) - Không có exception (Thành công)
        // Result Type: N (Normal) - Loại kết quả: Bình thường
        // Log message: None - Không có log message
        [TestMethod]
        public async Task DeleteAttachmentAsync_AttachmentExists_ReturnsTrue()
        {
            // Arrange
            var attachmentId = Guid.NewGuid();
            var attachment = new TicketAttachment
            {
                AttachmentId = attachmentId,
                TicketId = Guid.NewGuid()
            };

            _ticketRepositoryMock
                .Setup(r => r.GetAttachmentByIdAsync(attachmentId))
                .ReturnsAsync(attachment);

            _ticketRepositoryMock
                .Setup(r => r.DeleteAttachmentAsync(attachmentId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.DeleteAttachmentAsync(attachmentId);

            // Assert
            Assert.IsTrue(result);
            _ticketRepositoryMock.Verify(r => r.DeleteAttachmentAsync(attachmentId), Times.Once);
        }

        // UTCID03: Repository throws exception - Repository ném exception
        // Precondition: Can connect with server, Attachment exists - Có thể kết nối với server, Attachment tồn tại
        // Input: AttachmentId = existing GUID - AttachmentId = GUID tồn tại
        // Expected: DbUpdateException thrown - Ném DbUpdateException
        // Exception: DbUpdateException - Loại exception: DbUpdateException
        // Result Type: A (Abnormal) - Loại kết quả: Bất thường
        // Log message: None - Không có log message
        [TestMethod]
        public async Task DeleteAttachmentAsync_RepositoryThrowsException_ThrowsException()
        {
            // Arrange
            var attachmentId = Guid.NewGuid();
            var attachment = new TicketAttachment
            {
                AttachmentId = attachmentId,
                TicketId = Guid.NewGuid()
            };

            _ticketRepositoryMock
                .Setup(r => r.GetAttachmentByIdAsync(attachmentId))
                .ReturnsAsync(attachment);

            _ticketRepositoryMock
                .Setup(r => r.DeleteAttachmentAsync(attachmentId))
                .ThrowsAsync(new DbUpdateException("Database error"));

            // Act & Assert
            await Assert.ThrowsExceptionAsync<DbUpdateException>(async () => await _service.DeleteAttachmentAsync(attachmentId));
        }

        #endregion

        #region SearchAsync Tests
        // NOTE: SearchAsync tests require in-memory database or TestAsyncQueryProvider
        // because SearchAsync uses CountAsync and ToListAsync from EF Core.
        // These tests are commented out and should be implemented with in-memory database.

        /*
        // UTCID01: Search với tất cả filters rỗng - trả về tất cả tickets
        // Precondition: Can connect with server
        // Input: TicketQueryDto với tất cả fields null/empty, Page = 1, PageSize = 20
        // Expected: Returns all tickets với pagination
        // Result Type: N (Normal)
        [TestMethod]
        public async Task SearchAsync_NoFilters_ReturnsAllTickets()
        {
            // Arrange - Precondition: Can connect with server
            var queryDto = new TicketQueryDto
            {
                Page = 1,
                PageSize = 20
            };

            var tickets = new List<Ticket>
            {
                CreateTestTicket(status: "Mới tạo"),
                CreateTestTicket(status: "Đang xử lý"),
                CreateTestTicket(status: "Hoàn thành")
            };

            var mockQueryable = tickets.AsQueryable();
            _ticketRepositoryMock
                .Setup(r => r.Query())
                .Returns(mockQueryable);

            // Act
            var result = await _service.SearchAsync(queryDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.total);
            Assert.AreEqual(3, result.items.Count());
        }

        // UTCID02: Search với filter Status
        // Precondition: Can connect with server
        // Input: TicketQueryDto với Status = "Mới tạo"
        // Expected: Returns only tickets với Status = "Mới tạo"
        // Result Type: N (Normal)
        [TestMethod]
        public async Task SearchAsync_FilterByStatus_ReturnsFilteredTickets()
        {
            // Arrange - Precondition: Can connect with server
            var queryDto = new TicketQueryDto
            {
                Status = "Mới tạo",
                Page = 1,
                PageSize = 20
            };

            var tickets = new List<Ticket>
            {
                CreateTestTicket(status: "Mới tạo"),
                CreateTestTicket(status: "Mới tạo"),
                CreateTestTicket(status: "Đang xử lý")
            };

            var mockQueryable = tickets.AsQueryable();
            _ticketRepositoryMock
                .Setup(r => r.Query())
                .Returns(mockQueryable);

            // Act
            var result = await _service.SearchAsync(queryDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.total);
            Assert.IsTrue(result.items.All(t => t.Status == "Mới tạo"));
        }

        // UTCID03: Search với filter Priority
        // Precondition: Can connect with server
        // Input: TicketQueryDto với Priority = "Khẩn cấp"
        // Expected: Returns only tickets với Priority = "Khẩn cấp"
        // Result Type: N (Normal)
        [TestMethod]
        public async Task SearchAsync_FilterByPriority_ReturnsFilteredTickets()
        {
            // Arrange - Precondition: Can connect with server
            var queryDto = new TicketQueryDto
            {
                Priority = "Khẩn cấp",
                Page = 1,
                PageSize = 20
            };

            var tickets = new List<Ticket>
            {
                CreateTestTicket(priority: "Khẩn cấp"),
                CreateTestTicket(priority: "Khẩn cấp"),
                CreateTestTicket(priority: "Bình thường")
            };

            var mockQueryable = tickets.AsQueryable();
            _ticketRepositoryMock
                .Setup(r => r.Query())
                .Returns(mockQueryable);

            // Act
            var result = await _service.SearchAsync(queryDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.total);
            Assert.IsTrue(result.items.All(t => t.Priority == "Khẩn cấp"));
        }

        // UTCID04: Search với filter Category
        // Precondition: Can connect with server
        // Input: TicketQueryDto với Category = "An ninh"
        // Expected: Returns only tickets với Category = "An ninh"
        // Result Type: N (Normal)
        [TestMethod]
        public async Task SearchAsync_FilterByCategory_ReturnsFilteredTickets()
        {
            // Arrange - Precondition: Can connect with server
            var queryDto = new TicketQueryDto
            {
                Category = "An ninh",
                Page = 1,
                PageSize = 20
            };

            var tickets = new List<Ticket>
            {
                CreateTestTicket(),
                CreateTestTicket()
            };
            tickets[0].Category = "An ninh";
            tickets[1].Category = "Hóa đơn";

            var mockQueryable = tickets.AsQueryable();
            _ticketRepositoryMock
                .Setup(r => r.Query())
                .Returns(mockQueryable);

            // Act
            var result = await _service.SearchAsync(queryDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.total);
            Assert.IsTrue(result.items.All(t => t.Category == "An ninh"));
        }

        // UTCID05: Search với filter Search (Subject/Description)
        // Precondition: Can connect with server
        // Input: TicketQueryDto với Search = "Test"
        // Expected: Returns tickets có Subject hoặc Description chứa "Test"
        // Result Type: N (Normal)
        [TestMethod]
        public async Task SearchAsync_FilterBySearch_ReturnsFilteredTickets()
        {
            // Arrange - Precondition: Can connect with server
            var queryDto = new TicketQueryDto
            {
                Search = "Test",
                Page = 1,
                PageSize = 20
            };

            var tickets = new List<Ticket>
            {
                CreateTestTicket(),
                CreateTestTicket()
            };
            tickets[0].Subject = "Test Subject";
            tickets[1].Subject = "Other Subject";

            var mockQueryable = tickets.AsQueryable();
            _ticketRepositoryMock
                .Setup(r => r.Query())
                .Returns(mockQueryable);

            // Act
            var result = await _service.SearchAsync(queryDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.total);
            Assert.IsTrue(result.items.All(t => t.Subject.Contains("Test") || (t.Description ?? "").Contains("Test")));
        }

        // UTCID06: Search với filter FromDate
        // Precondition: Can connect with server
        // Input: TicketQueryDto với FromDate = specific date
        // Expected: Returns tickets có CreatedAt >= FromDate
        // Result Type: N (Normal)
        [TestMethod]
        public async Task SearchAsync_FilterByFromDate_ReturnsFilteredTickets()
        {
            // Arrange - Precondition: Can connect with server
            var fromDate = DateTime.UtcNow.AddDays(-5);
            var queryDto = new TicketQueryDto
            {
                FromDate = fromDate,
                Page = 1,
                PageSize = 20
            };

            var tickets = new List<Ticket>
            {
                CreateTestTicket(),
                CreateTestTicket()
            };
            tickets[0].CreatedAt = DateTime.UtcNow.AddDays(-3);
            tickets[1].CreatedAt = DateTime.UtcNow.AddDays(-10);

            var mockQueryable = tickets.AsQueryable();
            _ticketRepositoryMock
                .Setup(r => r.Query())
                .Returns(mockQueryable);

            // Act
            var result = await _service.SearchAsync(queryDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.total);
            Assert.IsTrue(result.items.All(t => t.CreatedAt >= fromDate));
        }

        // UTCID07: Search với filter ToDate
        // Precondition: Can connect with server
        // Input: TicketQueryDto với ToDate = specific date
        // Expected: Returns tickets có CreatedAt <= ToDate
        // Result Type: N (Normal)
        [TestMethod]
        public async Task SearchAsync_FilterByToDate_ReturnsFilteredTickets()
        {
            // Arrange - Precondition: Can connect with server
            var toDate = DateTime.UtcNow.AddDays(-5);
            var queryDto = new TicketQueryDto
            {
                ToDate = toDate,
                Page = 1,
                PageSize = 20
            };

            var tickets = new List<Ticket>
            {
                CreateTestTicket(),
                CreateTestTicket()
            };
            tickets[0].CreatedAt = DateTime.UtcNow.AddDays(-3);
            tickets[1].CreatedAt = DateTime.UtcNow.AddDays(-10);

            var mockQueryable = tickets.AsQueryable();
            _ticketRepositoryMock
                .Setup(r => r.Query())
                .Returns(mockQueryable);

            // Act
            var result = await _service.SearchAsync(queryDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.total);
            Assert.IsTrue(result.items.All(t => t.CreatedAt <= toDate));
        }

        // UTCID08: Search với filter CreatedByUserId
        // Precondition: Can connect with server
        // Input: TicketQueryDto với CreatedByUserId = specific GUID
        // Expected: Returns tickets có CreatedByUserId = specified GUID
        // Result Type: N (Normal)
        [TestMethod]
        public async Task SearchAsync_FilterByCreatedByUserId_ReturnsFilteredTickets()
        {
            // Arrange - Precondition: Can connect with server
            var userId = Guid.NewGuid();
            var queryDto = new TicketQueryDto
            {
                CreatedByUserId = userId,
                Page = 1,
                PageSize = 20
            };

            var tickets = new List<Ticket>
            {
                CreateTestTicket(),
                CreateTestTicket()
            };
            tickets[0].CreatedByUserId = userId;
            tickets[1].CreatedByUserId = Guid.NewGuid();

            var mockQueryable = tickets.AsQueryable();
            _ticketRepositoryMock
                .Setup(r => r.Query())
                .Returns(mockQueryable);

            // Act
            var result = await _service.SearchAsync(queryDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.total);
            Assert.IsTrue(result.items.All(t => t.CreatedByUserId == userId));
        }

        // UTCID09: Search với pagination - Page 1, PageSize 2
        // Precondition: Can connect with server
        // Input: TicketQueryDto với Page = 1, PageSize = 2
        // Expected: Returns 2 tickets (first page)
        // Result Type: N (Normal)
        [TestMethod]
        public async Task SearchAsync_WithPagination_ReturnsPagedResults()
        {
            // Arrange - Precondition: Can connect with server
            var queryDto = new TicketQueryDto
            {
                Page = 1,
                PageSize = 2
            };

            var tickets = new List<Ticket>
            {
                CreateTestTicket(),
                CreateTestTicket(),
                CreateTestTicket()
            };

            var mockQueryable = tickets.AsQueryable();
            _ticketRepositoryMock
                .Setup(r => r.Query())
                .Returns(mockQueryable);

            // Act
            var result = await _service.SearchAsync(queryDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.total);
            Assert.AreEqual(2, result.items.Count());
        }

        // UTCID10: Search với pagination - Page 2, PageSize 2
        // Precondition: Can connect with server
        // Input: TicketQueryDto với Page = 2, PageSize = 2
        // Expected: Returns 1 ticket (second page)
        // Result Type: N (Normal)
        [TestMethod]
        public async Task SearchAsync_WithPagination_Page2_ReturnsSecondPage()
        {
            // Arrange - Precondition: Can connect with server
            var queryDto = new TicketQueryDto
            {
                Page = 2,
                PageSize = 2
            };

            var tickets = new List<Ticket>
            {
                CreateTestTicket(),
                CreateTestTicket(),
                CreateTestTicket()
            };

            var mockQueryable = tickets.AsQueryable();
            _ticketRepositoryMock
                .Setup(r => r.Query())
                .Returns(mockQueryable);

            // Act
            var result = await _service.SearchAsync(queryDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.total);
            Assert.AreEqual(1, result.items.Count());
        }

        // UTCID11: Search với tất cả filters kết hợp
        // Precondition: Can connect with server
        // Input: TicketQueryDto với tất cả filters có giá trị
        // Expected: Returns tickets thỏa mãn tất cả điều kiện
        // Result Type: N (Normal)
        [TestMethod]
        public async Task SearchAsync_WithAllFilters_ReturnsFilteredTickets()
        {
            // Arrange - Precondition: Can connect with server
            var userId = Guid.NewGuid();
            var fromDate = DateTime.UtcNow.AddDays(-10);
            var toDate = DateTime.UtcNow;
            var queryDto = new TicketQueryDto
            {
                Status = "Mới tạo",
                Priority = "Khẩn cấp",
                Category = "An ninh",
                Search = "Test",
                FromDate = fromDate,
                ToDate = toDate,
                CreatedByUserId = userId,
                Page = 1,
                PageSize = 20
            };

            var tickets = new List<Ticket>
            {
                CreateTestTicket(status: "Mới tạo", priority: "Khẩn cấp")
            };
            tickets[0].Category = "An ninh";
            tickets[0].Subject = "Test Subject";
            tickets[0].CreatedByUserId = userId;
            tickets[0].CreatedAt = DateTime.UtcNow.AddDays(-5);

            var mockQueryable = tickets.AsQueryable();
            _ticketRepositoryMock
                .Setup(r => r.Query())
                .Returns(mockQueryable);

            // Act
            var result = await _service.SearchAsync(queryDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.total);
        }

        // UTCID12: Search với empty result
        // Precondition: Can connect with server
        // Input: TicketQueryDto với filter không match bất kỳ ticket nào
        // Expected: Returns empty list với total = 0
        // Result Type: N (Normal)
        [TestMethod]
        public async Task SearchAsync_NoMatchingTickets_ReturnsEmpty()
        {
            // Arrange - Precondition: Can connect with server
            var queryDto = new TicketQueryDto
            {
                Status = "Không tồn tại",
                Page = 1,
                PageSize = 20
            };

            var tickets = new List<Ticket>
            {
                CreateTestTicket(status: "Mới tạo")
            };

            var mockQueryable = tickets.AsQueryable();
            _ticketRepositoryMock
                .Setup(r => r.Query())
                .Returns(mockQueryable);

            // Act
            var result = await _service.SearchAsync(queryDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.total);
            Assert.AreEqual(0, result.items.Count());
        }

        */
        #endregion

        #region GetCommentsAsync Tests

        // UTCID01: Ticket có comments - Success
        // Precondition: Can connect with server, Ticket exists với comments
        // Input: TicketId = existing ticket với comments
        // Expected: Returns list of comments ordered by CommentTime, với user names populated
        // Result Type: N (Normal)
        [TestMethod]
        public async Task GetCommentsAsync_TicketWithComments_ReturnsComments()
        {
            // Arrange - Precondition: Can connect with server, Ticket exists với comments
            // Sử dụng in-memory database để test async EF Core operations
            var options = new DbContextOptionsBuilder<BuildingManagementContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var tenantAccessor = new TenantContextAccessor();
            using var db = new BuildingManagementContext(options, tenantAccessor);
            var ticketRepo = new TicketRepository(db);
            var userRepo = new Mock<IUserRepository>();
            var fileStorageMock = new Mock<IFileStorageHelper>();
            var loggerMock = new Mock<ILogger<TicketService>>();
            var service = new TicketService(ticketRepo, userRepo.Object, fileStorageMock.Object, db, loggerMock.Object);

            var ticketId = Guid.NewGuid();
            var userId1 = Guid.NewGuid();
            var userId2 = Guid.NewGuid();

            // Tạo ticket
            var ticket = new Ticket
            {
                TicketId = ticketId,
                Subject = "Test Ticket",
                Category = "An ninh",
                Priority = "Bình thường",
                Status = "Mới tạo",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = Guid.NewGuid()
            };
            db.Tickets.Add(ticket);

            // Tạo comments
            var comment1 = new TicketComment
            {
                CommentId = Guid.NewGuid(),
                TicketId = ticketId,
                CommentedBy = userId1,
                Content = "Comment 1",
                CommentTime = DateTime.UtcNow.AddHours(-2)
            };
            var comment2 = new TicketComment
            {
                CommentId = Guid.NewGuid(),
                TicketId = ticketId,
                CommentedBy = userId2,
                Content = "Comment 2",
                CommentTime = DateTime.UtcNow.AddHours(-1)
            };
            db.TicketComments.AddRange(comment1, comment2);
            await db.SaveChangesAsync();

            var users = new List<User>
            {
                new User { UserId = userId1, Username = "user1" },
                new User { UserId = userId2, Email = "user2@test.com" }
            };

            userRepo
                .Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(users);

            // Act
            var result = await service.GetCommentsAsync(ticketId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
            var resultList = result.ToList();
            Assert.AreEqual("Comment 1", resultList[0].Content);
            Assert.AreEqual("Comment 2", resultList[1].Content);
            Assert.IsTrue(resultList[0].CommentTime < resultList[1].CommentTime); // Ordered by CommentTime
            Assert.AreEqual("user1", resultList[0].CreatedByUserName);
            Assert.AreEqual("user2@test.com", resultList[1].CreatedByUserName);
        }

        // UTCID02: Ticket không có comments - Success
        // Precondition: Can connect with server, Ticket exists nhưng không có comments
        // Input: TicketId = existing ticket không có comments
        // Expected: Returns empty list
        // Result Type: N (Normal)
        [TestMethod]
        public async Task GetCommentsAsync_TicketWithoutComments_ReturnsEmpty()
        {
            // Arrange - Precondition: Can connect with server, Ticket exists nhưng không có comments
            var options = new DbContextOptionsBuilder<BuildingManagementContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var tenantAccessor = new TenantContextAccessor();
            using var db = new BuildingManagementContext(options, tenantAccessor);
            var ticketRepo = new TicketRepository(db);
            var userRepo = new Mock<IUserRepository>();
            // Setup mock để trả về empty list khi GetByIdsAsync được gọi
            userRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(new List<User>());
            // Setup thêm với List<Guid> để đảm bảo match
            userRepo.Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(new List<User>());
            var fileStorageMock = new Mock<IFileStorageHelper>();
            var loggerMock = new Mock<ILogger<TicketService>>();
            var service = new TicketService(ticketRepo, userRepo.Object, fileStorageMock.Object, db, loggerMock.Object);

            var ticketId = Guid.NewGuid();
            var ticket = new Ticket
            {
                TicketId = ticketId,
                Subject = "Test Ticket",
                Category = "An ninh",
                Priority = "Bình thường",
                Status = "Mới tạo",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = Guid.NewGuid()
            };
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();

            // Act
            var result = await service.GetCommentsAsync(ticketId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        // UTCID03: GetCommentsAsync với user không có Username - sử dụng Email
        // Precondition: Can connect with server
        // Input: Comment với user không có Username nhưng có Email
        // Expected: CreatedByUserName = Email
        // Result Type: N (Normal)
        [TestMethod]
        public async Task GetCommentsAsync_UserWithoutUsername_UsesEmail()
        {
            // Arrange - Precondition: Can connect with server
            var options = new DbContextOptionsBuilder<BuildingManagementContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var tenantAccessor = new TenantContextAccessor();
            using var db = new BuildingManagementContext(options, tenantAccessor);
            var ticketRepo = new TicketRepository(db);
            var userRepo = new Mock<IUserRepository>();
            var fileStorageMock = new Mock<IFileStorageHelper>();
            var loggerMock = new Mock<ILogger<TicketService>>();
            var service = new TicketService(ticketRepo, userRepo.Object, fileStorageMock.Object, db, loggerMock.Object);

            var ticketId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var ticket = new Ticket
            {
                TicketId = ticketId,
                Subject = "Test Ticket",
                Category = "An ninh",
                Priority = "Bình thường",
                Status = "Mới tạo",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = Guid.NewGuid()
            };
            db.Tickets.Add(ticket);

            var comment = new TicketComment
            {
                CommentId = Guid.NewGuid(),
                TicketId = ticketId,
                CommentedBy = userId,
                Content = "Comment",
                CommentTime = DateTime.UtcNow
            };
            db.TicketComments.Add(comment);
            await db.SaveChangesAsync();

            var users = new List<User>
            {
                new User { UserId = userId, Email = "user@test.com" } // No Username
            };

            userRepo
                .Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(users);

            // Act
            var result = await service.GetCommentsAsync(ticketId);

            // Assert
            Assert.IsNotNull(result);
            var commentResult = result.First();
            Assert.AreEqual("user@test.com", commentResult.CreatedByUserName);
        }

        // UTCID04: GetCommentsAsync với user không có Username và Email - sử dụng UserId
        // Precondition: Can connect with server
        // Input: Comment với user không có Username và Email
        // Expected: CreatedByUserName = UserId.ToString()
        // Result Type: N (Normal)
        [TestMethod]
        public async Task GetCommentsAsync_UserWithoutUsernameAndEmail_UsesUserId()
        {
            // Arrange - Precondition: Can connect with server
            var options = new DbContextOptionsBuilder<BuildingManagementContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var tenantAccessor = new TenantContextAccessor();
            using var db = new BuildingManagementContext(options, tenantAccessor);
            var ticketRepo = new TicketRepository(db);
            var userRepo = new Mock<IUserRepository>();
            var fileStorageMock = new Mock<IFileStorageHelper>();
            var loggerMock = new Mock<ILogger<TicketService>>();
            var service = new TicketService(ticketRepo, userRepo.Object, fileStorageMock.Object, db, loggerMock.Object);

            var ticketId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var ticket = new Ticket
            {
                TicketId = ticketId,
                Subject = "Test Ticket",
                Category = "An ninh",
                Priority = "Bình thường",
                Status = "Mới tạo",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = Guid.NewGuid()
            };
            db.Tickets.Add(ticket);

            var comment = new TicketComment
            {
                CommentId = Guid.NewGuid(),
                TicketId = ticketId,
                CommentedBy = userId,
                Content = "Comment",
                CommentTime = DateTime.UtcNow
            };
            db.TicketComments.Add(comment);
            await db.SaveChangesAsync();

            var users = new List<User>
            {
                new User { UserId = userId } // No Username and Email
            };

            userRepo
                .Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(users);

            // Act
            var result = await service.GetCommentsAsync(ticketId);

            // Assert
            Assert.IsNotNull(result);
            var commentResult = result.First();
            Assert.AreEqual(userId.ToString(), commentResult.CreatedByUserName);
        }

        // UTCID05: GetCommentsAsync với CommentedBy null - CreatedByUserName = "Unknown"
        // Precondition: Can connect with server
        // Input: Comment với CommentedBy = null
        // Expected: CreatedByUserName = "Unknown"
        // Result Type: N (Normal)
        [TestMethod]
        public async Task GetCommentsAsync_CommentWithoutCommentedBy_ReturnsUnknown()
        {
            // Arrange - Precondition: Can connect with server
            var options = new DbContextOptionsBuilder<BuildingManagementContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var tenantAccessor = new TenantContextAccessor();
            using var db = new BuildingManagementContext(options, tenantAccessor);
            var ticketRepo = new TicketRepository(db);
            var userRepo = new Mock<IUserRepository>();
            var fileStorageMock = new Mock<IFileStorageHelper>();
            var loggerMock = new Mock<ILogger<TicketService>>();
            var service = new TicketService(ticketRepo, userRepo.Object, fileStorageMock.Object, db, loggerMock.Object);

            var ticketId = Guid.NewGuid();

            var ticket = new Ticket
            {
                TicketId = ticketId,
                Subject = "Test Ticket",
                Category = "An ninh",
                Priority = "Bình thường",
                Status = "Mới tạo",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = Guid.NewGuid()
            };
            db.Tickets.Add(ticket);

            var comment = new TicketComment
            {
                CommentId = Guid.NewGuid(),
                TicketId = ticketId,
                CommentedBy = null,
                Content = "Comment",
                CommentTime = DateTime.UtcNow
            };
            db.TicketComments.Add(comment);
            await db.SaveChangesAsync();

            // Act
            var result = await service.GetCommentsAsync(ticketId);

            // Assert
            Assert.IsNotNull(result);
            var commentResult = result.First();
            Assert.AreEqual("Unknown", commentResult.CreatedByUserName);
        }

        #endregion

        #region GetAttachmentsAsync Tests

        // UTCID01: Ticket có attachments - Success
        // Precondition: Can connect with server, Ticket exists với attachments
        // Input: TicketId = existing ticket với attachments
        // Expected: Returns list of attachments
        // Result Type: N (Normal)
        [TestMethod]
        public async Task GetAttachmentsAsync_TicketWithAttachments_ReturnsAttachments()
        {
            // Arrange - Precondition: Can connect with server, Ticket exists với attachments
            var ticketId = Guid.NewGuid();
            var attachments = new List<TicketAttachment>
            {
                new TicketAttachment
                {
                    AttachmentId = Guid.NewGuid(),
                    TicketId = ticketId,
                    FileId = Guid.NewGuid(),
                    UploadedBy = Guid.NewGuid(),
                    Note = "Note 1",
                    UploadedAt = DateTime.UtcNow
                },
                new TicketAttachment
                {
                    AttachmentId = Guid.NewGuid(),
                    TicketId = ticketId,
                    FileId = Guid.NewGuid(),
                    UploadedBy = Guid.NewGuid(),
                    Note = "Note 2",
                    UploadedAt = DateTime.UtcNow
                }
            };

            _ticketRepositoryMock
                .Setup(r => r.GetAttachmentsByTicketIdAsync(ticketId))
                .ReturnsAsync(attachments);

            // Act
            var result = await _service.GetAttachmentsAsync(ticketId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
        }

        // UTCID02: Ticket không có attachments - Success
        // Precondition: Can connect with server, Ticket exists nhưng không có attachments
        // Input: TicketId = existing ticket không có attachments
        // Expected: Returns empty list
        // Result Type: N (Normal)
        [TestMethod]
        public async Task GetAttachmentsAsync_TicketWithoutAttachments_ReturnsEmpty()
        {
            // Arrange - Precondition: Can connect with server, Ticket exists nhưng không có attachments
            var ticketId = Guid.NewGuid();
            var attachments = new List<TicketAttachment>();

            _ticketRepositoryMock
                .Setup(r => r.GetAttachmentsByTicketIdAsync(ticketId))
                .ReturnsAsync(attachments);

            // Act
            var result = await _service.GetAttachmentsAsync(ticketId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        #endregion

        #region UploadFileAsync Tests

        // UTCID01: Upload file hợp lệ - Success
        // Precondition: Can connect with server
        // Input: IFormFile hợp lệ, subFolder = "tickets", uploadedBy = "user1"
        // Expected: FileDto returned successfully
        // Result Type: N (Normal)
        [TestMethod]
        public async Task UploadFileAsync_ValidFile_Success()
        {
            // Arrange - Precondition: Can connect with server
            var file = CreateTestFormFile("test.pdf", "application/pdf");
            var subFolder = "tickets";
            var uploadedBy = "user1";
            var fileId = Guid.NewGuid();

            var savedFile = new FileEntity
            {
                FileId = fileId,
                OriginalName = "test.pdf",
                StoragePath = "/tickets/test.pdf",
                MimeType = "application/pdf",
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.UtcNow
            };

            _fileStorageHelperMock
                .Setup(f => f.SaveAsync(file, subFolder!, uploadedBy))
                .ReturnsAsync(savedFile);

            _ticketRepositoryMock
                .Setup(r => r.AddFileAsync(It.IsAny<FileEntity>()))
                .ReturnsAsync(savedFile);

            // Act
            var result = await _service.UploadFileAsync(file, subFolder!, uploadedBy);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(fileId, result.FileId);
            Assert.AreEqual("test.pdf", result.OriginalName);
            _fileStorageHelperMock.Verify(f => f.SaveAsync(file, subFolder, uploadedBy), Times.Once);
            _ticketRepositoryMock.Verify(r => r.AddFileAsync(It.IsAny<FileEntity>()), Times.Once);
        }

        // UTCID02: Upload file với subFolder null - Success
        // Precondition: Can connect with server
        // Input: IFormFile hợp lệ, subFolder = null, uploadedBy = "user1"
        // Expected: FileDto returned successfully
        // Result Type: N (Normal)
        [TestMethod]
        public async Task UploadFileAsync_SubFolderNull_Success()
        {
            // Arrange - Precondition: Can connect with server
            var file = CreateTestFormFile("test.pdf", "application/pdf");
            string? subFolder = null;
            var uploadedBy = "user1";
            var fileId = Guid.NewGuid();

            var savedFile = new FileEntity
            {
                FileId = fileId,
                OriginalName = "test.pdf",
                StoragePath = "/test.pdf",
                MimeType = "application/pdf",
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.UtcNow
            };

            _fileStorageHelperMock
                .Setup(f => f.SaveAsync(file, subFolder!, uploadedBy))
                .ReturnsAsync(savedFile);

            _ticketRepositoryMock
                .Setup(r => r.AddFileAsync(It.IsAny<FileEntity>()))
                .ReturnsAsync(savedFile);

            // Act
            var result = await _service.UploadFileAsync(file, subFolder!, uploadedBy);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(fileId, result.FileId);
        }

        // UTCID03: Upload file với uploadedBy null - Success
        // Precondition: Can connect with server
        // Input: IFormFile hợp lệ, subFolder = "tickets", uploadedBy = null
        // Expected: FileDto returned successfully
        // Result Type: N (Normal)
        [TestMethod]
        public async Task UploadFileAsync_UploadedByNull_Success()
        {
            // Arrange - Precondition: Can connect with server
            var file = CreateTestFormFile("test.pdf", "application/pdf");
            var subFolder = "tickets";
            string? uploadedBy = null;
            var fileId = Guid.NewGuid();

            var savedFile = new FileEntity
            {
                FileId = fileId,
                OriginalName = "test.pdf",
                StoragePath = "/tickets/test.pdf",
                MimeType = "application/pdf",
                UploadedBy = null,
                UploadedAt = DateTime.UtcNow
            };

            _fileStorageHelperMock
                .Setup(f => f.SaveAsync(file, subFolder!, uploadedBy))
                .ReturnsAsync(savedFile);

            _ticketRepositoryMock
                .Setup(r => r.AddFileAsync(It.IsAny<FileEntity>()))
                .ReturnsAsync(savedFile);

            // Act
            var result = await _service.UploadFileAsync(file, subFolder!, uploadedBy);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(fileId, result.FileId);
        }

        // UTCID04: FileStorageHelper throws ArgumentException (validation error)
        // Precondition: FileStorageHelper throws ArgumentException due to validation
        // Input: IFormFile không hợp lệ (file quá lớn), subFolder = "tickets", uploadedBy = "user1"
        // Expected: ArgumentException thrown
        // Exception: ArgumentException
        // Result Type: A (Abnormal)
        // Log message: None
        [TestMethod]
        public async Task UploadFileAsync_FileTooLarge_ThrowsArgumentException()
        {
            // Arrange - Precondition: FileStorageHelper throws ArgumentException due to validation
            var file = CreateTestFormFile("test.pdf", "application/pdf", fileSizeBytes: 11 * 1024 * 1024); // 11MB > 10MB limit
            var subFolder = "tickets";
            var uploadedBy = "user1";

            _fileStorageHelperMock
                .Setup(f => f.SaveAsync(file, subFolder, uploadedBy))
                .ThrowsAsync(new ArgumentException("File size (11.00MB) exceeds maximum allowed size (10MB)."));

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.UploadFileAsync(file, subFolder, uploadedBy));
            Assert.IsTrue(ex.Message.Contains("exceeds maximum allowed size"));
        }

        // UTCID05: FileStorageHelper throws ArgumentException (dangerous extension)
        // Precondition: FileStorageHelper throws ArgumentException due to dangerous file extension
        // Input: IFormFile với extension nguy hiểm (.exe), subFolder = "tickets", uploadedBy = "user1"
        // Expected: ArgumentException thrown
        // Exception: ArgumentException
        // Result Type: A (Abnormal)
        // Log message: None
        [TestMethod]
        public async Task UploadFileAsync_DangerousExtension_ThrowsArgumentException()
        {
            // Arrange - Precondition: FileStorageHelper throws ArgumentException due to dangerous file extension
            var file = CreateTestFormFile("malware.exe", "application/x-msdownload");
            var subFolder = "tickets";
            var uploadedBy = "user1";

            _fileStorageHelperMock
                .Setup(f => f.SaveAsync(file, subFolder, uploadedBy))
                .ThrowsAsync(new ArgumentException("File type '.exe' is not allowed for security reasons."));

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.UploadFileAsync(file, subFolder, uploadedBy));
            Assert.IsTrue(ex.Message.Contains("not allowed for security reasons"));
        }

        // UTCID06: FileStorageHelper throws ArgumentException (invalid extension)
        // Precondition: FileStorageHelper throws ArgumentException due to invalid file extension
        // Input: IFormFile với extension không được phép (.zip), subFolder = "tickets", uploadedBy = "user1"
        // Expected: ArgumentException thrown
        // Exception: ArgumentException
        // Result Type: A (Abnormal)
        // Log message: None
        [TestMethod]
        public async Task UploadFileAsync_InvalidExtension_ThrowsArgumentException()
        {
            // Arrange - Precondition: FileStorageHelper throws ArgumentException due to invalid file extension
            var file = CreateTestFormFile("archive.zip", "application/zip");
            var subFolder = "tickets";
            var uploadedBy = "user1";

            _fileStorageHelperMock
                .Setup(f => f.SaveAsync(file, subFolder, uploadedBy))
                .ThrowsAsync(new ArgumentException("File type '.zip' is not allowed. Allowed types: .jpg, .jpeg, .png, .gif, .bmp, .webp, .pdf, .doc, .docx"));

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.UploadFileAsync(file, subFolder, uploadedBy));
            Assert.IsTrue(ex.Message.Contains("is not allowed"));
        }

        // UTCID07: FileStorageHelper throws ArgumentException (no extension)
        // Precondition: FileStorageHelper throws ArgumentException due to missing file extension
        // Input: IFormFile không có extension, subFolder = "tickets", uploadedBy = "user1"
        // Expected: ArgumentException thrown
        // Exception: ArgumentException
        // Result Type: A (Abnormal)
        // Log message: None
        [TestMethod]
        public async Task UploadFileAsync_NoExtension_ThrowsArgumentException()
        {
            // Arrange - Precondition: FileStorageHelper throws ArgumentException due to missing file extension
            var file = CreateTestFormFile("testfile", "application/octet-stream");
            var subFolder = "tickets";
            var uploadedBy = "user1";

            _fileStorageHelperMock
                .Setup(f => f.SaveAsync(file, subFolder, uploadedBy))
                .ThrowsAsync(new ArgumentException("File must have an extension."));

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.UploadFileAsync(file, subFolder, uploadedBy));
            Assert.IsTrue(ex.Message.Contains("must have an extension"));
        }

        // UTCID08: FileStorageHelper throws ArgumentException (invalid MIME type)
        // Precondition: FileStorageHelper throws ArgumentException due to invalid MIME type
        // Input: IFormFile với MIME type không hợp lệ, subFolder = "tickets", uploadedBy = "user1"
        // Expected: ArgumentException thrown
        // Exception: ArgumentException
        // Result Type: A (Abnormal)
        // Log message: None
        [TestMethod]
        public async Task UploadFileAsync_InvalidMimeType_ThrowsArgumentException()
        {
            // Arrange - Precondition: FileStorageHelper throws ArgumentException due to invalid MIME type
            var file = CreateTestFormFile("test.pdf", "application/javascript"); // Invalid MIME for PDF
            var subFolder = "tickets";
            var uploadedBy = "user1";

            _fileStorageHelperMock
                .Setup(f => f.SaveAsync(file, subFolder, uploadedBy))
                .ThrowsAsync(new ArgumentException("File MIME type 'application/javascript' is not allowed."));

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.UploadFileAsync(file, subFolder, uploadedBy));
            Assert.IsTrue(ex.Message.Contains("MIME type") && ex.Message.Contains("is not allowed"));
        }

        // UTCID09: Upload file hợp lệ - Image (JPG) - Success
        // Precondition: Can connect with server
        // Input: IFormFile hợp lệ (JPG), subFolder = "tickets", uploadedBy = "user1"
        // Expected: FileDto returned successfully
        // Result Type: N (Normal)
        // Log message: None
        [TestMethod]
        public async Task UploadFileAsync_ValidImageJpg_Success()
        {
            // Arrange - Precondition: Can connect with server
            var file = CreateTestFormFile("test.jpg", "image/jpeg");
            var subFolder = "tickets";
            var uploadedBy = "user1";
            var fileId = Guid.NewGuid();

            var savedFile = new FileEntity
            {
                FileId = fileId,
                OriginalName = "test.jpg",
                StoragePath = "/tickets/test.jpg",
                MimeType = "image/jpeg",
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.UtcNow
            };

            _fileStorageHelperMock
                .Setup(f => f.SaveAsync(file, subFolder!, uploadedBy))
                .ReturnsAsync(savedFile);

            _ticketRepositoryMock
                .Setup(r => r.AddFileAsync(It.IsAny<FileEntity>()))
                .ReturnsAsync(savedFile);

            // Act
            var result = await _service.UploadFileAsync(file, subFolder!, uploadedBy);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(fileId, result.FileId);
            Assert.AreEqual("test.jpg", result.OriginalName);
        }

        // UTCID10: Upload file hợp lệ - Image (PNG) - Success
        // Precondition: Can connect with server
        // Input: IFormFile hợp lệ (PNG), subFolder = "tickets", uploadedBy = "user1"
        // Expected: FileDto returned successfully
        // Result Type: N (Normal)
        // Log message: None
        [TestMethod]
        public async Task UploadFileAsync_ValidImagePng_Success()
        {
            // Arrange - Precondition: Can connect with server
            var file = CreateTestFormFile("test.png", "image/png");
            var subFolder = "tickets";
            var uploadedBy = "user1";
            var fileId = Guid.NewGuid();

            var savedFile = new FileEntity
            {
                FileId = fileId,
                OriginalName = "test.png",
                StoragePath = "/tickets/test.png",
                MimeType = "image/png",
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.UtcNow
            };

            _fileStorageHelperMock
                .Setup(f => f.SaveAsync(file, subFolder!, uploadedBy))
                .ReturnsAsync(savedFile);

            _ticketRepositoryMock
                .Setup(r => r.AddFileAsync(It.IsAny<FileEntity>()))
                .ReturnsAsync(savedFile);

            // Act
            var result = await _service.UploadFileAsync(file, subFolder!, uploadedBy);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(fileId, result.FileId);
            Assert.AreEqual("test.png", result.OriginalName);
        }

        // UTCID11: Upload file hợp lệ - Document (DOCX) - Success
        // Precondition: Can connect with server
        // Input: IFormFile hợp lệ (DOCX), subFolder = "tickets", uploadedBy = "user1"
        // Expected: FileDto returned successfully
        // Result Type: N (Normal)
        // Log message: None
        [TestMethod]
        public async Task UploadFileAsync_ValidDocumentDocx_Success()
        {
            // Arrange - Precondition: Can connect with server
            var file = CreateTestFormFile("test.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
            var subFolder = "tickets";
            var uploadedBy = "user1";
            var fileId = Guid.NewGuid();

            var savedFile = new FileEntity
            {
                FileId = fileId,
                OriginalName = "test.docx",
                StoragePath = "/tickets/test.docx",
                MimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.UtcNow
            };

            _fileStorageHelperMock
                .Setup(f => f.SaveAsync(file, subFolder!, uploadedBy))
                .ReturnsAsync(savedFile);

            _ticketRepositoryMock
                .Setup(r => r.AddFileAsync(It.IsAny<FileEntity>()))
                .ReturnsAsync(savedFile);

            // Act
            var result = await _service.UploadFileAsync(file, subFolder!, uploadedBy);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(fileId, result.FileId);
            Assert.AreEqual("test.docx", result.OriginalName);
        }

        // UTCID12: Upload file hợp lệ - Boundary size (10MB) - Success
        // Precondition: Can connect with server
        // Input: IFormFile hợp lệ với size = 10MB (boundary), subFolder = "tickets", uploadedBy = "user1"
        // Expected: FileDto returned successfully
        // Result Type: N (Normal)
        // Log message: None
        [TestMethod]
        public async Task UploadFileAsync_ValidFileBoundarySize10MB_Success()
        {
            // Arrange - Precondition: Can connect with server
            var file = CreateTestFormFile("test.pdf", "application/pdf", fileSizeBytes: 10 * 1024 * 1024); // Exactly 10MB
            var subFolder = "tickets";
            var uploadedBy = "user1";
            var fileId = Guid.NewGuid();

            var savedFile = new FileEntity
            {
                FileId = fileId,
                OriginalName = "test.pdf",
                StoragePath = "/tickets/test.pdf",
                MimeType = "application/pdf",
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.UtcNow
            };

            _fileStorageHelperMock
                .Setup(f => f.SaveAsync(file, subFolder!, uploadedBy))
                .ReturnsAsync(savedFile);

            _ticketRepositoryMock
                .Setup(r => r.AddFileAsync(It.IsAny<FileEntity>()))
                .ReturnsAsync(savedFile);

            // Act
            var result = await _service.UploadFileAsync(file, subFolder!, uploadedBy);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(fileId, result.FileId);
        }

        // UTCID13: FileStorageHelper throws InvalidOperationException (storage error)
        // Precondition: FileStorageHelper throws InvalidOperationException
        // Input: IFormFile hợp lệ, subFolder = "tickets", uploadedBy = "user1"
        // Expected: InvalidOperationException thrown
        // Exception: InvalidOperationException
        // Result Type: A (Abnormal)
        // Log message: None
        [TestMethod]
        public async Task UploadFileAsync_FileStorageThrowsInvalidOperationException_ThrowsException()
        {
            // Arrange - Precondition: FileStorageHelper throws InvalidOperationException
            var file = CreateTestFormFile("test.pdf", "application/pdf");
            var subFolder = "tickets";
            var uploadedBy = "user1";

            _fileStorageHelperMock
                .Setup(f => f.SaveAsync(file, subFolder, uploadedBy))
                .ThrowsAsync(new InvalidOperationException("Cloudinary upload failed: Network error"));

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await _service.UploadFileAsync(file, subFolder, uploadedBy));
            Assert.IsTrue(ex.Message.Contains("Cloudinary upload failed"));
        }



        // UTCID14: FileStorageHelper throws ArgumentException (file too large for tickets)
        // Precondition: FileStorageHelper throws ArgumentException due to file size > 100MB
        // Input: IFormFile với size = 101MB, subFolder = "tickets", uploadedBy = "user1"
        // Expected: ArgumentException thrown
        // Exception: ArgumentException
        // Result Type: A (Abnormal)
        // Log message: None
        [TestMethod]
        public async Task UploadFileAsync_FileTooLargeForDocuments_ThrowsArgumentException()
        {
            // Arrange - Precondition: FileStorageHelper throws ArgumentException due to file size > 100MB
            var file = CreateTestFormFile("huge-document.pdf", "application/pdf", fileSizeBytes: 101 * 1024 * 1024); // 101MB > 100MB limit
            var subFolder = "tickets";
            var uploadedBy = "user1";

            _fileStorageHelperMock
                .Setup(f => f.SaveAsync(file, subFolder, uploadedBy))
                .ThrowsAsync(new ArgumentException("File size (101.00MB) exceeds maximum allowed size (100MB)."));

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.UploadFileAsync(file, subFolder, uploadedBy));
            Assert.IsTrue(ex.Message.Contains("exceeds maximum allowed size"));
        }

        // UTCID16: Repository throws exception
        // Precondition: Repository throws exception
        // Input: IFormFile hợp lệ, subFolder = "tickets", uploadedBy = "user1"
        // Expected: DbUpdateException thrown
        // Exception: DbUpdateException
        // Result Type: A (Abnormal)
        // Log message: None
        [TestMethod]
        public async Task UploadFileAsync_RepositoryThrowsException_ThrowsException()
        {
            // Arrange - Precondition: Repository throws exception
            var file = CreateTestFormFile("test.pdf", "application/pdf");
            var subFolder = "tickets";
            var uploadedBy = "user1";
            var savedFile = new FileEntity
            {
                FileId = Guid.NewGuid(),
                OriginalName = "test.pdf",
                StoragePath = "/tickets/test.pdf",
                MimeType = "application/pdf"
            };

            _fileStorageHelperMock
                .Setup(f => f.SaveAsync(file, subFolder!, uploadedBy))
                .ReturnsAsync(savedFile);

            _ticketRepositoryMock
                .Setup(r => r.AddFileAsync(It.IsAny<FileEntity>()))
                .ThrowsAsync(new DbUpdateException("Database error"));

            // Act & Assert
            await Assert.ThrowsExceptionAsync<DbUpdateException>(async () => await _service.UploadFileAsync(file, subFolder, uploadedBy));
        }

        private IFormFile CreateTestFormFile(string fileName = "test.pdf", string contentType = "application/pdf", string? content = null, long? fileSizeBytes = null)
        {
            byte[] bytes;
            if (fileSizeBytes.HasValue)
            {
                // Create a byte array of the specified size
                bytes = new byte[fileSizeBytes.Value];
                // Fill with some data (not all zeros)
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = (byte)(i % 256);
                }
            }
            else
            {
                // Use provided content or default
                bytes = System.Text.Encoding.UTF8.GetBytes(content ?? "test content");
            }

            var stream = new MemoryStream(bytes);
            var formFile = new Mock<IFormFile>();
            formFile.Setup(f => f.FileName).Returns(fileName);
            formFile.Setup(f => f.ContentType).Returns(contentType);
            formFile.Setup(f => f.Length).Returns(bytes.Length);
            formFile.Setup(f => f.OpenReadStream()).Returns(stream);
            return formFile.Object;
        }

        #endregion

        #region GetFileAsync Tests

        // UTCID01: File tồn tại - Success
        // Precondition: Can connect with server
        // Input: FileId = existing file GUID
        // Expected: FileDto returned successfully
        // Result Type: N (Normal)
        [TestMethod]
        public async Task GetFileAsync_FileExists_ReturnsFile()
        {
            // Arrange - Precondition: Can connect with server
            var fileId = Guid.NewGuid();
            var file = new FileEntity
            {
                FileId = fileId,
                OriginalName = "test.pdf",
                StoragePath = "/tickets/test.pdf",
                MimeType = "application/pdf",
                UploadedBy = "user1",
                UploadedAt = DateTime.UtcNow
            };

            _ticketRepositoryMock
                .Setup(r => r.GetFileByIdAsync(fileId))
                .ReturnsAsync(file);

            // Act
            var result = await _service.GetFileAsync(fileId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(fileId, result.FileId);
            Assert.AreEqual("test.pdf", result.OriginalName);
        }

        // UTCID02: File không tồn tại - Returns null
        // Precondition: Can connect with server
        // Input: FileId = non-existent GUID
        // Expected: Returns null
        // Result Type: N (Normal)
        [TestMethod]
        public async Task GetFileAsync_FileNotFound_ReturnsNull()
        {
            // Arrange - Precondition: Can connect with server
            var fileId = Guid.NewGuid();

            _ticketRepositoryMock
                .Setup(r => r.GetFileByIdAsync(fileId))
                .ReturnsAsync((FileEntity?)null);

            // Act
            var result = await _service.GetFileAsync(fileId);

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region DeleteFileAsync Tests

        // UTCID01: File tồn tại - Success
        // Precondition: Can connect with server
        // Input: FileId = existing file GUID
        // Expected: Returns true
        // Result Type: N (Normal)
        [TestMethod]
        public async Task DeleteFileAsync_FileExists_ReturnsTrue()
        {
            // Arrange - Precondition: Can connect with server
            var fileId = Guid.NewGuid();
            var file = new FileEntity
            {
                FileId = fileId,
                OriginalName = "test.pdf"
            };

            _ticketRepositoryMock
                .Setup(r => r.GetFileByIdAsync(fileId))
                .ReturnsAsync(file);

            _ticketRepositoryMock
                .Setup(r => r.DeleteFileAsync(fileId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.DeleteFileAsync(fileId);

            // Assert
            Assert.IsTrue(result);
            _ticketRepositoryMock.Verify(r => r.DeleteFileAsync(fileId), Times.Once);
        }

        // UTCID02: File không tồn tại - Returns false
        // Precondition: Can connect with server
        // Input: FileId = non-existent GUID
        // Expected: Returns false
        // Result Type: N (Normal)
        [TestMethod]
        public async Task DeleteFileAsync_FileNotFound_ReturnsFalse()
        {
            // Arrange - Precondition: Can connect with server
            var fileId = Guid.NewGuid();

            _ticketRepositoryMock
                .Setup(r => r.GetFileByIdAsync(fileId))
                .ReturnsAsync((FileEntity?)null);

            // Act
            var result = await _service.DeleteFileAsync(fileId);

            // Assert
            Assert.IsFalse(result);
            _ticketRepositoryMock.Verify(r => r.DeleteFileAsync(It.IsAny<Guid>()), Times.Never);
        }

        // UTCID03: Repository throws exception
        // Precondition: Repository throws exception
        // Input: FileId = existing file GUID
        // Expected: DbUpdateException thrown
        // Result Type: A (Abnormal)
        [TestMethod]
        public async Task DeleteFileAsync_RepositoryThrowsException_ThrowsException()
        {
            // Arrange - Precondition: Repository throws exception
            var fileId = Guid.NewGuid();
            var file = new FileEntity
            {
                FileId = fileId,
                OriginalName = "test.pdf"
            };

            _ticketRepositoryMock
                .Setup(r => r.GetFileByIdAsync(fileId))
                .ReturnsAsync(file);

            _ticketRepositoryMock
                .Setup(r => r.DeleteFileAsync(fileId))
                .ThrowsAsync(new DbUpdateException("Database error"));

            // Act & Assert
            await Assert.ThrowsExceptionAsync<DbUpdateException>(async () => await _service.DeleteFileAsync(fileId));
        }

        #endregion

        #region GetInvoiceDetailsAsync Tests

        // UTCID01: Ticket có invoice với details - Success
        // Precondition: Can connect with server, Ticket exists với invoice có details
        // Input: TicketId = existing ticket với invoice có details
        // Expected: Returns list of InvoiceDetailDto
        // Result Type: N (Normal)
        [TestMethod]
        public async Task GetInvoiceDetailsAsync_TicketWithInvoice_ReturnsDetails()
        {
            // Arrange - Precondition: Can connect with server, Ticket exists với invoice có details
            // Sử dụng in-memory database để test async EF Core operations
            var options = new DbContextOptionsBuilder<BuildingManagementContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var tenantAccessor = new TenantContextAccessor();
            using var db = new BuildingManagementContext(options, tenantAccessor);
            var ticketRepo = new TicketRepository(db);
            var userRepo = new Mock<IUserRepository>();
            var fileStorageMock = new Mock<IFileStorageHelper>();
            var loggerMock = new Mock<ILogger<TicketService>>();
            var service = new TicketService(ticketRepo, userRepo.Object, fileStorageMock.Object, db, loggerMock.Object);

            var ticketId = Guid.NewGuid();
            var invoiceId = Guid.NewGuid();
            var invoice = new Invoice
            {
                InvoiceId = invoiceId,
                InvoiceNo = $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}",
                TicketId = ticketId,
                Status = "ISSUED",
                ApartmentId = Guid.NewGuid(),
                IssueDate = DateOnly.FromDateTime(DateTime.UtcNow),
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
                SubtotalAmount = 0,
                TaxAmount = 0,
                TotalAmount = 0,
                CreatedAt = DateTime.UtcNow
            };
            db.Invoices.Add(invoice);

            var invoiceDetails = new List<InvoiceDetail>
            {
                new InvoiceDetail
                {
                    InvoiceDetailId = Guid.NewGuid(),
                    InvoiceId = invoiceId,
                    Description = "Item 1",
                    Quantity = 1,
                    UnitPrice = 1000,
                    Amount = 1000
                },
                new InvoiceDetail
                {
                    InvoiceDetailId = Guid.NewGuid(),
                    InvoiceId = invoiceId,
                    Description = "Item 2",
                    Quantity = 2,
                    UnitPrice = 500,
                    Amount = 1000
                }
            };
            db.InvoiceDetails.AddRange(invoiceDetails);
            await db.SaveChangesAsync();

            // Act
            var result = await service.GetInvoiceDetailsAsync(ticketId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
        }

        // UTCID02: Ticket không có invoice - Returns empty
        // Precondition: Can connect with server, Ticket exists nhưng không có invoice
        // Input: TicketId = existing ticket không có invoice
        // Expected: Returns empty list
        // Result Type: N (Normal)
        [TestMethod]
        public async Task GetInvoiceDetailsAsync_TicketWithoutInvoice_ReturnsEmpty()
        {
            // Arrange - Precondition: Can connect with server, Ticket exists nhưng không có invoice
            // Sử dụng in-memory database để test async EF Core operations
            var options = new DbContextOptionsBuilder<BuildingManagementContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var tenantAccessor = new TenantContextAccessor();
            using var db = new BuildingManagementContext(options, tenantAccessor);
            var ticketRepo = new TicketRepository(db);
            var userRepo = new Mock<IUserRepository>();
            var fileStorageMock = new Mock<IFileStorageHelper>();
            var loggerMock = new Mock<ILogger<TicketService>>();
            var service = new TicketService(ticketRepo, userRepo.Object, fileStorageMock.Object, db, loggerMock.Object);

            var ticketId = Guid.NewGuid();
            // Không thêm invoice nào

            // Act
            var result = await service.GetInvoiceDetailsAsync(ticketId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        // UTCID03: Ticket có invoice nhưng không có details - Returns empty
        // Precondition: Can connect with server, Ticket exists với invoice nhưng không có details
        // Input: TicketId = existing ticket với invoice không có details
        // Expected: Returns empty list
        // Result Type: N (Normal)
        [TestMethod]
        public async Task GetInvoiceDetailsAsync_InvoiceWithoutDetails_ReturnsEmpty()
        {
            // Arrange - Precondition: Can connect with server, Ticket exists với invoice nhưng không có details
            // Sử dụng in-memory database để test async EF Core operations
            var options = new DbContextOptionsBuilder<BuildingManagementContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var tenantAccessor = new TenantContextAccessor();
            using var db = new BuildingManagementContext(options, tenantAccessor);
            var ticketRepo = new TicketRepository(db);
            var userRepo = new Mock<IUserRepository>();
            var fileStorageMock = new Mock<IFileStorageHelper>();
            var loggerMock = new Mock<ILogger<TicketService>>();
            var service = new TicketService(ticketRepo, userRepo.Object, fileStorageMock.Object, db, loggerMock.Object);

            var ticketId = Guid.NewGuid();
            var invoiceId = Guid.NewGuid();
            var invoice = new Invoice
            {
                InvoiceId = invoiceId,
                InvoiceNo = $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}",
                TicketId = ticketId,
                Status = "ISSUED",
                ApartmentId = Guid.NewGuid(),
                IssueDate = DateOnly.FromDateTime(DateTime.UtcNow),
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
                SubtotalAmount = 0,
                TaxAmount = 0,
                TotalAmount = 0,
                CreatedAt = DateTime.UtcNow
            };
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();
            // Không thêm invoice details

            // Act
            var result = await service.GetInvoiceDetailsAsync(ticketId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        #endregion

        #region GetVoucherItemsAsync Tests

        // UTCID01: Ticket có voucher với items - Success
        // Precondition: Can connect with server, Ticket exists với voucher có items
        // Input: TicketId = existing ticket với voucher có items
        // Expected: Returns list of VoucherItemDto
        // Result Type: N (Normal)
        [TestMethod]
        public async Task GetVoucherItemsAsync_TicketWithVoucher_ReturnsItems()
        {
            // Arrange - Precondition: Can connect with server, Ticket exists với voucher có items
            // Sử dụng in-memory database để test async EF Core operations
            var options = new DbContextOptionsBuilder<BuildingManagementContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var tenantAccessor = new TenantContextAccessor();
            using var db = new BuildingManagementContext(options, tenantAccessor);
            var ticketRepo = new TicketRepository(db);
            var userRepo = new Mock<IUserRepository>();
            var fileStorageMock = new Mock<IFileStorageHelper>();
            var loggerMock = new Mock<ILogger<TicketService>>();
            var service = new TicketService(ticketRepo, userRepo.Object, fileStorageMock.Object, db, loggerMock.Object);

            var ticketId = Guid.NewGuid();
            var voucherId = Guid.NewGuid();
            var voucher = new Voucher
            {
                VoucherId = voucherId,
                VoucherNumber = $"VCH-{DateTime.UtcNow:yyyyMMddHHmmss}",
                TicketId = ticketId,
                Type = "EXPENSE",
                Status = "DRAFT",
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                TotalAmount = 0,
                CreatedAt = DateTime.UtcNow
            };
            db.Vouchers.Add(voucher);

            var voucherItems = new List<VoucherItem>
            {
                new VoucherItem
                {
                    VoucherItemsId = Guid.NewGuid(),
                    VoucherId = voucherId,
                    Description = "Item 1",
                    Quantity = 1,
                    UnitPrice = 1000,
                    Amount = 1000
                },
                new VoucherItem
                {
                    VoucherItemsId = Guid.NewGuid(),
                    VoucherId = voucherId,
                    Description = "Item 2",
                    Quantity = 2,
                    UnitPrice = 500,
                    Amount = 1000
                }
            };
            db.VoucherItems.AddRange(voucherItems);
            await db.SaveChangesAsync();

            // Act
            var result = await service.GetVoucherItemsAsync(ticketId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
        }

        // UTCID02: Ticket không có voucher - Returns empty
        // Precondition: Can connect with server, Ticket exists nhưng không có voucher
        // Input: TicketId = existing ticket không có voucher
        // Expected: Returns empty list
        // Result Type: N (Normal)
        [TestMethod]
        public async Task GetVoucherItemsAsync_TicketWithoutVoucher_ReturnsEmpty()
        {
            // Arrange - Precondition: Can connect with server, Ticket exists nhưng không có voucher
            // Sử dụng in-memory database để test async EF Core operations
            var options = new DbContextOptionsBuilder<BuildingManagementContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var tenantAccessor = new TenantContextAccessor();
            using var db = new BuildingManagementContext(options, tenantAccessor);
            var ticketRepo = new TicketRepository(db);
            var userRepo = new Mock<IUserRepository>();
            var fileStorageMock = new Mock<IFileStorageHelper>();
            var loggerMock = new Mock<ILogger<TicketService>>();
            var service = new TicketService(ticketRepo, userRepo.Object, fileStorageMock.Object, db, loggerMock.Object);

            var ticketId = Guid.NewGuid();
            // Không thêm voucher nào

            // Act
            var result = await service.GetVoucherItemsAsync(ticketId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        // UTCID03: Ticket có voucher nhưng không có items - Returns empty
        // Precondition: Can connect with server, Ticket exists với voucher nhưng không có items
        // Input: TicketId = existing ticket với voucher không có items
        // Expected: Returns empty list
        // Result Type: N (Normal)
        [TestMethod]
        public async Task GetVoucherItemsAsync_VoucherWithoutItems_ReturnsEmpty()
        {
            // Arrange - Precondition: Can connect with server, Ticket exists với voucher nhưng không có items
            // Sử dụng in-memory database để test async EF Core operations
            var options = new DbContextOptionsBuilder<BuildingManagementContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var tenantAccessor = new TenantContextAccessor();
            using var db = new BuildingManagementContext(options, tenantAccessor);
            var ticketRepo = new TicketRepository(db);
            var userRepo = new Mock<IUserRepository>();
            var fileStorageMock = new Mock<IFileStorageHelper>();
            var loggerMock = new Mock<ILogger<TicketService>>();
            var service = new TicketService(ticketRepo, userRepo.Object, fileStorageMock.Object, db, loggerMock.Object);

            var ticketId = Guid.NewGuid();
            var voucherId = Guid.NewGuid();
            var voucher = new Voucher
            {
                VoucherId = voucherId,
                VoucherNumber = $"VCH-{DateTime.UtcNow:yyyyMMddHHmmss}",
                TicketId = ticketId,
                Type = "EXPENSE",
                Status = "DRAFT",
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                TotalAmount = 0,
                CreatedAt = DateTime.UtcNow
            };
            db.Vouchers.Add(voucher);
            await db.SaveChangesAsync();
            // Không thêm voucher items

            // Act
            var result = await service.GetVoucherItemsAsync(ticketId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        #endregion
    }
}
