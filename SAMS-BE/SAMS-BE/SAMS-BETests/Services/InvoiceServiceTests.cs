using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Interfaces.IMail;
using SAMS_BE.Models;
using SAMS_BE.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IVoucherService = SAMS_BE.Interfaces.IService.IVoucherService;
using SAMS_BE.Interfaces.IService;

namespace SAMS_BE.Services.Tests
{
    [TestClass]
    public class InvoiceServiceTests
    {
        private Mock<IInvoiceRepository> _invoiceRepoMock = null!;
        private Mock<ILogger<InvoiceService>> _loggerMock = null!;
        private Mock<IApartmentRepository> _apartmentRepoMock = null!;
        private Mock<IServiceTypeRepository> _serviceTypeRepoMock = null!;
        private Mock<IServicePriceRepository> _servicePriceRepoMock = null!;
        private Mock<ITicketRepository> _ticketRepoMock = null!;
        private Mock<BuildingManagementContext> _contextMock = null!;
        private Mock<IVoucherService> _voucherServiceMock = null!;
        private Mock<IInvoiceConfigurationService> _invoiceConfigServiceMock = null!;
        private Mock<IEmailSender> _emailSenderMock = null!;
        private InvoiceService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _invoiceRepoMock = new Mock<IInvoiceRepository>();
            _loggerMock = new Mock<ILogger<InvoiceService>>();
            _apartmentRepoMock = new Mock<IApartmentRepository>();
            _serviceTypeRepoMock = new Mock<IServiceTypeRepository>();
            _servicePriceRepoMock = new Mock<IServicePriceRepository>();
            _ticketRepoMock = new Mock<ITicketRepository>();
            _contextMock = new Mock<BuildingManagementContext>();
            _voucherServiceMock = new Mock<IVoucherService>();
            _invoiceConfigServiceMock = new Mock<IInvoiceConfigurationService>();
            _emailSenderMock = new Mock<IEmailSender>();

            _service = new InvoiceService(
                _invoiceRepoMock.Object,
                _loggerMock.Object,
                _apartmentRepoMock.Object,
                _serviceTypeRepoMock.Object,
                _servicePriceRepoMock.Object,
                _ticketRepoMock.Object,
                _invoiceRepoMock.Object,
                _contextMock.Object,
                _voucherServiceMock.Object,
                _invoiceConfigServiceMock.Object,
                _emailSenderMock.Object);
        }

        #region Helper Methods

        private Invoice CreateTestInvoice(
            Guid? id = null,
            string invoiceNo = "INV202500001",
            string status = "ISSUED",
            decimal totalAmount = 1000000)
        {
            return new Invoice
            {
                InvoiceId = id ?? Guid.NewGuid(),
                InvoiceNo = invoiceNo,
                ApartmentId = Guid.NewGuid(),
                IssueDate = DateOnly.FromDateTime(DateTime.UtcNow),
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                Status = status,
                SubtotalAmount = totalAmount,
                TaxAmount = 0,
                TotalAmount = totalAmount,
                CreatedAt = DateTime.UtcNow
            };
        }

        private Apartment CreateTestApartment(Guid? id = null)
        {
            return new Apartment
            {
                ApartmentId = id ?? Guid.NewGuid(),
                Number = "A101",
                FloorId = Guid.NewGuid(),
                Status = "OCCUPIED",
                Floor = new Floor
                {
                    FloorId = Guid.NewGuid(),
                    FloorNumber = 1,
                    Name = "Floor 1"
                }
            };
        }

        #endregion

        #region GetByIdAsync Tests

        [TestMethod]
        public async Task GetByIdAsync_InvoiceExists_ReturnsDto()
        {
            // Arrange
            var invoiceId = Guid.NewGuid();
            var invoice = CreateTestInvoice(invoiceId);

            _invoiceRepoMock.Setup(r => r.GetByIdAsync(invoiceId))
                .ReturnsAsync(invoice);

            // Act
            var result = await _service.GetByIdAsync(invoiceId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(invoiceId, result.InvoiceId);
            Assert.AreEqual("INV202500001", result.InvoiceNo);
        }

        [TestMethod]
        public async Task GetByIdAsync_InvoiceNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var invoiceId = Guid.NewGuid();
            _invoiceRepoMock.Setup(r => r.GetByIdAsync(invoiceId))
                .ReturnsAsync((Invoice?)null);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<KeyNotFoundException>(
                async () => await _service.GetByIdAsync(invoiceId));
        }

