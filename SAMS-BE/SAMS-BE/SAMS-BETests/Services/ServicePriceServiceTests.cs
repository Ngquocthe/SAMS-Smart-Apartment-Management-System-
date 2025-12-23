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
    public class ServicePriceServiceTests
    {
        private Mock<IServicePriceRepository> _repMock = null!;
        private Mock<IServiceTypeRepository> _typeRepoMock = null!;
     private ServicePriceService _service = null!;

        [TestInitialize]
   public void Setup()
    {
            _repMock = new Mock<IServicePriceRepository>();
   _typeRepoMock = new Mock<IServiceTypeRepository>();
    _service = new ServicePriceService(_repMock.Object, _typeRepoMock.Object);
      }

    #region Helper Methods

        private ServiceType CreateTestServiceType(
    Guid? id = null,
            string code = "ELECTRIC",
  bool isActive = true,
            bool isDelete = false)
     {
         return new ServiceType
     {
     ServiceTypeId = id ?? Guid.NewGuid(),
         Code = code,
           Name = "?i?n",
    CategoryId = Guid.NewGuid(),
                Unit = "kWh",
            IsActive = isActive,
        IsDelete = isDelete,
         CreatedAt = DateTime.UtcNow
            };
        }

    private ServicePrice CreateTestServicePrice(
    Guid? id = null,
      Guid? serviceTypeId = null,
        decimal unitPrice = 1800,
    DateOnly? effectiveDate = null,
            DateOnly? endDate = null,
            string status = "APPROVED")
      {
            return new ServicePrice
       {
   ServicePrices = id ?? Guid.NewGuid(),
          ServiceTypeId = serviceTypeId ?? Guid.NewGuid(),
 UnitPrice = unitPrice,
        EffectiveDate = effectiveDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = endDate,
              Status = status,
    CreatedAt = DateTime.UtcNow,
            ApprovedDate = DateTime.UtcNow,
      ServiceType = CreateTestServiceType(serviceTypeId)
  };
        }

        #endregion

        #region ListAsync Tests

        [TestMethod]
        public async Task ListAsync_ValidQuery_ReturnsPagedResult()
        {
        // Arrange
       var serviceTypeId = Guid.NewGuid();
        var query = new ServicePriceListQueryDto
            {
                Page = 1,
         PageSize = 10
            };

            var prices = new List<ServicePrice>
        {
        CreateTestServicePrice(serviceTypeId: serviceTypeId, unitPrice: 1800),
   CreateTestServicePrice(serviceTypeId: serviceTypeId, unitPrice: 2000)
         };

            _repMock
      .Setup(r => r.ListAsync(serviceTypeId, query))
    .ReturnsAsync((prices, 2));

            // Act
  var result = await _service.ListAsync(serviceTypeId, query);

    // Assert
            Assert.IsNotNull(result);
     Assert.AreEqual(2, result.TotalItems);
            Assert.AreEqual(2, result.Items.Count());
    Assert.AreEqual(1, result.PageNumber);
            Assert.AreEqual(10, result.PageSize);
      }

        [TestMethod]
        public async Task ListAsync_InvalidDateRange_ThrowsArgumentException()
        {
            // Arrange
    var serviceTypeId = Guid.NewGuid();
       var query = new ServicePriceListQueryDto
 {
   FromDate = DateOnly.FromDateTime(DateTime.UtcNow),
        ToDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10))
    };

         // Act & Assert
         await Assert.ThrowsExceptionAsync<ArgumentException>(
    async () => await _service.ListAsync(serviceTypeId, query),
   "FromDate must be <= ToDate.");
        }

        [TestMethod]
     public async Task ListAsync_PageLessThanOne_DefaultsToPageOne()
        {
        // Arrange
            var serviceTypeId = Guid.NewGuid();
          var query = new ServicePriceListQueryDto { Page = 0, PageSize = 10 };

      _repMock
        .Setup(r => r.ListAsync(serviceTypeId, query))
  .ReturnsAsync((new List<ServicePrice>(), 0));

            // Act
  var result = await _service.ListAsync(serviceTypeId, query);

     // Assert
            Assert.AreEqual(1, result.PageNumber);
  }

        [TestMethod]
     public async Task ListAsync_PageSizeLessThanOne_DefaultsToTwenty()
   {
   // Arrange
            var serviceTypeId = Guid.NewGuid();
            var query = new ServicePriceListQueryDto { Page = 1, PageSize = 0 };

            _repMock
                .Setup(r => r.ListAsync(serviceTypeId, query))
      .ReturnsAsync((new List<ServicePrice>(), 0));

          // Act
    var result = await _service.ListAsync(serviceTypeId, query);

            // Assert
        Assert.AreEqual(20, result.PageSize);
        }

     #endregion

        #region CreateAsync Tests

        [TestMethod]
     public async Task CreateAsync_ValidData_Success()
        {
 // Arrange
            var serviceTypeId = Guid.NewGuid();
            var serviceType = CreateTestServiceType(serviceTypeId);
          var dto = new CreateServicePriceDto
       {
    UnitPrice = 2000,
        EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
         Notes = "New price"
 };

   _typeRepoMock
     .Setup(r => r.GetByIdForUpdateAsync(serviceTypeId))
   .ReturnsAsync(serviceType);

          _repMock
                .Setup(r => r.AnyOverlapAsync(serviceTypeId, dto.EffectiveDate, dto.EndDate, null))
            .ReturnsAsync(false);

        _repMock
         .Setup(r => r.GetOpenEndedAsync(serviceTypeId))
  .ReturnsAsync((ServicePrice?)null);

            _repMock
    .Setup(r => r.AddAsync(It.IsAny<ServicePrice>()))
            .Returns(Task.CompletedTask);

       _repMock
         .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
      .ReturnsAsync((Guid id) => CreateTestServicePrice(id, serviceTypeId, 2000));

  // Act
        var result = await _service.CreateAsync(serviceTypeId, dto);

            // Assert
       Assert.IsNotNull(result);
       Assert.AreEqual(2000, result.UnitPrice);
         Assert.AreEqual("APPROVED", result.Status);
            _repMock.Verify(r => r.AddAsync(It.IsAny<ServicePrice>()), Times.Once);
      }

        [TestMethod]
        public async Task CreateAsync_ZeroPrice_ThrowsArgumentException()
     {
         // Arrange
       var serviceTypeId = Guid.NewGuid();
       var dto = new CreateServicePriceDto
            {
  UnitPrice = 0,
    EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow)
       };

       // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
    async () => await _service.CreateAsync(serviceTypeId, dto),
          "Unit price must be > 0.");
        }

        [TestMethod]
        public async Task CreateAsync_NegativePrice_ThrowsArgumentException()
        {
  // Arrange
      var serviceTypeId = Guid.NewGuid();
  var dto = new CreateServicePriceDto
   {
    UnitPrice = -100,
             EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow)
       };

         // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await _service.CreateAsync(serviceTypeId, dto));
        }

        [TestMethod]
