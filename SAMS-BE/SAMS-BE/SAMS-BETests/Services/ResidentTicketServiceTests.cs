using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SAMS_BE.DTOs;
using SAMS_BE.Helpers;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Models;
using SAMS_BE.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAMS_BE.Services.Tests
{
    [TestClass]
    public class ResidentTicketServiceTests
    {
        private Mock<IResidentTicketRepository> _repositoryMock = null!;
        private Mock<IUserService> _userServiceMock = null!;
        private Mock<IUserRepository> _userRepositoryMock = null!;
        private Mock<IFileStorageHelper> _fileStorageHelperMock = null!;
        private Mock<ILogger<ResidentTicketService>> _loggerMock = null!;
        private ResidentTicketService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _repositoryMock = new Mock<IResidentTicketRepository>();
            _userServiceMock = new Mock<IUserService>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _fileStorageHelperMock = new Mock<IFileStorageHelper>();
            _loggerMock = new Mock<ILogger<ResidentTicketService>>();
            _service = new ResidentTicketService(_repositoryMock.Object, _userServiceMock.Object, _userRepositoryMock.Object, _fileStorageHelperMock.Object, _loggerMock.Object);
        }

        #region Helper Methods

        private Ticket CreateTestTicket(Guid? ticketId = null, Guid? userId = null, Guid? apartmentId = null)
        {
            return new Ticket
            {
                TicketId = ticketId ?? Guid.NewGuid(),
                Subject = "Test Ticket",
                Description = "Test Description",
                Category = "Bảo trì",
                Priority = "Bình thường",
                Status = "Mới tạo",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId ?? Guid.NewGuid(),
                ApartmentId = apartmentId ?? Guid.NewGuid()
            };
        }

        private Apartment CreateTestApartmentModel(Guid? apartmentId = null)
        {
            return new Apartment
            {
                ApartmentId = apartmentId ?? Guid.NewGuid(),
                Number = "A0801"
            };
        }

        private ApartmentResponseDto CreateTestApartment(Guid? apartmentId = null)
        {
            return new ApartmentResponseDto
            {
                ApartmentId = apartmentId ?? Guid.NewGuid(),
                Number = "A0801"
            };
        }

        #endregion

        #region CreateMaintenanceTicketAsync Tests

        [TestMethod]
        public async Task CreateMaintenanceTicketAsync_WithApartmentId_Success()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var apartmentId = Guid.NewGuid();
            var dto = new CreateMaintenanceTicketDto
            {
                ApartmentId = apartmentId,
                Subject = "Maintenance Request",
                Description = "Fix the door"
            };

            Ticket? capturedTicket = null;
            _repositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Ticket>()))
                .ReturnsAsync((Ticket t) => 
                {
                    capturedTicket = t;
                    return t;
                });

            _repositoryMock
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) => capturedTicket ?? CreateTestTicket(userId: userId, apartmentId: apartmentId));

            // Act
            var result = await _service.CreateMaintenanceTicketAsync(dto, userId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(dto.Subject, result.Subject);
            Assert.IsNotNull(capturedTicket);
            Assert.AreEqual(dto.Subject, capturedTicket.Subject);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Ticket>()), Times.Once);
        }

        [TestMethod]
        public async Task CreateMaintenanceTicketAsync_WithoutApartmentId_UsesPrimaryApartment()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var apartmentId = Guid.NewGuid();
            var apartmentModel = CreateTestApartmentModel(apartmentId);
            var dto = new CreateMaintenanceTicketDto
            {
                ApartmentId = null,
                Subject = "Maintenance Request",
                Description = "Fix the door"
            };

            _userServiceMock
                .Setup(s => s.GetUserPrimaryApartmentAsync(userId))
                .ReturnsAsync(apartmentModel);

            var createdTicket = CreateTestTicket(userId: userId, apartmentId: apartmentId);
            _repositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Ticket>()))
                .ReturnsAsync((Ticket t) => t);

            _repositoryMock
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(createdTicket);

            // Act
            var result = await _service.CreateMaintenanceTicketAsync(dto, userId);

            // Assert
            Assert.IsNotNull(result);
            _userServiceMock.Verify(s => s.GetUserPrimaryApartmentAsync(userId), Times.Once);
        }

        [TestMethod]
        public async Task CreateMaintenanceTicketAsync_NoPrimaryApartment_ThrowsException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dto = new CreateMaintenanceTicketDto
            {
                ApartmentId = null,
                Subject = "Maintenance Request",
                Description = "Test description"
            };

            _userServiceMock
                .Setup(s => s.GetUserPrimaryApartmentAsync(userId))
                .ReturnsAsync((Apartment?)null);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await _service.CreateMaintenanceTicketAsync(dto, userId));
        }

        #endregion

        #region CreateComplaintTicketAsync Tests

        [TestMethod]
        public async Task CreateComplaintTicketAsync_WithApartmentId_Success()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var apartmentId = Guid.NewGuid();
            var dto = new CreateComplaintTicketDto
            {
                ApartmentId = apartmentId,
                Subject = "Complaint",
                Description = "Noise complaint"
            };

            Ticket? capturedTicket = null;
            _repositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Ticket>()))
                .ReturnsAsync((Ticket t) => 
                {
                    capturedTicket = t;
                    return t;
                });

            _repositoryMock
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) => capturedTicket ?? CreateTestTicket(userId: userId, apartmentId: apartmentId));

            // Act
            var result = await _service.CreateComplaintTicketAsync(dto, userId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(dto.Subject, result.Subject);
            Assert.IsNotNull(capturedTicket);
            Assert.AreEqual(dto.Subject, capturedTicket.Subject);
        }

        #endregion

        #region GetTicketByIdAsync Tests

        [TestMethod]
        public async Task GetTicketByIdAsync_TicketExists_ReturnsTicket()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var ticket = CreateTestTicket(ticketId, userId);

            _repositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            _userServiceMock
                .Setup(s => s.GetLoginUserAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new SAMS_BE.DTOs.Response.LoginUserDto { Username = "testuser" });

            // Act
            var result = await _service.GetTicketByIdAsync(ticketId, userId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(ticketId, result.TicketId);
        }

        [TestMethod]
        public async Task GetTicketByIdAsync_TicketNotFound_ReturnsNull()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _repositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync((Ticket?)null);

            // Act
            var result = await _service.GetTicketByIdAsync(ticketId, userId);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetTicketByIdAsync_UnauthorizedUser_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var ownerUserId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var ticket = CreateTestTicket(ticketId, ownerUserId);

            _repositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(async () => await _service.GetTicketByIdAsync(ticketId, otherUserId));
        }

        #endregion

        #region AddCommentAsync Tests

        [TestMethod]
        public async Task AddCommentAsync_ValidData_Success()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var ticket = CreateTestTicket(ticketId, userId);
            var commentDto = new CreateResidentTicketCommentDto
            {
                TicketId = ticketId,
                Content = "Test comment"
            };

            _repositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            var comment = new TicketComment
            {
                CommentId = Guid.NewGuid(),
                TicketId = ticketId,
                Content = "Test comment",
                CommentedBy = userId
            };

            _repositoryMock
                .Setup(r => r.AddCommentAsync(It.IsAny<TicketComment>()))
                .ReturnsAsync((TicketComment c) => c);

            _userServiceMock
                .Setup(s => s.GetLoginUserAsync(userId))
                .ReturnsAsync(new SAMS_BE.DTOs.Response.LoginUserDto { Username = "testuser" });

            // Act
            var result = await _service.AddCommentAsync(commentDto, userId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(commentDto.Content, result.Content);
            _repositoryMock.Verify(r => r.AddCommentAsync(It.IsAny<TicketComment>()), Times.Once);
        }

        [TestMethod]
        public async Task AddCommentAsync_UnauthorizedUser_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var ownerUserId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var ticket = CreateTestTicket(ticketId, ownerUserId);
            var commentDto = new CreateResidentTicketCommentDto
            {
                TicketId = ticketId,
                Content = "Test comment"
            };

            _repositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(async () => await _service.AddCommentAsync(commentDto, otherUserId));
        }

        #endregion

        #region AddAttachmentAsync Tests

        [TestMethod]
        public async Task AddAttachmentAsync_ValidData_Success()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var fileId = Guid.NewGuid();
            var ticket = CreateTestTicket(ticketId, userId);

            var file = new SAMS_BE.Models.File
            {
                FileId = fileId,
                OriginalName = "test.pdf",
                MimeType = "application/pdf",
                StoragePath = "test/path"
            };

            _repositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            _repositoryMock
                .Setup(r => r.GetFileByIdAsync(fileId))
                .ReturnsAsync(file);

            var attachment = new TicketAttachment
            {
                AttachmentId = Guid.NewGuid(),
                TicketId = ticketId,
                FileId = fileId,
                UploadedBy = userId
            };

            _repositoryMock
                .Setup(r => r.AddAttachmentAsync(It.IsAny<TicketAttachment>()))
                .ReturnsAsync((TicketAttachment a) => a);

            _repositoryMock
                .Setup(r => r.GetAttachmentByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(attachment);

            // Act
            var result = await _service.AddAttachmentAsync(ticketId, fileId, null, userId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(ticketId, result.TicketId);
            _repositoryMock.Verify(r => r.AddAttachmentAsync(It.IsAny<TicketAttachment>()), Times.Once);
        }

        [TestMethod]
        public async Task AddAttachmentAsync_UnauthorizedUser_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var ownerUserId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var fileId = Guid.NewGuid();
            var ticket = CreateTestTicket(ticketId, ownerUserId);

            _repositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(async () => await _service.AddAttachmentAsync(ticketId, fileId, null, otherUserId));
        }

        [TestMethod]
        public async Task AddAttachmentAsync_FileNotFound_ThrowsArgumentException()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var fileId = Guid.NewGuid();
            var ticket = CreateTestTicket(ticketId, userId);

            _repositoryMock
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            _repositoryMock
                .Setup(r => r.GetFileByIdAsync(fileId))
                .ReturnsAsync((SAMS_BE.Models.File?)null);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await _service.AddAttachmentAsync(ticketId, fileId, null, userId));
        }

        #endregion

        #region DeleteAttachmentAsync Tests

        [TestMethod]
        public async Task DeleteAttachmentAsync_OwnerDeletes_Success()
        {
            // Arrange
            var attachmentId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var attachment = new TicketAttachment
            {
                AttachmentId = attachmentId,
                TicketId = Guid.NewGuid(),
                UploadedBy = userId
            };

            _repositoryMock
                .Setup(r => r.GetAttachmentByIdAsync(attachmentId))
                .ReturnsAsync(attachment);

            _repositoryMock
                .Setup(r => r.DeleteAttachmentAsync(attachmentId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.DeleteAttachmentAsync(attachmentId, userId);

            // Assert
            Assert.IsTrue(result);
            _repositoryMock.Verify(r => r.DeleteAttachmentAsync(attachmentId), Times.Once);
        }

        [TestMethod]
        public async Task DeleteAttachmentAsync_OtherUserDeletes_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var attachmentId = Guid.NewGuid();
            var ownerUserId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var attachment = new TicketAttachment
            {
                AttachmentId = attachmentId,
                TicketId = Guid.NewGuid(),
                UploadedBy = ownerUserId
            };

            _repositoryMock
                .Setup(r => r.GetAttachmentByIdAsync(attachmentId))
                .ReturnsAsync(attachment);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(async () => await _service.DeleteAttachmentAsync(attachmentId, otherUserId));
        }

        [TestMethod]
        public async Task DeleteAttachmentAsync_AttachmentNotFound_ReturnsFalse()
        {
            // Arrange
            var attachmentId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _repositoryMock
                .Setup(r => r.GetAttachmentByIdAsync(attachmentId))
                .ReturnsAsync((TicketAttachment?)null);

            // Act
            var result = await _service.DeleteAttachmentAsync(attachmentId, userId);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region GetMyTicketStatisticsAsync Tests

        // Note: This test is skipped because it requires complex IQueryable mocking for EF Core
        // In a real scenario, you would use an in-memory database or a more sophisticated mocking library

        #endregion
    }
}
