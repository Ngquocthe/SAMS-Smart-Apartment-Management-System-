using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces;
using SAMS_BE.Models;
using SAMS_BE.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAMS_BE.Services.Tests
{
    [TestClass]
    public class InvoiceDetailServiceTests
    {
        private Mock<IInvoiceDetailRepository> _detailRepoMock = null!;
        private Mock<IInvoiceRepository> _invoiceRepoMock = null!;
        private Mock<BuildingManagementContext> _contextMock = null!;
        private Mock<ILogger<InvoiceDetailService>> _loggerMock = null!;
        private InvoiceDetailService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _detailRepoMock = new Mock<IInvoiceDetailRepository>();
            _invoiceRepoMock = new Mock<IInvoiceRepository>();
            _contextMock = new Mock<BuildingManagementContext>();
            _loggerMock = new Mock<ILogger<InvoiceDetailService>>();

            _service = new InvoiceDetailService(
                _detailRepoMock.Object,
                _invoiceRepoMock.Object,
                _contextMock.Object,
                _loggerMock.Object);
        }

        #region Helper Methods

        private InvoiceDetail CreateTestInvoiceDetail(
            Guid? id = null,
            Guid? invoiceId = null,
            Guid? serviceId = null,
            decimal quantity = 1,
            decimal unitPrice = 100000,
            decimal? vatRate = 10)
        {
            var detail = new InvoiceDetail
            {
                InvoiceDetailId = id ?? Guid.NewGuid(),
                InvoiceId = invoiceId ?? Guid.NewGuid(),
                ServiceId = serviceId ?? Guid.NewGuid(),
                Description = "Test Service",
                Quantity = quantity,
                UnitPrice = unitPrice,
                VatRate = vatRate,
                Amount = quantity * unitPrice,
                VatAmount = vatRate.HasValue ? (quantity * unitPrice * vatRate.Value / 100) : 0
            };

            return detail;
        }

        private Invoice CreateTestInvoice(Guid? id = null, string status = "DRAFT")
        {
            return new Invoice
            {
                InvoiceId = id ?? Guid.NewGuid(),
                InvoiceNo = "INV-TEST-001",
                ApartmentId = Guid.NewGuid(),
                IssueDate = DateOnly.FromDateTime(DateTime.UtcNow),
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                Status = status,
                SubtotalAmount = 0,
                TaxAmount = 0,
                TotalAmount = 0,
                CreatedAt = DateTime.UtcNow,
                InvoiceDetails = new List<InvoiceDetail>()
            };
        }

        #endregion

        #region CreateAsync Tests

        [TestMethod]
        public async Task CreateAsync_InvoiceNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var createDto = new CreateInvoiceDetailDto
            {
                InvoiceId = Guid.NewGuid(),
                ServiceId = Guid.NewGuid(),
                Quantity = 1,
                Description = "Test"
            };

            _detailRepoMock.Setup(r => r.InvoiceExistsAsync(createDto.InvoiceId))
      .ReturnsAsync(false);

         // Act & Assert
await Assert.ThrowsExceptionAsync<KeyNotFoundException>(
       async () => await _service.CreateAsync(createDto));
      }

  [TestMethod]
        public async Task CreateAsync_ServiceNotFound_ThrowsKeyNotFoundException()
        {
 // Arrange
 var createDto = new CreateInvoiceDetailDto
    {
         InvoiceId = Guid.NewGuid(),
     ServiceId = Guid.NewGuid(),
     Quantity = 1,
    Description = "Test"
            };

   _detailRepoMock.Setup(r => r.InvoiceExistsAsync(createDto.InvoiceId))
          .ReturnsAsync(true);
            _detailRepoMock.Setup(r => r.ServiceExistsAsync(createDto.ServiceId))
  .ReturnsAsync(false);

      // Act & Assert
            await Assert.ThrowsExceptionAsync<KeyNotFoundException>(
        async () => await _service.CreateAsync(createDto));
        }

    [TestMethod]
     public async Task CreateAsync_InvalidQuantity_ThrowsArgumentException()
 {
   // Arrange
    var createDto = new CreateInvoiceDetailDto
        {
                InvoiceId = Guid.NewGuid(),
  ServiceId = Guid.NewGuid(),
     Quantity = 0,
         Description = "Test"
   };

   _detailRepoMock.Setup(r => r.InvoiceExistsAsync(createDto.InvoiceId))
                .ReturnsAsync(true);
_detailRepoMock.Setup(r => r.ServiceExistsAsync(createDto.ServiceId))
                .ReturnsAsync(true);

   // Act & Assert
  await Assert.ThrowsExceptionAsync<ArgumentException>(
      async () => await _service.CreateAsync(createDto));
 }

   #endregion

        #region GetByIdAsync Tests