        #endregion

        #region ListAsync Tests

        [TestMethod]
        public async Task ListAsync_ReturnsPagedResult()
        {
            // Arrange
            var query = new InvoiceListQueryDto
            {
                Page = 1,
                PageSize = 10
            };

            var invoices = new List<Invoice>
            {
                CreateTestInvoice(invoiceNo: "INV001"),
                CreateTestInvoice(invoiceNo: "INV002")
            };

            _invoiceRepoMock.Setup(r => r.ListAsync(query))
                .ReturnsAsync((invoices, 2));

            // Act
            var result = await _service.ListAsync(query);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.TotalItems);
            Assert.AreEqual(2, result.Items.Count());
        }

        #endregion

        #region UpdateAsync Tests

        [TestMethod]
        public async Task UpdateAsync_ValidData_Success()
        {
            // Arrange
            var invoiceId = Guid.NewGuid();
            var apartmentId = Guid.NewGuid();
            var invoice = CreateTestInvoice(invoiceId, status: "DRAFT");
            var apartment = CreateTestApartment(apartmentId);

            var updateDto = new UpdateInvoiceDto
            {
                ApartmentId = apartmentId,
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)),
                Note = "Updated note"
            };

            _invoiceRepoMock.Setup(r => r.GetByIdForUpdateAsync(invoiceId))
                .ReturnsAsync(invoice);
            _apartmentRepoMock.Setup(r => r.GetApartmentByIdAsync(apartmentId))
                .ReturnsAsync(apartment);
            _invoiceRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Invoice>()))
                .ReturnsAsync((Invoice inv) => inv);
            _invoiceRepoMock.Setup(r => r.GetByIdAsync(invoiceId))
                .ReturnsAsync(invoice);

            // Act
            var result = await _service.UpdateAsync(invoiceId, updateDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Updated note", result.Note);
            _invoiceRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Invoice>()), Times.Once);
        }

        [TestMethod]
        public async Task UpdateAsync_InvoiceNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var invoiceId = Guid.NewGuid();
            var updateDto = new UpdateInvoiceDto();

            _invoiceRepoMock.Setup(r => r.GetByIdForUpdateAsync(invoiceId))
                .ReturnsAsync((Invoice?)null);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<KeyNotFoundException>(
                async () => await _service.UpdateAsync(invoiceId, updateDto));
        }

        [TestMethod]
        public async Task UpdateAsync_PaidInvoice_ThrowsInvalidOperationException()
        {
            // Arrange
            var invoiceId = Guid.NewGuid();
            var invoice = CreateTestInvoice(invoiceId, status: "PAID");
            var updateDto = new UpdateInvoiceDto();

            _invoiceRepoMock.Setup(r => r.GetByIdForUpdateAsync(invoiceId))
                .ReturnsAsync(invoice);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await _service.UpdateAsync(invoiceId, updateDto));
        }

        [TestMethod]
        public async Task UpdateAsync_DueDateBeforeIssueDate_ThrowsArgumentException()
        {
            // Arrange
            var invoiceId = Guid.NewGuid();
            var invoice = CreateTestInvoice(invoiceId, status: "DRAFT");
            invoice.IssueDate = DateOnly.FromDateTime(DateTime.UtcNow);

            var updateDto = new UpdateInvoiceDto
            {
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10))
            };

            _invoiceRepoMock.Setup(r => r.GetByIdForUpdateAsync(invoiceId))
                .ReturnsAsync(invoice);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await _service.UpdateAsync(invoiceId, updateDto));
        }

        #endregion

        #region DeleteAsync Tests

        [TestMethod]
        public async Task DeleteAsync_DraftInvoice_Success()
        {
            // Arrange
            var invoiceId = Guid.NewGuid();
            var invoice = CreateTestInvoice(invoiceId, status: "DRAFT");

            _invoiceRepoMock.Setup(r => r.GetByIdForUpdateAsync(invoiceId))
                .ReturnsAsync(invoice);
            _invoiceRepoMock.Setup(r => r.DeleteAsync(invoiceId))
                .Returns(Task.CompletedTask);

            // Act
            await _service.DeleteAsync(invoiceId);

            // Assert
            _invoiceRepoMock.Verify(r => r.DeleteAsync(invoiceId), Times.Once);
        }

        [TestMethod]
        public async Task DeleteAsync_NonDraftInvoice_ThrowsInvalidOperationException()
        {
            // Arrange
            var invoiceId = Guid.NewGuid();
            var invoice = CreateTestInvoice(invoiceId, status: "ISSUED");

            _invoiceRepoMock.Setup(r => r.GetByIdForUpdateAsync(invoiceId))
                .ReturnsAsync(invoice);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await _service.DeleteAsync(invoiceId));
        }

        [TestMethod]
        public async Task DeleteAsync_InvoiceNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var invoiceId = Guid.NewGuid();
            _invoiceRepoMock.Setup(r => r.GetByIdForUpdateAsync(invoiceId))
                .ReturnsAsync((Invoice?)null);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<KeyNotFoundException>(
                async () => await _service.DeleteAsync(invoiceId));
        }

        #endregion

        #region UpdateStatusAsync Tests

        [TestMethod]
        public async Task UpdateStatusAsync_ValidTransition_Success()
        {
            // Arrange
            var invoiceId = Guid.NewGuid();
            var invoice = CreateTestInvoice(invoiceId, status: "ISSUED");

            var statusDto = new UpdateInvoiceStatusDto
            {
                Status = "PAID",
                Note = "Payment received"
            };

            _invoiceRepoMock.Setup(r => r.GetByIdForUpdateAsync(invoiceId))
                .ReturnsAsync(invoice);
            _invoiceRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Invoice>()))
                .ReturnsAsync((Invoice inv) => inv);
            _invoiceRepoMock.Setup(r => r.GetByIdAsync(invoiceId))
                .ReturnsAsync(invoice);

            // Act
            var result = await _service.UpdateStatusAsync(invoiceId, statusDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("PAID", result.Status);
        }

        [TestMethod]
        public async Task UpdateStatusAsync_SameStatus_ThrowsInvalidOperationException()
        {
            // Arrange
            var invoiceId = Guid.NewGuid();
            var invoice = CreateTestInvoice(invoiceId, status: "PAID");

            var statusDto = new UpdateInvoiceStatusDto
            {
                Status = "PAID"
            };

            _invoiceRepoMock.Setup(r => r.GetByIdForUpdateAsync(invoiceId))
                .ReturnsAsync(invoice);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await _service.UpdateStatusAsync(invoiceId, statusDto));
        }

        [TestMethod]
        public async Task UpdateStatusAsync_InvalidTransition_ThrowsInvalidOperationException()
        {
            // Arrange
            var invoiceId = Guid.NewGuid();
            var invoice = CreateTestInvoice(invoiceId, status: "PAID");

            var statusDto = new UpdateInvoiceStatusDto
            {
                Status = "DRAFT"
            };

            _invoiceRepoMock.Setup(r => r.GetByIdForUpdateAsync(invoiceId))
                .ReturnsAsync(invoice);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await _service.UpdateStatusAsync(invoiceId, statusDto));
        }

        #endregion

        #region UpdateOverdueInvoicesAsync Tests

        [TestMethod]
        public async Task UpdateOverdueInvoicesAsync_UpdatesOverdueInvoices()
        {
            // Arrange
            var today = DateOnly.FromDateTime(DateTime.Now);
            var overdueInvoices = new List<Invoice>
            {
                CreateTestInvoice(status: "ISSUED"),
                CreateTestInvoice(status: "ISSUED")
            };

            // Set due dates to past
            foreach (var inv in overdueInvoices)
            {
                inv.DueDate = today.AddDays(-10);
            }

            _invoiceRepoMock.Setup(r => r.GetOverdueInvoicesAsync(today))
                .ReturnsAsync(overdueInvoices);
            _invoiceRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Invoice>()))
                .ReturnsAsync((Invoice inv) => inv);

            // Act
            var count = await _service.UpdateOverdueInvoicesAsync();

            // Assert
            Assert.AreEqual(2, count);
            _invoiceRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Invoice>()), Times.Exactly(2));
        }

        [TestMethod]
        public async Task UpdateOverdueInvoicesAsync_NoOverdueInvoices_ReturnsZero()
        {
            // Arrange
            var today = DateOnly.FromDateTime(DateTime.Now);
            _invoiceRepoMock.Setup(r => r.GetOverdueInvoicesAsync(today))
                .ReturnsAsync(new List<Invoice>());

            // Act
            var count = await _service.UpdateOverdueInvoicesAsync();

            // Assert
            Assert.AreEqual(0, count);
        }

        #endregion
    }
}