public async Task CreateAsync_EndDateBeforeEffectiveDate_ThrowsArgumentException()
        {
        // Arrange
          var serviceTypeId = Guid.NewGuid();
       var dto = new CreateServicePriceDto
  {
                UnitPrice = 2000,
      EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
      EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10))
            };

   // Act & Assert
    await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await _service.CreateAsync(serviceTypeId, dto),
"EndDate must be >= EffectiveDate.");
        }

        [TestMethod]
        public async Task CreateAsync_ServiceTypeNotFound_ThrowsKeyNotFoundException()
        {
 // Arrange
            var serviceTypeId = Guid.NewGuid();
    var dto = new CreateServicePriceDto
        {
           UnitPrice = 2000,
  EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow)
      };

            _typeRepoMock
     .Setup(r => r.GetByIdForUpdateAsync(serviceTypeId))
      .ReturnsAsync((ServiceType?)null);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<KeyNotFoundException>(
   async () => await _service.CreateAsync(serviceTypeId, dto),
       "Service type not found.");
        }

        [TestMethod]
      public async Task CreateAsync_ServiceTypeDeleted_ThrowsInvalidOperationException()
        {
            // Arrange
 var serviceTypeId = Guid.NewGuid();
    var serviceType = CreateTestServiceType(serviceTypeId, isDelete: true);
   var dto = new CreateServicePriceDto
            {
           UnitPrice = 2000,
  EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow)
         };

         _typeRepoMock
         .Setup(r => r.GetByIdForUpdateAsync(serviceTypeId))
         .ReturnsAsync(serviceType);

       // Act & Assert
          await Assert.ThrowsExceptionAsync<InvalidOperationException>(
      async () => await _service.CreateAsync(serviceTypeId, dto),
       "Service type was deleted.");
        }

        [TestMethod]
        public async Task CreateAsync_OverlappingPeriodWithoutAutoClose_ThrowsInvalidOperationException()
     {
      // Arrange
    var serviceTypeId = Guid.NewGuid();
       var serviceType = CreateTestServiceType(serviceTypeId);
            var dto = new CreateServicePriceDto
   {
 UnitPrice = 2000,
        EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow)
            };

            _typeRepoMock
 .Setup(r => r.GetByIdForUpdateAsync(serviceTypeId))
     .ReturnsAsync(serviceType);

      _repMock
    .Setup(r => r.AnyOverlapAsync(serviceTypeId, dto.EffectiveDate, dto.EndDate, null))
                .ReturnsAsync(true);

       _repMock
         .Setup(r => r.GetOpenEndedAsync(serviceTypeId))
    .ReturnsAsync((ServicePrice?)null);

  // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
           async () => await _service.CreateAsync(serviceTypeId, dto, autoClosePrevious: false),
                "Price period overlaps existing records.");
        }

    [TestMethod]
        public async Task CreateAsync_WithAutoClosePrevious_ClosesOpenEndedPrice()
     {
            // Arrange
   var serviceTypeId = Guid.NewGuid();
            var serviceType = CreateTestServiceType(serviceTypeId);
       var effectiveDate = DateOnly.FromDateTime(DateTime.UtcNow);
 var existingPrice = CreateTestServicePrice(
         serviceTypeId: serviceTypeId,
           effectiveDate: effectiveDate.AddDays(-30),
         endDate: null);

 var dto = new CreateServicePriceDto
            {
     UnitPrice = 2000,
              EffectiveDate = effectiveDate
    };

    _typeRepoMock
                .Setup(r => r.GetByIdForUpdateAsync(serviceTypeId))
             .ReturnsAsync(serviceType);

 _repMock
    .Setup(r => r.AnyOverlapAsync(serviceTypeId, dto.EffectiveDate, dto.EndDate, null))
 .ReturnsAsync(true);

    _repMock
  .Setup(r => r.GetOpenEndedAsync(serviceTypeId))
   .ReturnsAsync(existingPrice);

          _repMock
        .Setup(r => r.UpdateAsync(It.IsAny<ServicePrice>()))
   .Returns(Task.CompletedTask);

 _repMock
    .Setup(r => r.AddAsync(It.IsAny<ServicePrice>()))
.Returns(Task.CompletedTask);

         _repMock
             .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
  .ReturnsAsync((Guid id) => CreateTestServicePrice(id, serviceTypeId, 2000));

            // Act
       var result = await _service.CreateAsync(serviceTypeId, dto, autoClosePrevious: true);

            // Assert
        Assert.IsNotNull(result);
         Assert.AreEqual(effectiveDate.AddDays(-1), existingPrice.EndDate);
     _repMock.Verify(r => r.UpdateAsync(existingPrice), Times.Once);
         _repMock.Verify(r => r.AddAsync(It.IsAny<ServicePrice>()), Times.Once);
}

        [TestMethod]
        public async Task CreateAsync_RoundsDecimalCorrectly()
     {
 // Arrange
     var serviceTypeId = Guid.NewGuid();
 var serviceType = CreateTestServiceType(serviceTypeId);
            var dto = new CreateServicePriceDto
     {
    UnitPrice = 1999.999m,
    EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow)
            };

        _typeRepoMock
