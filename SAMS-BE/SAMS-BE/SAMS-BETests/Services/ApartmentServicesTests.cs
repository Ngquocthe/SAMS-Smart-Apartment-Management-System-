using AutoMapper;
using Microsoft.EntityFrameworkCore;
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

namespace SAMS_BE.Services.Tests
{
    [TestClass]
    public class ApartmentServicesTests
    {
        private Mock<IApartmentRepository> _apartmentRepositoryMock = null!;
        private Mock<IFloorRepository> _floorRepositoryMock = null!;
        private Mock<IMapper> _mapperMock = null!;
        private ApartmentServices _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _apartmentRepositoryMock = new Mock<IApartmentRepository>();
            _floorRepositoryMock = new Mock<IFloorRepository>();
            _mapperMock = new Mock<IMapper>();
            _service = new ApartmentServices(_apartmentRepositoryMock.Object, _floorRepositoryMock.Object, _mapperMock.Object);
        }

        #region Helper Methods

        private Floor CreateTestFloor(int floorNumber = 8, string? name = null, Guid? floorId = null)
        {
            return new Floor
            {
                FloorId = floorId ?? Guid.NewGuid(),
                FloorNumber = floorNumber,
                Name = name ?? $"Tầng {floorNumber}",
                 
            };
        }

        private Apartment CreateTestApartment(Guid? apartmentId = null, string? number = null, Guid? floorId = null)
        {
            return new Apartment
            {
                ApartmentId = apartmentId ?? Guid.NewGuid(),
                Number = number ?? "A0801",
                FloorId = floorId ?? Guid.NewGuid(),
                AreaM2 = 50.5m,
                Bedrooms = 2,
                Status = "ACTIVE",
                Type = "2BR",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };
        }

        private ApartmentResponseDto CreateTestApartmentResponseDto(Guid? apartmentId = null, string? number = null)
        {
            return new ApartmentResponseDto
            {
                ApartmentId = apartmentId ?? Guid.NewGuid(),
                Number = number ?? "A0801",
                AreaM2 = 50.5m,
                Bedrooms = 2,
                Status = "ACTIVE",
                Type = "2BR",
                FloorNumber = 8,
                FloorName = "Tầng 8"
            };
        }

        #endregion

        #region GetAllApartmentsAsync Tests

        [TestMethod]
        public async Task GetAllApartmentsAsync_Success_ReturnsList()
        {
            // Arrange
            var apartments = new List<Apartment>
            {
                CreateTestApartment(number: "A0801"),
                CreateTestApartment(number: "A0802")
            };

            _apartmentRepositoryMock
                .Setup(r => r.GetAllApartmentsAsync())
                .ReturnsAsync(apartments);

            _mapperMock
                .Setup(m => m.Map<ApartmentResponseDto>(It.IsAny<Apartment>()))
                .Returns((Apartment a) => CreateTestApartmentResponseDto(a.ApartmentId, a.Number));

            // Act
            var result = await _service.GetAllApartmentsAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            _apartmentRepositoryMock.Verify(r => r.GetAllApartmentsAsync(), Times.Once);
        }

