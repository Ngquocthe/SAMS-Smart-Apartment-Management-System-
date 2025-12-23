using Microsoft.EntityFrameworkCore;
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
    public class ServiceTypeServiceTests
    {
        private Mock<IServiceTypeRepository> _repositoryMock = null!;
        private Mock<ILogger<ServiceTypeService>> _loggerMock = null!;
        private ServiceTypeService _service = null!;

    [TestInitialize]
        public void Setup()
        {
          _repositoryMock = new Mock<IServiceTypeRepository>();
            _loggerMock = new Mock<ILogger<ServiceTypeService>>();
            _service = new ServiceTypeService(_repositoryMock.Object, _loggerMock.Object);
 }

  #region Helper Methods

        private ServiceType CreateTestServiceType(
Guid? id = null,
        string code = "ELECTRIC",
            string name = "?i?n",
         bool isActive = true,
            bool isDelete = false,
            bool isMandatory = false,
       bool isRecurring = false)
        {
         return new ServiceType
     {
      ServiceTypeId = id ?? Guid.NewGuid(),
                Code = code,
       Name = name,
           CategoryId = Guid.NewGuid(),
                Unit = "kWh",
 IsMandatory = isMandatory,
      IsRecurring = isRecurring,
     IsActive = isActive,
            IsDelete = isDelete,
              CreatedAt = DateTime.UtcNow,
       UpdatedAt = null,
          Category = new ServiceTypeCategory 
   { 
     CategoryId = Guid.NewGuid(), 
       Name = "Utilities" 
     }
          };
  }

  #endregion

        #region CreateAsync Tests

        [TestMethod]
        public async Task CreateAsync_ValidData_Success()
    {
            // Arrange
 var dto = new CreateServiceTypeDto
            {
      Code = "water",
                Name = "N??c",
       CategoryId = Guid.NewGuid(),
             Unit = "m³",
   IsMandatory = false,
  IsRecurring = true
       };

            _repositoryMock
        .Setup(r => r.CodeExistsAsync("WATER"))
          .ReturnsAsync(false);

    _repositoryMock
.Setup(r => r.CreateAsync(It.IsAny<ServiceType>()))
     .ReturnsAsync((ServiceType st) => st);

        // Act
 var result = await _service.CreateAsync(dto);

 // Assert
   Assert.IsNotNull(result);
          Assert.AreEqual("WATER", result.Code); // Code should be normalized to uppercase
            Assert.AreEqual("N??c", result.Name);
          _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<ServiceType>()), Times.Once);
        }

 [TestMethod]
        public async Task CreateAsync_EmptyCode_ThrowsArgumentException()
    {
      // Arrange
            var dto = new CreateServiceTypeDto
 {
                Code = "",
    Name = "Test",
       CategoryId = Guid.NewGuid(),
       Unit = "unit"
      };

         // Act & Assert
   await Assert.ThrowsExceptionAsync<ArgumentException>(
           async () => await _service.CreateAsync(dto),
        "Code is required.");
        }

        [TestMethod]
  public async Task CreateAsync_CodeTooShort_ThrowsArgumentException()
        {
     // Arrange
        var dto = new CreateServiceTypeDto
      {
                Code = "A",
        Name = "Test",
         CategoryId = Guid.NewGuid(),
       Unit = "unit"
  };

            // Act & Assert
 await Assert.ThrowsExceptionAsync<ArgumentException>(
       async () => await _service.CreateAsync(dto),
 "Code length must be between 2 and 50 characters.");
        }

        [TestMethod]
        public async Task CreateAsync_CodeWithInvalidCharacters_ThrowsArgumentException()
        {
    // Arrange
       var dto = new CreateServiceTypeDto
     {
    Code = "TEST-CODE",
                Name = "Test",
         CategoryId = Guid.NewGuid(),
        Unit = "unit"
    };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
          async () => await _service.CreateAsync(dto),
           "Code must contain only A-Z, 0-9 or underscore.");
        }

        [TestMethod]