.Setup(r => r.GetByIdForUpdateAsync(serviceTypeId))
     .ReturnsAsync(serviceType);

            _repMock
  .Setup(r => r.AnyOverlapAsync(serviceTypeId, dto.EffectiveDate, dto.EndDate, null))
   .ReturnsAsync(false);

  _repMock
                .Setup(r => r.GetOpenEndedAsync(serviceTypeId))
      .ReturnsAsync((ServicePrice?)null);

            ServicePrice? capturedPrice = null;
     _repMock
  .Setup(r => r.AddAsync(It.IsAny<ServicePrice>()))
       .Callback<ServicePrice>(p => capturedPrice = p)
           .Returns(Task.CompletedTask);

      _repMock
       .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
 .ReturnsAsync((Guid id) => capturedPrice ?? CreateTestServicePrice(id, serviceTypeId, 2000));

            // Act
            await _service.CreateAsync(serviceTypeId, dto);

         // Assert
 Assert.IsNotNull(capturedPrice);
          Assert.AreEqual(2000.00m, capturedPrice.UnitPrice);
        }

 #endregion

     #region UpdateAsync Tests

   [TestMethod]
   public async Task UpdateAsync_ValidData_Success()
    {
      // Arrange
      var priceId = Guid.NewGuid();
       var serviceTypeId = Guid.NewGuid();
            var existingPrice = CreateTestServicePrice(priceId, serviceTypeId, 1800);
 var updateDto = new UpdateServicePriceDto
            {
  UnitPrice = 2200,
              EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Notes = "Updated price"
          };

    _repMock
     .Setup(r => r.GetByIdAsync(priceId))
                .ReturnsAsync(existingPrice);

    _repMock
    .Setup(r => r.AnyOverlapAsync(serviceTypeId, updateDto.EffectiveDate, updateDto.EndDate, priceId))
.ReturnsAsync(false);

            _repMock
       .Setup(r => r.UpdateAsync(It.IsAny<ServicePrice>()))
         .Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateAsync(priceId, updateDto);

       // Assert
       Assert.IsNotNull(result);
   Assert.AreEqual(2200, result.UnitPrice);
 _repMock.Verify(r => r.UpdateAsync(It.IsAny<ServicePrice>()), Times.Once);
        }

        [TestMethod]
        public async Task UpdateAsync_PriceNotFound_ReturnsNull()
 {
     // Arrange
            var priceId = Guid.NewGuid();
            var updateDto = new UpdateServicePriceDto
            {
    UnitPrice = 2000,
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow)
            };

         _repMock
         .Setup(r => r.GetByIdAsync(priceId))
      .ReturnsAsync((ServicePrice?)null);

       // Act
            var result = await _service.UpdateAsync(priceId, updateDto);

    // Assert
            Assert.IsNull(result);
        _repMock.Verify(r => r.UpdateAsync(It.IsAny<ServicePrice>()), Times.Never);
        }

        [TestMethod]
        public async Task UpdateAsync_ZeroPrice_ThrowsArgumentException()
 {
            // Arrange
     var priceId = Guid.NewGuid();
            var updateDto = new UpdateServicePriceDto
       {
      UnitPrice = 0,
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow)
          };

 // Act & Assert
          await Assert.ThrowsExceptionAsync<ArgumentException>(
     async () => await _service.UpdateAsync(priceId, updateDto));
      }

        [TestMethod]
        public async Task UpdateAsync_EndDateBeforeEffectiveDate_ThrowsArgumentException()
        {
         // Arrange
            var priceId = Guid.NewGuid();
       var updateDto = new UpdateServicePriceDto
            {
 UnitPrice = 2000,
       EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
     EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5))
 };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
          async () => await _service.UpdateAsync(priceId, updateDto));
}

        [TestMethod]
        public async Task UpdateAsync_CanceledPrice_ThrowsInvalidOperationException()
        {
            // Arrange
      var priceId = Guid.NewGuid();
    var serviceTypeId = Guid.NewGuid();
         var existingPrice = CreateTestServicePrice(priceId, serviceTypeId, 1800, status: "CANCELED");
   var updateDto = new UpdateServicePriceDto
     {
     UnitPrice = 2000,
      EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow)
      };

        _repMock
      .Setup(r => r.GetByIdAsync(priceId))
          .ReturnsAsync(existingPrice);

        // Act & Assert
    await Assert.ThrowsExceptionAsync<InvalidOperationException>(
    async () => await _service.UpdateAsync(priceId, updateDto),
   "This price is canceled and cannot be updated.");
   }

        [TestMethod]
        public async Task UpdateAsync_OverlappingPeriod_ThrowsInvalidOperationException()
        {
            // Arrange
 var priceId = Guid.NewGuid();
     var serviceTypeId = Guid.NewGuid();
            var existingPrice = CreateTestServicePrice(priceId, serviceTypeId, 1800);
   var updateDto = new UpdateServicePriceDto
            {
        UnitPrice = 2000,
                EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow)
       };

     _repMock
        .Setup(r => r.GetByIdAsync(priceId))
  .ReturnsAsync(existingPrice);

            _repMock
      .Setup(r => r.AnyOverlapAsync(serviceTypeId, updateDto.EffectiveDate, updateDto.EndDate, priceId))
        .ReturnsAsync(true);

            // Act & Assert
      await Assert.ThrowsExceptionAsync<InvalidOperationException>(
        async () => await _service.UpdateAsync(priceId, updateDto),
           "Updated period overlaps existing records.");
     }

        #endregion

        #region CancelAsync Tests

      [TestMethod]
        public async Task CancelAsync_ValidPrice_Success()
        {
   // Arrange
      var priceId = Guid.NewGuid();
  var serviceTypeId = Guid.NewGuid();
   var existingPrice = CreateTestServicePrice(priceId, serviceTypeId, 1800);

 _repMock
.Setup(r => r.GetByIdAsync(priceId))
         .ReturnsAsync(existingPrice);

         _repMock
            .Setup(r => r.UpdateAsync(It.IsAny<ServicePrice>()))
     .Returns(Task.CompletedTask);

  // Act
            var result = await _service.CancelAsync(priceId);

            // Assert
            Assert.IsTrue(result);
    Assert.AreEqual("CANCELED", existingPrice.Status);
            Assert.IsNotNull(existingPrice.EndDate);
     _repMock.Verify(r => r.UpdateAsync(existingPrice), Times.Once);
        }

        [TestMethod]
        public async Task CancelAsync_PriceNotFound_ReturnsFalse()
      {
   // Arrange
        var priceId = Guid.NewGuid();
    _repMock
           .Setup(r => r.GetByIdAsync(priceId))
                .ReturnsAsync((ServicePrice?)null);

         // Act
     var result = await _service.CancelAsync(priceId);

         // Assert
     Assert.IsFalse(result);
        _repMock.Verify(r => r.UpdateAsync(It.IsAny<ServicePrice>()), Times.Never);
        }

      [TestMethod]
        public async Task CancelAsync_AlreadyCanceled_ReturnsTrue()
        {
          // Arrange
  var priceId = Guid.NewGuid();
            var serviceTypeId = Guid.NewGuid();
   var existingPrice = CreateTestServicePrice(priceId, serviceTypeId, 1800, status: "CANCELED");

         _repMock
                .Setup(r => r.GetByIdAsync(priceId))
       .ReturnsAsync(existingPrice);

     // Act
            var result = await _service.CancelAsync(priceId);

         // Assert
        Assert.IsTrue(result);
            _repMock.Verify(r => r.UpdateAsync(It.IsAny<ServicePrice>()), Times.Never);
        }

        [TestMethod]
        public async Task CancelAsync_SetsEndDateIfNull()
   {
            // Arrange
    var priceId = Guid.NewGuid();
        var serviceTypeId = Guid.NewGuid();
            var existingPrice = CreateTestServicePrice(priceId, serviceTypeId, 1800, endDate: null);

     _repMock
           .Setup(r => r.GetByIdAsync(priceId))
       .ReturnsAsync(existingPrice);

      _repMock
           .Setup(r => r.UpdateAsync(It.IsAny<ServicePrice>()))
     .Returns(Task.CompletedTask);

          // Act
 var result = await _service.CancelAsync(priceId);

      // Assert
        Assert.IsTrue(result);
            Assert.IsNotNull(existingPrice.EndDate);
     Assert.AreEqual(DateOnly.FromDateTime(DateTime.UtcNow.Date), existingPrice.EndDate);
        }

        [TestMethod]
        public async Task CancelAsync_KeepsExistingEndDate()
        {
            // Arrange
var priceId = Guid.NewGuid();
       var serviceTypeId = Guid.NewGuid();
      var originalEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
            var existingPrice = CreateTestServicePrice(priceId, serviceTypeId, 1800, endDate: originalEndDate);

      _repMock
                .Setup(r => r.GetByIdAsync(priceId))
      .ReturnsAsync(existingPrice);

        _repMock
          .Setup(r => r.UpdateAsync(It.IsAny<ServicePrice>()))
           .Returns(Task.CompletedTask);

      // Act
var result = await _service.CancelAsync(priceId);

            // Assert
         Assert.IsTrue(result);
   Assert.AreEqual(originalEndDate, existingPrice.EndDate);
}

        #endregion

        #region GetCurrentPriceAsync Tests

        [TestMethod]
        public async Task GetCurrentPriceAsync_PriceExists_ReturnsUnitPrice()
        {
         // Arrange
          var serviceTypeId = Guid.NewGuid();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
 var currentPrice = CreateTestServicePrice(serviceTypeId: serviceTypeId, unitPrice: 2500);

   _repMock
        .Setup(r => r.GetCurrentPriceAsync(serviceTypeId, today))
              .ReturnsAsync(currentPrice);

   // Act
            var result = await _service.GetCurrentPriceAsync(serviceTypeId);

            // Assert
 Assert.IsNotNull(result);
    Assert.AreEqual(2500m, result.Value);
     }

        [TestMethod]
        public async Task GetCurrentPriceAsync_NoPriceFound_ReturnsNull()
        {
   // Arrange
            var serviceTypeId = Guid.NewGuid();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

 _repMock
           .Setup(r => r.GetCurrentPriceAsync(serviceTypeId, today))
       .ReturnsAsync((ServicePrice?)null);

    // Act
            var result = await _service.GetCurrentPriceAsync(serviceTypeId);

            // Assert
      Assert.IsNull(result);
     }

     [TestMethod]
        public async Task GetCurrentPriceAsync_WithSpecificDate_UsesProvidedDate()
        {
          // Arrange
        var serviceTypeId = Guid.NewGuid();
    var specificDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
 var historicalPrice = CreateTestServicePrice(serviceTypeId: serviceTypeId, unitPrice: 1500);

   _repMock
     .Setup(r => r.GetCurrentPriceAsync(serviceTypeId, specificDate))
         .ReturnsAsync(historicalPrice);

  // Act
       var result = await _service.GetCurrentPriceAsync(serviceTypeId, specificDate);

   // Assert
            Assert.IsNotNull(result);
      Assert.AreEqual(1500m, result.Value);
   _repMock.Verify(r => r.GetCurrentPriceAsync(serviceTypeId, specificDate), Times.Once);
   }

        [TestMethod]
        public async Task GetCurrentPriceAsync_NoDateProvided_UsesToday()
        {
     // Arrange
   var serviceTypeId = Guid.NewGuid();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

         _repMock
       .Setup(r => r.GetCurrentPriceAsync(serviceTypeId, today))
         .ReturnsAsync(CreateTestServicePrice(serviceTypeId: serviceTypeId, unitPrice: 2000));

       // Act
        var result = await _service.GetCurrentPriceAsync(serviceTypeId, null);

  // Assert
    Assert.IsNotNull(result);
        _repMock.Verify(r => r.GetCurrentPriceAsync(serviceTypeId, today), Times.Once);
        }

    #endregion
 }
}