        [TestMethod]
        public async Task GetAllApartmentsAsync_Exception_ThrowsException()
        {
            // Arrange
            var exception = new Exception("Database error");
            _apartmentRepositoryMock
                .Setup(r => r.GetAllApartmentsAsync())
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.GetAllApartmentsAsync());
        }

        #endregion

        #region GetApartmentByIdAsync Tests

        [TestMethod]
        public async Task GetApartmentByIdAsync_ApartmentExists_ReturnsApartment()
        {
            // Arrange
            var apartmentId = Guid.NewGuid();
            var apartment = CreateTestApartment(apartmentId);

            _apartmentRepositoryMock
                .Setup(r => r.GetApartmentByIdAsync(apartmentId))
                .ReturnsAsync(apartment);

            _mapperMock
                .Setup(m => m.Map<ApartmentResponseDto>(apartment))
                .Returns(CreateTestApartmentResponseDto(apartmentId));

            // Act
            var result = await _service.GetApartmentByIdAsync(apartmentId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(apartmentId, result.ApartmentId);
            _apartmentRepositoryMock.Verify(r => r.GetApartmentByIdAsync(apartmentId), Times.Once);
        }

        [TestMethod]
        public async Task GetApartmentByIdAsync_ApartmentNotFound_ReturnsNull()
        {
            // Arrange
            var apartmentId = Guid.NewGuid();
            _apartmentRepositoryMock
                .Setup(r => r.GetApartmentByIdAsync(apartmentId))
                .ReturnsAsync((Apartment?)null);

            // Act
            var result = await _service.GetApartmentByIdAsync(apartmentId);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetApartmentByIdAsync_Exception_ThrowsException()
        {
            // Arrange
            var apartmentId = Guid.NewGuid();
            var exception = new Exception("Database error");
            _apartmentRepositoryMock
                .Setup(r => r.GetApartmentByIdAsync(apartmentId))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.GetApartmentByIdAsync(apartmentId));
        }

        #endregion

        #region GetApartmentByNumberAsync Tests

        [TestMethod]
        public async Task GetApartmentByNumberAsync_ApartmentExists_ReturnsApartment()
        {
            // Arrange
            var apartmentNumber = "A0801";
            var apartment = CreateTestApartment(number: apartmentNumber);

            _apartmentRepositoryMock
                .Setup(r => r.GetApartmentByNumberAsync(apartmentNumber))
                .ReturnsAsync(apartment);

            _mapperMock
                .Setup(m => m.Map<ApartmentResponseDto>(apartment))
                .Returns(CreateTestApartmentResponseDto(apartment.ApartmentId, apartmentNumber));

            // Act
            var result = await _service.GetApartmentByNumberAsync(apartmentNumber);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(apartmentNumber, result.Number);
            _apartmentRepositoryMock.Verify(r => r.GetApartmentByNumberAsync(apartmentNumber), Times.Once);
        }

        [TestMethod]
        public async Task GetApartmentByNumberAsync_ApartmentNotFound_ReturnsNull()
        {
            // Arrange
            var apartmentNumber = "A9999";
            _apartmentRepositoryMock
                .Setup(r => r.GetApartmentByNumberAsync(apartmentNumber))
                .ReturnsAsync((Apartment?)null);

            // Act
            var result = await _service.GetApartmentByNumberAsync(apartmentNumber);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetApartmentByNumberAsync_Exception_ThrowsException()
        {
            // Arrange
            var apartmentNumber = "A0801";
            var exception = new Exception("Database error");
            _apartmentRepositoryMock
                .Setup(r => r.GetApartmentByNumberAsync(apartmentNumber))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.GetApartmentByNumberAsync(apartmentNumber));
        }

        #endregion

        #region GetApartmentsByFloorAsync Tests

        [TestMethod]
        public async Task GetApartmentsByFloorAsync_Success_ReturnsList()
        {
            // Arrange
            var floorNumber = 8;
            var apartments = new List<Apartment>
            {
                CreateTestApartment(number: "A0801"),
                CreateTestApartment(number: "A0802")
            };

            _apartmentRepositoryMock
                .Setup(r => r.GetApartmentsByFloorNumberAsync(floorNumber))
                .ReturnsAsync(apartments);

            _mapperMock
                .Setup(m => m.Map<ApartmentResponseDto>(It.IsAny<Apartment>()))
                .Returns((Apartment a) => CreateTestApartmentResponseDto(a.ApartmentId, a.Number));

            // Act
            var result = await _service.GetApartmentsByFloorAsync(floorNumber);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            _apartmentRepositoryMock.Verify(r => r.GetApartmentsByFloorNumberAsync(floorNumber), Times.Once);
        }

        [TestMethod]
        public async Task GetApartmentsByFloorAsync_Exception_ThrowsException()
        {
            // Arrange
            var floorNumber = 8;
            var exception = new Exception("Database error");
            _apartmentRepositoryMock
                .Setup(r => r.GetApartmentsByFloorNumberAsync(floorNumber))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.GetApartmentsByFloorAsync(floorNumber));
        }

        #endregion

        #region CreateSingleApartmentAsync Tests

        [TestMethod]
        public async Task CreateSingleApartmentAsync_ValidData_Success()
        {
            // Arrange
            var floorNumber = 8;
            var floor = CreateTestFloor(floorNumber);
            var request = new CreateSingleApartmentRequestDto
            {
                BuildingCode = "A",
                FloorNumber = floorNumber,
                ApartmentNumber = "01",
                AreaM2 = 50.5m,
                Bedrooms = 2,
                Status = "ACTIVE",
                Type = "2BR"
            };

            _apartmentRepositoryMock
                .Setup(r => r.GetFloorByNumberAsync(floorNumber))
                .ReturnsAsync(floor);

            _apartmentRepositoryMock
                .Setup(r => r.ApartmentNumberExistsOnFloorAsync(It.IsAny<string>(), floor.FloorId))
                .ReturnsAsync(false);

            _apartmentRepositoryMock
                .Setup(r => r.CreateApartmentsAsync(It.IsAny<List<Apartment>>()))
                .ReturnsAsync((List<Apartment> a) => a);

            _mapperMock
                .Setup(m => m.Map<ApartmentResponseDto>(It.IsAny<Apartment>()))
                .Returns((Apartment a) => new ApartmentResponseDto
                {
                    ApartmentId = a.ApartmentId,
                    Number = a.Number,
                    AreaM2 = a.AreaM2,
                    Bedrooms = a.Bedrooms,
                    Status = a.Status,
                    Type = a.Type,
                    FloorNumber = floor.FloorNumber,
                    FloorName = floor.Name
                });

            // Act
            var result = await _service.CreateSingleApartmentAsync(request);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("A0801", result.Number);
            Assert.AreEqual(floorNumber, result.FloorNumber);
            _apartmentRepositoryMock.Verify(r => r.GetFloorByNumberAsync(floorNumber), Times.Once);
            _apartmentRepositoryMock.Verify(r => r.CreateApartmentsAsync(It.IsAny<List<Apartment>>()), Times.Once);
        }

        [TestMethod]
        public async Task CreateSingleApartmentAsync_FloorNotFound_ThrowsException()
        {
            // Arrange
            var request = new CreateSingleApartmentRequestDto
            {
                BuildingCode = "A",
                FloorNumber = 99,
                ApartmentNumber = "01"
            };

            _apartmentRepositoryMock
                .Setup(r => r.GetFloorByNumberAsync(99))
                .ReturnsAsync((Floor?)null);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.CreateSingleApartmentAsync(request));
        }

        [TestMethod]
        public async Task CreateSingleApartmentAsync_ApartmentExists_ThrowsException()
        {
            // Arrange
            var floorNumber = 8;
            var floor = CreateTestFloor(floorNumber);
            var request = new CreateSingleApartmentRequestDto
            {
                BuildingCode = "A",
                FloorNumber = floorNumber,
                ApartmentNumber = "01"
            };

            _apartmentRepositoryMock
                .Setup(r => r.GetFloorByNumberAsync(floorNumber))
                .ReturnsAsync(floor);

            _apartmentRepositoryMock
                .Setup(r => r.ApartmentNumberExistsOnFloorAsync(It.IsAny<string>(), floor.FloorId))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.CreateSingleApartmentAsync(request));
        }

        #endregion

        #region UpdateApartmentAsync Tests

        [TestMethod]
        public async Task UpdateApartmentAsync_ApartmentExists_Success()
        {
            // Arrange
            var apartmentId = Guid.NewGuid();
            var existingApartment = CreateTestApartment(apartmentId);
            var updateDto = new CreateApartmentDto
            {
                AreaM2 = 60.0m,
                Bedrooms = 3,
                Status = "INACTIVE",
                Type = "3BR"
            };

            _apartmentRepositoryMock
                .Setup(r => r.GetApartmentByIdAsync(apartmentId))
                .ReturnsAsync(existingApartment);

            _apartmentRepositoryMock
                .Setup(r => r.UpdateApartmentAsync(It.IsAny<Apartment>()))
                .ReturnsAsync((Apartment a) => a);

            _mapperMock
                .Setup(m => m.Map<ApartmentResponseDto>(It.IsAny<Apartment>()))
                .Returns((Apartment a) => CreateTestApartmentResponseDto(a.ApartmentId, a.Number));

            // Act
            var result = await _service.UpdateApartmentAsync(apartmentId, updateDto);

            // Assert
            Assert.IsNotNull(result);
            _apartmentRepositoryMock.Verify(r => r.GetApartmentByIdAsync(apartmentId), Times.Once);
            _apartmentRepositoryMock.Verify(r => r.UpdateApartmentAsync(It.IsAny<Apartment>()), Times.Once);
        }

        [TestMethod]
        public async Task UpdateApartmentAsync_ApartmentNotFound_ThrowsException()
        {
            // Arrange
            var apartmentId = Guid.NewGuid();
            var updateDto = new CreateApartmentDto();

            _apartmentRepositoryMock
                .Setup(r => r.GetApartmentByIdAsync(apartmentId))
                .ReturnsAsync((Apartment?)null);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.UpdateApartmentAsync(apartmentId, updateDto));
        }

        [TestMethod]
        public async Task UpdateApartmentAsync_Exception_ThrowsException()
        {
            // Arrange
            var apartmentId = Guid.NewGuid();
            var updateDto = new CreateApartmentDto();
            var exception = new Exception("Database error");

            _apartmentRepositoryMock
                .Setup(r => r.GetApartmentByIdAsync(apartmentId))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.UpdateApartmentAsync(apartmentId, updateDto));
        }

        #endregion

        #region DeleteApartmentAsync Tests

        [TestMethod]
        public async Task DeleteApartmentAsync_Success_ReturnsTrue()
        {
            // Arrange
            var apartmentId = Guid.NewGuid();
            _apartmentRepositoryMock
                .Setup(r => r.DeleteApartmentAsync(apartmentId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteApartmentAsync(apartmentId);

            // Assert
            Assert.IsTrue(result);
            _apartmentRepositoryMock.Verify(r => r.DeleteApartmentAsync(apartmentId), Times.Once);
        }

        [TestMethod]
        public async Task DeleteApartmentAsync_Failure_ReturnsFalse()
        {
            // Arrange
            var apartmentId = Guid.NewGuid();
            _apartmentRepositoryMock
                .Setup(r => r.DeleteApartmentAsync(apartmentId))
                .ReturnsAsync(false);

            // Act
            var result = await _service.DeleteApartmentAsync(apartmentId);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task DeleteApartmentAsync_Exception_ThrowsException()
        {
            // Arrange
            var apartmentId = Guid.NewGuid();
            var exception = new Exception("Database error");
            _apartmentRepositoryMock
                .Setup(r => r.DeleteApartmentAsync(apartmentId))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(async () => await _service.DeleteApartmentAsync(apartmentId));
        }

        #endregion

        #region CreateApartmentsAsync Tests

        [TestMethod]
        public async Task CreateApartmentsAsync_ValidData_Success()
        {
            // Arrange
            var floorNumber = 8;
            var floor = CreateTestFloor(floorNumber);
            var request = new CreateApartmentsRequestDto
            {
                BuildingCode = "A",
                SourceFloorNumber = floorNumber,
                Apartments = new List<CreateApartmentDto>
                {
                    new CreateApartmentDto { Number = "01", AreaM2 = 50.5m, Bedrooms = 2, Status = "ACTIVE", Type = "2BR" },
                    new CreateApartmentDto { Number = "02", AreaM2 = 60.0m, Bedrooms = 3, Status = "ACTIVE", Type = "3BR" }
                }
            };

            _apartmentRepositoryMock
                .Setup(r => r.GetFloorByNumberAsync(floorNumber))
                .ReturnsAsync(floor);

            _apartmentRepositoryMock
                .Setup(r => r.ApartmentNumberExistsOnFloorAsync(It.IsAny<string>(), floor.FloorId))
                .ReturnsAsync(false);

            _apartmentRepositoryMock
                .Setup(r => r.CreateApartmentsAsync(It.IsAny<List<Apartment>>()))
                .ReturnsAsync((List<Apartment> a) => a);

            _mapperMock
                .Setup(m => m.Map<Apartment>(It.IsAny<CreateApartmentDto>()))
                .Returns((CreateApartmentDto dto) => new Apartment
                {
                    ApartmentId = Guid.NewGuid(),
                    Number = dto.Number,
                    AreaM2 = dto.AreaM2,
                    Bedrooms = dto.Bedrooms,
                    Status = dto.Status,
                    Type = dto.Type
                });

            _mapperMock
                .Setup(m => m.Map<ApartmentResponseDto>(It.IsAny<Apartment>()))
                .Returns((Apartment a) => new ApartmentResponseDto
                {
                    ApartmentId = a.ApartmentId,
                    Number = a.Number,
                    AreaM2 = a.AreaM2,
                    Bedrooms = a.Bedrooms,
                    Status = a.Status,
                    Type = a.Type,
                    FloorNumber = floor.FloorNumber,
                    FloorName = floor.Name
                });

            // Act
            var result = await _service.CreateApartmentsAsync(request);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(2, result.TotalCreated);
            _apartmentRepositoryMock.Verify(r => r.GetFloorByNumberAsync(floorNumber), Times.Once);
            _apartmentRepositoryMock.Verify(r => r.CreateApartmentsAsync(It.IsAny<List<Apartment>>()), Times.Once);
        }

        [TestMethod]
        public async Task CreateApartmentsAsync_FloorNotFound_ReturnsFailure()
        {
            // Arrange
            var request = new CreateApartmentsRequestDto
            {
                BuildingCode = "A",
                SourceFloorNumber = 99,
                Apartments = new List<CreateApartmentDto>
                {
                    new CreateApartmentDto { Number = "01" }
                }
            };

            _apartmentRepositoryMock
                .Setup(r => r.GetFloorByNumberAsync(99))
                .ReturnsAsync((Floor?)null);

            // Act
            var result = await _service.CreateApartmentsAsync(request);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Success);
            Assert.AreEqual(0, result.TotalCreated);
            Assert.IsTrue(result.Message.Contains("Không tìm thấy tầng"));
        }

        [TestMethod]
        public async Task CreateApartmentsAsync_AllApartmentsExist_ReturnsFailure()
        {
            // Arrange
            var floorNumber = 8;
            var floor = CreateTestFloor(floorNumber);
            var request = new CreateApartmentsRequestDto
            {
                BuildingCode = "A",
                SourceFloorNumber = floorNumber,
                Apartments = new List<CreateApartmentDto>
                {
                    new CreateApartmentDto { Number = "01" }
                }
            };

            _apartmentRepositoryMock
                .Setup(r => r.GetFloorByNumberAsync(floorNumber))
                .ReturnsAsync(floor);

            _apartmentRepositoryMock
                .Setup(r => r.ApartmentNumberExistsOnFloorAsync(It.IsAny<string>(), floor.FloorId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.CreateApartmentsAsync(request);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Success);
            Assert.AreEqual(0, result.TotalCreated);
            Assert.IsTrue(result.Message.Contains("Không có apartment nào được tạo"));
        }

        #endregion
    }
}