[TestMethod]
        public async Task GetByIdAsync_DetailExists_ReturnsDto()
  {
      // Arrange
      var detailId = Guid.NewGuid();
      var detail = CreateTestInvoiceDetail(detailId);

          _detailRepoMock.Setup(r => r.GetByIdAsync(detailId))
       .ReturnsAsync(detail);

            // Act
            var result = await _service.GetByIdAsync(detailId);

     // Assert
    Assert.IsNotNull(result);
            Assert.AreEqual(detailId, result.InvoiceDetailId);
       Assert.AreEqual(detail.Description, result.Description);
        }

      [TestMethod]
        public async Task GetByIdAsync_DetailNotFound_ThrowsKeyNotFoundException()
        {
   // Arrange
            var detailId = Guid.NewGuid();
          _detailRepoMock.Setup(r => r.GetByIdAsync(detailId))
       .ReturnsAsync((InvoiceDetail?)null);

       // Act & Assert
    await Assert.ThrowsExceptionAsync<KeyNotFoundException>(
async () => await _service.GetByIdAsync(detailId));
        }

  #endregion

        #region GetByInvoiceIdAsync Tests

        [TestMethod]
        public async Task GetByInvoiceIdAsync_ReturnsDetails()
     {
       // Arrange
            var invoiceId = Guid.NewGuid();
   var details = new List<InvoiceDetail>
       {
   CreateTestInvoiceDetail(invoiceId: invoiceId),
      CreateTestInvoiceDetail(invoiceId: invoiceId)
         };

            _detailRepoMock.Setup(r => r.GetByInvoiceIdAsync(invoiceId))
         .ReturnsAsync(details);

            // Act
       var result = await _service.GetByInvoiceIdAsync(invoiceId);

          // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
        }

        #endregion

     #region ListAsync Tests

        [TestMethod]
        public async Task ListAsync_ReturnsPagedResult()
   {
            // Arrange
            var query = new InvoiceDetailListQueryDto
            {
     Page = 1,
     PageSize = 10
      };

       var details = new List<InvoiceDetail>
   {
            CreateTestInvoiceDetail(),
        CreateTestInvoiceDetail()
   };

    _detailRepoMock.Setup(r => r.ListAsync(query))
    .ReturnsAsync((details, 2));

            // Act
            var result = await _service.ListAsync(query);

   // Assert
      Assert.IsNotNull(result);
    Assert.AreEqual(2, result.TotalItems);
      Assert.AreEqual(2, result.Items.Count());
        }

   [TestMethod]
        public async Task ListAsync_InvalidPage_NormalizesToOne()
        {
            // Arrange
        var query = new InvoiceDetailListQueryDto
{
         Page = 0,
         PageSize = 10
        };

            _detailRepoMock.Setup(r => r.ListAsync(It.IsAny<InvoiceDetailListQueryDto>()))
     .ReturnsAsync((new List<InvoiceDetail>(), 0));

       // Act
            var result = await _service.ListAsync(query);

     // Assert
            Assert.AreEqual(1, result.PageNumber);
        }

        [TestMethod]
     public async Task ListAsync_InvalidPageSize_NormalizesToDefault()
  {
   // Arrange
            var query = new InvoiceDetailListQueryDto
  {
    Page = 1,
            PageSize = 0
};

      _detailRepoMock.Setup(r => r.ListAsync(It.IsAny<InvoiceDetailListQueryDto>()))
        .ReturnsAsync((new List<InvoiceDetail>(), 0));

            // Act
            var result = await _service.ListAsync(query);

          // Assert
            Assert.AreEqual(20, result.PageSize);
        }

        [TestMethod]
      public async Task ListAsync_PageSizeExceedsLimit_NormalizesToDefault()
        {
       // Arrange
    var query = new InvoiceDetailListQueryDto
       {
     Page = 1,
        PageSize = 300
            };

            _detailRepoMock.Setup(r => r.ListAsync(It.IsAny<InvoiceDetailListQueryDto>()))
             .ReturnsAsync((new List<InvoiceDetail>(), 0));

            // Act
            var result = await _service.ListAsync(query);

  // Assert
     Assert.AreEqual(20, result.PageSize);
      }

        #endregion

    #region DeleteAsync Tests

        [TestMethod]
     public async Task DeleteAsync_DetailNotFound_ThrowsKeyNotFoundException()
        {
   // Arrange
            var detailId = Guid.NewGuid();
    _detailRepoMock.Setup(r => r.GetByIdForUpdateAsync(detailId))
          .ReturnsAsync((InvoiceDetail?)null);

       // Act & Assert
            await Assert.ThrowsExceptionAsync<KeyNotFoundException>(
                async () => await _service.DeleteAsync(detailId));
    }