public async Task CreateAsync_DuplicateCode_ThrowsInvalidOperationException()
        {
            // Arrange
       var dto = new CreateServiceTypeDto
            {
          Code = "ELECTRIC",
    Name = "?i?n",
            CategoryId = Guid.NewGuid(),
    Unit = "kWh"
            };

            _repositoryMock
                .Setup(r => r.CodeExistsAsync("ELECTRIC"))
 .ReturnsAsync(true);

        // Act & Assert
   await Assert.ThrowsExceptionAsync<InvalidOperationException>(
       async () => await _service.CreateAsync(dto));
        }

        [TestMethod]
 public async Task CreateAsync_MandatoryButNotRecurring_ThrowsArgumentException()
      {
          // Arrange
 var dto = new CreateServiceTypeDto
         {
 Code = "TEST",
    Name = "Test",
      CategoryId = Guid.NewGuid(),
        Unit = "unit",
        IsMandatory = true,
      IsRecurring = false
        };

// Act & Assert
   await Assert.ThrowsExceptionAsync<ArgumentException>(
            async () => await _service.CreateAsync(dto),
     "Mandatory service must also be Recurring.");
      }

     [TestMethod]
    public async Task CreateAsync_EmptyCategoryId_ThrowsArgumentException()
     {
  // Arrange
  var dto = new CreateServiceTypeDto
            {
        Code = "TEST",
 Name = "Test",
      CategoryId = Guid.Empty,
                Unit = "unit"
     };

            // Act & Assert
      await Assert.ThrowsExceptionAsync<ArgumentException>(
            async () => await _service.CreateAsync(dto),
       "CategoryId is required.");
      }

     [TestMethod]
        public async Task CreateAsync_DbUpdateException_ThrowsInvalidOperationException()
 {
        // Arrange
  var dto = new CreateServiceTypeDto
        {
        Code = "ELECTRIC",
      Name = "?i?n",
  CategoryId = Guid.NewGuid(),
                Unit = "kWh"
  };

_repositoryMock
    .Setup(r => r.CodeExistsAsync("ELECTRIC"))
        .ReturnsAsync(false);

            _repositoryMock
        .Setup(r => r.CreateAsync(It.IsAny<ServiceType>()))
       .ThrowsAsync(new DbUpdateException("Duplicate key"));

            // Act & Assert
   await Assert.ThrowsExceptionAsync<InvalidOperationException>(
      async () => await _service.CreateAsync(dto));
        }

        #endregion

        #region GetByIdAsync Tests

        [TestMethod]
    public async Task GetByIdAsync_ServiceTypeExists_ReturnsDto()
        {
     // Arrange
            var id = Guid.NewGuid();
        var serviceType = CreateTestServiceType(id);

      _repositoryMock
 .Setup(r => r.GetByIdForUpdateAsync(id))
    .ReturnsAsync(serviceType);

            // Act
   var result = await _service.GetByIdAsync(id);

  // Assert
  Assert.IsNotNull(result);
   Assert.AreEqual(id, result.ServiceTypeId);
   Assert.AreEqual("ELECTRIC", result.Code);
        }

        [TestMethod]
        public async Task GetByIdAsync_ServiceTypeNotFound_ReturnsNull()
   {
       // Arrange
      var id = Guid.NewGuid();
            _repositoryMock
       .Setup(r => r.GetByIdForUpdateAsync(id))
      .ReturnsAsync((ServiceType?)null);

  // Act
  var result = await _service.GetByIdAsync(id);

  // Assert
      Assert.IsNull(result);
        }

        #endregion

        #region UpdateAsync Tests

        [TestMethod]
  public async Task UpdateAsync_ValidData_Success()
        {
     // Arrange
        var id = Guid.NewGuid();
            var existingServiceType = CreateTestServiceType(id);
            var updateDto = new UpdateServiceTypeDto
            {
      Name = "?i?n C?p Nh?t",
      CategoryId = Guid.NewGuid(),
       Unit = "kWh",
     IsMandatory = true,
          IsRecurring = true,
     IsActive = true
     };

   _repositoryMock
    .Setup(r => r.GetByIdForUpdateAsync(id))
  .ReturnsAsync(existingServiceType);

     _repositoryMock
      .Setup(r => r.UpdateAsync(It.IsAny<ServiceType>()))
    .ReturnsAsync((ServiceType st) => st);

            // Act
          var result = await _service.UpdateAsync(id, updateDto);

    // Assert
          Assert.IsNotNull(result);
         Assert.AreEqual("?i?n C?p Nh?t", result.Name);
            Assert.IsTrue(result.IsMandatory);
     Assert.IsTrue(result.IsRecurring);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ServiceType>()), Times.Once);
  }

    [TestMethod]
        public async Task UpdateAsync_ServiceTypeNotFound_ReturnsNull()
        {
            // Arrange
          var id = Guid.NewGuid();
            var updateDto = new UpdateServiceTypeDto
       {
      Name = "Test",
       CategoryId = Guid.NewGuid(),
     Unit = "unit",
     IsMandatory = false,
        IsRecurring = false,
        IsActive = true
 };

       _repositoryMock
    .Setup(r => r.GetByIdForUpdateAsync(id))
                .ReturnsAsync((ServiceType?)null);

   // Act
 var result = await _service.UpdateAsync(id, updateDto);

   // Assert
            Assert.IsNull(result);
   _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ServiceType>()), Times.Never);
        }

    [TestMethod]
        public async Task UpdateAsync_MandatoryButNotRecurring_ThrowsArgumentException()
   {
  // Arrange
            var id = Guid.NewGuid();
       var existingServiceType = CreateTestServiceType(id);
   var updateDto = new UpdateServiceTypeDto
  {
        Name = "Test",
 CategoryId = Guid.NewGuid(),
                Unit = "unit",
      IsMandatory = true,
                IsRecurring = false,
         IsActive = true
        };

            _repositoryMock
                .Setup(r => r.GetByIdForUpdateAsync(id))
  .ReturnsAsync(existingServiceType);

         // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
    async () => await _service.UpdateAsync(id, updateDto),
        "Mandatory service must also be Recurring.");
        }

     #endregion

 #region SoftDeleteAsync Tests

        [TestMethod]
        public async Task SoftDeleteAsync_ServiceTypeExists_ReturnsTrue()
    {
            // Arrange
        var id = Guid.NewGuid();
            var serviceType = CreateTestServiceType(id, isActive: true);

            _repositoryMock
         .Setup(r => r.GetByIdForUpdateAsync(id))
          .ReturnsAsync(serviceType);

       _repositoryMock
     .Setup(r => r.UpdateAsync(It.IsAny<ServiceType>()))
       .ReturnsAsync((ServiceType st) => st);

        // Act
var result = await _service.SoftDeleteAsync(id);

        // Assert
  Assert.IsTrue(result);
   Assert.IsTrue(serviceType.IsDelete);
            Assert.IsFalse(serviceType.IsActive);
_repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ServiceType>()), Times.Once);
        }

   [TestMethod]
        public async Task SoftDeleteAsync_ServiceTypeNotFound_ReturnsFalse()
        {
            // Arrange
        var id = Guid.NewGuid();
            _repositoryMock
         .Setup(r => r.GetByIdForUpdateAsync(id))
   .ReturnsAsync((ServiceType?)null);

            // Act
         var result = await _service.SoftDeleteAsync(id);

            // Assert
  Assert.IsFalse(result);
  _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ServiceType>()), Times.Never);
}

     [TestMethod]
     public async Task SoftDeleteAsync_AlreadyInactive_ReturnsTrue()
        {
            // Arrange
            var id = Guid.NewGuid();
   var serviceType = CreateTestServiceType(id, isActive: false);

          _repositoryMock
    .Setup(r => r.GetByIdForUpdateAsync(id))
   .ReturnsAsync(serviceType);

       _repositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<ServiceType>()))
                .ReturnsAsync((ServiceType st) => st);

            // Act
            var result = await _service.SoftDeleteAsync(id);

  // Assert
      Assert.IsTrue(result);
    }

        #endregion

     #region SetActiveAsync Tests

    [TestMethod]
      public async Task SetActiveAsync_SetToActive_Success()
  {
    // Arrange
    var id = Guid.NewGuid();
    var serviceType = CreateTestServiceType(id, isActive: false, isDelete: false);

      _repositoryMock
                .Setup(r => r.GetByIdForUpdateAsync(id))
                .ReturnsAsync(serviceType);

         _repositoryMock
         .Setup(r => r.UpdateAsync(It.IsAny<ServiceType>()))
     .ReturnsAsync((ServiceType st) => st);

            // Act
            var result = await _service.SetActiveAsync(id, true);

            // Assert
            Assert.IsTrue(result);
   Assert.IsTrue(serviceType.IsActive);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ServiceType>()), Times.Once);
        }

        [TestMethod]
 public async Task SetActiveAsync_SetToInactive_Success()
        {
        // Arrange
         var id = Guid.NewGuid();
    var serviceType = CreateTestServiceType(id, isActive: true, isDelete: false);

 _repositoryMock
                .Setup(r => r.GetByIdForUpdateAsync(id))
     .ReturnsAsync(serviceType);

            _repositoryMock
             .Setup(r => r.UpdateAsync(It.IsAny<ServiceType>()))
  .ReturnsAsync((ServiceType st) => st);

 // Act
        var result = await _service.SetActiveAsync(id, false);

          // Assert
            Assert.IsTrue(result);
            Assert.IsFalse(serviceType.IsActive);
   _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ServiceType>()), Times.Once);
        }

        [TestMethod]
        public async Task SetActiveAsync_ServiceTypeNotFound_ReturnsFalse()
        {
       // Arrange
            var id = Guid.NewGuid();
    _repositoryMock
     .Setup(r => r.GetByIdForUpdateAsync(id))
    .ReturnsAsync((ServiceType?)null);

     // Act
            var result = await _service.SetActiveAsync(id, true);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task SetActiveAsync_DeletedServiceType_ThrowsInvalidOperationException()
        {
            // Arrange
       var id = Guid.NewGuid();
            var serviceType = CreateTestServiceType(id, isActive: false, isDelete: true);

    _repositoryMock
        .Setup(r => r.GetByIdForUpdateAsync(id))
        .ReturnsAsync(serviceType);

            // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await _service.SetActiveAsync(id, true),
     "This service type was deleted and cannot be (de)activated.");
        }

        [TestMethod]
        public async Task SetActiveAsync_SameActiveStatus_ReturnsTrue()
        {
   // Arrange
            var id = Guid.NewGuid();
     var serviceType = CreateTestServiceType(id, isActive: true, isDelete: false);

            _repositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(id))
   .ReturnsAsync(serviceType);

      // Act
            var result = await _service.SetActiveAsync(id, true);

            // Assert
      Assert.IsTrue(result);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ServiceType>()), Times.Never);
        }

        #endregion

    #region ListAsync Tests

        [TestMethod]
        public async Task ListAsync_ReturnsPagedResult()
      {
     // Arrange
            var query = new ServiceTypeListQueryDto
     {
      Page = 1,
PageSize = 10
     };

     var serviceTypes = new List<ServiceType>
            {
  CreateTestServiceType(code: "ELECTRIC", name: "?i?n"),
  CreateTestServiceType(code: "WATER", name: "N??c")
            };

   _repositoryMock
     .Setup(r => r.ListAsync(query))
         .ReturnsAsync((serviceTypes, 2));

            // Act
        var result = await _service.ListAsync(query);

  // Assert
  Assert.IsNotNull(result);
      Assert.AreEqual(2, result.TotalItems);
            Assert.AreEqual(2, result.Items.Count());
            Assert.AreEqual(1, result.PageNumber);
            Assert.AreEqual(10, result.PageSize);
     }

 [TestMethod]
        public async Task ListAsync_EmptyResult_ReturnsEmptyPagedResult()
      {
   // Arrange
            var query = new ServiceTypeListQueryDto
            {
     Page = 1,
        PageSize = 10
   };

      _repositoryMock
         .Setup(r => r.ListAsync(query))
                .ReturnsAsync((new List<ServiceType>(), 0));

    // Act
      var result = await _service.ListAsync(query);

          // Assert
       Assert.IsNotNull(result);
      Assert.AreEqual(0, result.TotalItems);
          Assert.AreEqual(0, result.Items.Count());
        }

        #endregion

        #region GetAllOptionsAsync Tests

        [TestMethod]
        public async Task GetAllOptionsAsync_ReturnsAllActiveOptions()
        {
    // Arrange
  var serviceTypes = new List<ServiceType>
     {
     CreateTestServiceType(code: "ELECTRIC", name: "?i?n", isActive: true),
             CreateTestServiceType(code: "WATER", name: "N??c", isActive: true),
           CreateTestServiceType(code: "GAS", name: "Gas", isActive: false) // Inactive, should not be included
    };

            _repositoryMock
.Setup(r => r.ListAsync(It.Is<ServiceTypeListQueryDto>(q => 
         q.IsActive == true && 
           q.PageSize == int.MaxValue)))
                .ReturnsAsync((serviceTypes.Where(st => st.IsActive).ToList(), 2));

            // Act
  var result = await _service.GetAllOptionsAsync();

          // Assert
            Assert.IsNotNull(result);
    var options = result.ToList();
       Assert.AreEqual(2, options.Count);
            Assert.IsTrue(options.Any(o => o.Label == "?i?n"));
        Assert.IsTrue(options.Any(o => o.Label == "N??c"));
       Assert.IsFalse(options.Any(o => o.Label == "Gas"));
        }

 [TestMethod]
  public async Task GetAllOptionsAsync_NoActiveServices_ReturnsEmpty()
        {
            // Arrange
      _repositoryMock
                .Setup(r => r.ListAsync(It.IsAny<ServiceTypeListQueryDto>()))
         .ReturnsAsync((new List<ServiceType>(), 0));

  // Act
            var result = await _service.GetAllOptionsAsync();

            // Assert
          Assert.IsNotNull(result);
Assert.AreEqual(0, result.Count());
        }

      #endregion
    }
}