[TestMethod]
     public async Task DeleteAsync_InvoiceNotDraft_ThrowsInvalidOperationException()
 {
      // Arrange
            var detailId = Guid.NewGuid();
   var invoiceId = Guid.NewGuid();
     var detail = CreateTestInvoiceDetail(detailId, invoiceId);
     var invoice = CreateTestInvoice(invoiceId, status: "ISSUED");

 _detailRepoMock.Setup(r => r.GetByIdForUpdateAsync(detailId))
          .ReturnsAsync(detail);
      _invoiceRepoMock.Setup(r => r.GetByIdAsync(invoiceId))
       .ReturnsAsync(invoice);

       // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
     async () => await _service.DeleteAsync(detailId));
        }

        [TestMethod]
        public async Task DeleteAsync_DraftInvoice_Success()
        {
    // Arrange
            var detailId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var detail = CreateTestInvoiceDetail(detailId, invoiceId);
            var invoice = CreateTestInvoice(invoiceId, status: "DRAFT");

 _detailRepoMock.Setup(r => r.GetByIdForUpdateAsync(detailId))
            .ReturnsAsync(detail);
            _invoiceRepoMock.Setup(r => r.GetByIdAsync(invoiceId))
  .ReturnsAsync(invoice);
     _invoiceRepoMock.Setup(r => r.GetByIdWithDetailsAsync(invoiceId))
                .ReturnsAsync(invoice);
    _invoiceRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Invoice>()))
         .ReturnsAsync((Invoice inv) => inv);
 _detailRepoMock.Setup(r => r.DeleteAsync(detail))
          .Returns(Task.CompletedTask);

   // Act
     await _service.DeleteAsync(detailId);

            // Assert
   _detailRepoMock.Verify(r => r.DeleteAsync(detail), Times.Once);
      }

     #endregion

      #region UpdateAsync Tests

    [TestMethod]
        public async Task UpdateAsync_DetailNotFound_ThrowsKeyNotFoundException()
        {
  // Arrange
            var detailId = Guid.NewGuid();
          var updateDto = new UpdateInvoiceDetailDto();

          _detailRepoMock.Setup(r => r.GetByIdForUpdateAsync(detailId))
   .ReturnsAsync((InvoiceDetail?)null);

        // Act & Assert
await Assert.ThrowsExceptionAsync<KeyNotFoundException>(
        async () => await _service.UpdateAsync(detailId, updateDto));
        }

        [TestMethod]
   public async Task UpdateAsync_InvoiceNotDraft_ThrowsInvalidOperationException()
   {
            // Arrange
            var detailId = Guid.NewGuid();
            var invoiceId = Guid.NewGuid();
    var detail = CreateTestInvoiceDetail(detailId, invoiceId);
       var invoice = CreateTestInvoice(invoiceId, status: "PAID");
         var updateDto = new UpdateInvoiceDetailDto { Quantity = 5 };

         _detailRepoMock.Setup(r => r.GetByIdForUpdateAsync(detailId))
    .ReturnsAsync(detail);
       _invoiceRepoMock.Setup(r => r.GetByIdAsync(invoiceId))
.ReturnsAsync(invoice);

     // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
      async () => await _service.UpdateAsync(detailId, updateDto));
 }

  [TestMethod]
 public async Task UpdateAsync_InvalidQuantity_ThrowsArgumentException()
        {
 // Arrange
var detailId = Guid.NewGuid();
   var invoiceId = Guid.NewGuid();
        var detail = CreateTestInvoiceDetail(detailId, invoiceId);
       var invoice = CreateTestInvoice(invoiceId, status: "DRAFT");
        var updateDto = new UpdateInvoiceDetailDto { Quantity = 0 };

        _detailRepoMock.Setup(r => r.GetByIdForUpdateAsync(detailId))
      .ReturnsAsync(detail);
     _invoiceRepoMock.Setup(r => r.GetByIdAsync(invoiceId))
     .ReturnsAsync(invoice);

            // Act & Assert
         await Assert.ThrowsExceptionAsync<ArgumentException>(
         async () => await _service.UpdateAsync(detailId, updateDto));
        }

        [TestMethod]
      public async Task UpdateAsync_ValidQuantityUpdate_Success()
        {
            // Arrange
 var detailId = Guid.NewGuid();
            var invoiceId = Guid.NewGuid();
     var detail = CreateTestInvoiceDetail(detailId, invoiceId, quantity: 1, unitPrice: 100000);
         var invoice = CreateTestInvoice(invoiceId, status: "DRAFT");
         var updateDto = new UpdateInvoiceDetailDto { Quantity = 5 };

            _detailRepoMock.Setup(r => r.GetByIdForUpdateAsync(detailId))
                .ReturnsAsync(detail);
   _invoiceRepoMock.Setup(r => r.GetByIdAsync(invoiceId))
     .ReturnsAsync(invoice);
 _detailRepoMock.Setup(r => r.UpdateAsync(It.IsAny<InvoiceDetail>()))
            .ReturnsAsync((InvoiceDetail d) => d);
      _detailRepoMock.Setup(r => r.GetByIdAsync(detailId))
        .ReturnsAsync(detail);
            _invoiceRepoMock.Setup(r => r.GetByIdWithDetailsAsync(invoiceId))
    .ReturnsAsync(invoice);
            _invoiceRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Invoice>()))
      .ReturnsAsync((Invoice inv) => inv);

      // Act
       var result = await _service.UpdateAsync(detailId, updateDto);

        // Assert
    Assert.IsNotNull(result);
            _detailRepoMock.Verify(r => r.UpdateAsync(It.Is<InvoiceDetail>(d => d.Quantity == 5)), Times.Once);
        }

        [TestMethod]
        public async Task UpdateAsync_UpdateDescription_Success()
 {
   // Arrange
         var detailId = Guid.NewGuid();
            var invoiceId = Guid.NewGuid();
var detail = CreateTestInvoiceDetail(detailId, invoiceId);
            var invoice = CreateTestInvoice(invoiceId, status: "DRAFT");
            var updateDto = new UpdateInvoiceDetailDto { Description = "Updated Description" };

       _detailRepoMock.Setup(r => r.GetByIdForUpdateAsync(detailId))
  .ReturnsAsync(detail);
            _invoiceRepoMock.Setup(r => r.GetByIdAsync(invoiceId))
     .ReturnsAsync(invoice);
         _detailRepoMock.Setup(r => r.UpdateAsync(It.IsAny<InvoiceDetail>()))
            .ReturnsAsync((InvoiceDetail d) => d);
   _detailRepoMock.Setup(r => r.GetByIdAsync(detailId))
       .ReturnsAsync(detail);
      _invoiceRepoMock.Setup(r => r.GetByIdWithDetailsAsync(invoiceId))
                .ReturnsAsync(invoice);
            _invoiceRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Invoice>()))
     .ReturnsAsync((Invoice inv) => inv);

            // Act
            var result = await _service.UpdateAsync(detailId, updateDto);

         // Assert
 Assert.IsNotNull(result);
      _detailRepoMock.Verify(r => r.UpdateAsync(
    It.Is<InvoiceDetail>(d => d.Description == "Updated Description")), Times.Once);
        }

        [TestMethod]
      public async Task UpdateAsync_UpdateVatRate_RecalculatesAmounts()
        {
       // Arrange
            var detailId = Guid.NewGuid();
       var invoiceId = Guid.NewGuid();
      var detail = CreateTestInvoiceDetail(detailId, invoiceId, quantity: 1, unitPrice: 100000, vatRate: 10);
   var invoice = CreateTestInvoice(invoiceId, status: "DRAFT");
            var updateDto = new UpdateInvoiceDetailDto { VatRate = 5 };

      _detailRepoMock.Setup(r => r.GetByIdForUpdateAsync(detailId))
         .ReturnsAsync(detail);
   _invoiceRepoMock.Setup(r => r.GetByIdAsync(invoiceId))
      .ReturnsAsync(invoice);
            _detailRepoMock.Setup(r => r.UpdateAsync(It.IsAny<InvoiceDetail>()))
      .ReturnsAsync((InvoiceDetail d) => d);
 _detailRepoMock.Setup(r => r.GetByIdAsync(detailId))
       .ReturnsAsync(detail);
            _invoiceRepoMock.Setup(r => r.GetByIdWithDetailsAsync(invoiceId))
   .ReturnsAsync(invoice);
      _invoiceRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Invoice>()))
     .ReturnsAsync((Invoice inv) => inv);

    // Act
          var result = await _service.UpdateAsync(detailId, updateDto);

    // Assert
            Assert.IsNotNull(result);
            _detailRepoMock.Verify(r => r.UpdateAsync(
      It.Is<InvoiceDetail>(d => d.VatRate == 5 && d.VatAmount == 5000)), Times.Once);
        }

 #endregion
    }
}
