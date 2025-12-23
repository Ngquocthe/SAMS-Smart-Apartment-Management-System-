using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SAMS_BE.DTOs;
using SAMS_BE.DTOs.Request;
using SAMS_BE.DTOs.Response;
using SAMS_BE.Interfaces.IMail;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Interfaces.IRepository.GlobalAdmin;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Models;
using SAMS_BE.Services;
using SAMS_BE.Tenant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAMS_BETests.Services
{
    [TestClass]
    public class AmenityBookingServiceTests
    {
        private Mock<IAmenityBookingRepository> _bookingRepositoryMock = null!;
        private Mock<IAmenityRepository> _amenityRepositoryMock = null!;
        private Mock<IAmenityPackageRepository> _packageRepositoryMock = null!;
        private Mock<IAmenityNotificationService> _notificationServiceMock = null!;
        private Mock<IAssetMaintenanceScheduleRepository> _scheduleRepositoryMock = null!;
        private Mock<IEmailSender> _emailSenderMock = null!;
        private Mock<IUserRepository> _userRepositoryMock = null!;
        private Mock<IBuildingRepository> _buildingRepositoryMock = null!;
        private Mock<ITenantContextAccessor> _tenantContextAccessorMock = null!;
        private Mock<BuildingManagementContext> _contextMock = null!;
        private Mock<IWebHostEnvironment> _webHostEnvironmentMock = null!;
        private Mock<ILogger<AmenityBookingService>> _loggerMock = null!;
        private AmenityBookingService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _bookingRepositoryMock = new Mock<IAmenityBookingRepository>();
            _amenityRepositoryMock = new Mock<IAmenityRepository>();
            _packageRepositoryMock = new Mock<IAmenityPackageRepository>();
            _notificationServiceMock = new Mock<IAmenityNotificationService>();
            _scheduleRepositoryMock = new Mock<IAssetMaintenanceScheduleRepository>();
            _emailSenderMock = new Mock<IEmailSender>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _buildingRepositoryMock = new Mock<IBuildingRepository>();
            _tenantContextAccessorMock = new Mock<ITenantContextAccessor>();
            _contextMock = new Mock<BuildingManagementContext>();
            _webHostEnvironmentMock = new Mock<IWebHostEnvironment>();
            _loggerMock = new Mock<ILogger<AmenityBookingService>>();

            _service = new AmenityBookingService(
                _bookingRepositoryMock.Object,
                _amenityRepositoryMock.Object,
                _packageRepositoryMock.Object,
                _notificationServiceMock.Object,
                _scheduleRepositoryMock.Object,
                _emailSenderMock.Object,
                _userRepositoryMock.Object,
                _buildingRepositoryMock.Object,
                _tenantContextAccessorMock.Object,
                _contextMock.Object,
                _webHostEnvironmentMock.Object,
                _loggerMock.Object);
        }

        #region Helper Methods

        private AmenityBooking CreateTestBooking(
            Guid? bookingId = null,
            Guid? amenityId = null,
            Guid? packageId = null,
            Guid? userId = null,
            Guid? apartmentId = null,
            string? status = "Pending",
            string? paymentStatus = "Unpaid",
            DateOnly? startDate = null,
            DateOnly? endDate = null)
        {
            var defaultStartDate = DateOnly.FromDateTime(DateTime.UtcNow);
            var defaultEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1));
            
            return new AmenityBooking
            {
                BookingId = bookingId ?? Guid.NewGuid(),
                AmenityId = amenityId ?? Guid.NewGuid(),
                PackageId = packageId ?? Guid.NewGuid(),
                ApartmentId = apartmentId ?? Guid.NewGuid(),
                UserId = userId ?? Guid.NewGuid(),
                StartDate = startDate ?? defaultStartDate,
                EndDate = endDate ?? defaultEndDate,
                Price = 100000,
                TotalPrice = 100000,
                Status = status ?? "Pending",
                PaymentStatus = paymentStatus ?? "Unpaid",
                Notes = "Test booking",
                CreatedAt = DateTime.UtcNow,
                IsDelete = false,
                Amenity = new Amenity
                {
                    AmenityId = amenityId ?? Guid.NewGuid(),
                    Name = "Swimming Pool",
                    Status = "ACTIVE",
                    FeeType = "Paid"
                },
                Package = new AmenityPackage
                {
                    PackageId = packageId ?? Guid.NewGuid(),
                    Name = "1 Month Package",
                    MonthCount = 1,
                    Price = 100000
                }
            };
        }

        private Amenity CreateTestAmenity(Guid? amenityId = null, string? status = "ACTIVE", Guid? assetId = null)
        {
            return new Amenity
            {
                AmenityId = amenityId ?? Guid.NewGuid(),
                Code = "AMENITY-001",
                Name = "Swimming Pool",
                Status = status ?? "ACTIVE",
                FeeType = "Paid",
                HasMonthlyPackage = true,
                AssetId = assetId,
                IsDelete = false
            };
        }

        private AmenityPackage CreateTestPackage(
            Guid? packageId = null, 
            Guid? amenityId = null, 
            string? status = "ACTIVE",
            string? periodUnit = "Month",
            int monthCount = 1,
            int? durationDays = null,
            int price = 100000)
        {
            return new AmenityPackage
            {
                PackageId = packageId ?? Guid.NewGuid(),
                AmenityId = amenityId ?? Guid.NewGuid(),
                Name = "1 Month Package",
                MonthCount = monthCount,
                DurationDays = durationDays,
                PeriodUnit = periodUnit,
                Price = price,
                Status = status ?? "ACTIVE"
            };
        }

        #endregion

        #region Constructor Test

        [TestMethod]
        public void AmenityBookingServiceTest()
        {
            // Arrange & Act
            var service = new AmenityBookingService(
                _bookingRepositoryMock.Object,
                _amenityRepositoryMock.Object,
                _packageRepositoryMock.Object,
                _notificationServiceMock.Object,
                _scheduleRepositoryMock.Object,
                _emailSenderMock.Object,
                _userRepositoryMock.Object,
                _buildingRepositoryMock.Object,
                _tenantContextAccessorMock.Object,
                _contextMock.Object,
                _webHostEnvironmentMock.Object,
                _loggerMock.Object);

            // Assert
            Assert.IsNotNull(service);
        }

        #endregion

        #region GetByIdAsync Tests

        [TestMethod]
        public async Task GetByIdAsync_BookingExists_ReturnsBookingDto()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var booking = CreateTestBooking(bookingId);

            _bookingRepositoryMock
                .Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync(booking);

            // Act
            var result = await _service.GetByIdAsync(bookingId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(bookingId, result.BookingId);
            _bookingRepositoryMock.Verify(r => r.GetByIdAsync(bookingId), Times.Once);
        }

        [TestMethod]
        public async Task GetByIdAsync_BookingNotFound_ReturnsNull()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            _bookingRepositoryMock
                .Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync((AmenityBooking?)null);

            // Act
            var result = await _service.GetByIdAsync(bookingId);

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region GetAllAsync Tests

        [TestMethod]
        public async Task GetAllAsync_Success_ReturnsBookingDtos()
        {
            // Arrange
            var bookings = new List<AmenityBooking>
            {
                CreateTestBooking(),
                CreateTestBooking()
            };

            _bookingRepositoryMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(bookings);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
        }

        #endregion

        #region GetPagedAsync Tests

        [TestMethod]
        public async Task GetPagedAsync_Success_ReturnsPagedResult()
        {
            // Arrange
            var query = new AmenityBookingQueryDto
            {
                PageNumber = 1,
                PageSize = 10
            };

            var bookings = new List<AmenityBooking> { CreateTestBooking() };
            var pagedResult = new PagedResult<AmenityBooking>
            {
                Items = bookings,
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 10
            };

            _bookingRepositoryMock
                .Setup(r => r.GetPagedAsync(query))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _service.GetPagedAsync(query);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.TotalCount);
            Assert.AreEqual(1, result.Items.Count());
        }

        #endregion

        #region GetByAmenityIdAsync Tests

        [TestMethod]
        public async Task GetByAmenityIdAsync_Success_ReturnsBookings()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            var bookings = new List<AmenityBooking>
            {
                CreateTestBooking(amenityId: amenityId),
                CreateTestBooking(amenityId: amenityId)
            };

            _bookingRepositoryMock
                .Setup(r => r.GetByAmenityIdAsync(amenityId))
                .ReturnsAsync(bookings);

            // Act
            var result = await _service.GetByAmenityIdAsync(amenityId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
        }

        #endregion

        #region GetByApartmentIdAsync Tests

        [TestMethod]
        public async Task GetByApartmentIdAsync_Success_ReturnsBookings()
        {
            // Arrange
            var apartmentId = Guid.NewGuid();
            var bookings = new List<AmenityBooking>
            {
                CreateTestBooking(apartmentId: apartmentId)
            };

            _bookingRepositoryMock
                .Setup(r => r.GetByApartmentIdAsync(apartmentId))
                .ReturnsAsync(bookings);

            // Act
            var result = await _service.GetByApartmentIdAsync(apartmentId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
        }

        #endregion

        #region GetByUserIdAsync Tests

        [TestMethod]
        public async Task GetByUserIdAsync_Success_ReturnsBookings()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var bookings = new List<AmenityBooking>
            {
                CreateTestBooking(userId: userId)
            };

            _bookingRepositoryMock
                .Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(bookings);

            // Act
            var result = await _service.GetByUserIdAsync(userId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
        }

        #endregion

        #region GetMyBookingsAsync Tests

        [TestMethod]
        public async Task GetMyBookingsAsync_Success_ReturnsBookings()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var bookings = new List<AmenityBooking>
            {
                CreateTestBooking(userId: userId)
            };

            _bookingRepositoryMock
                .Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(bookings);

            // Act
            var result = await _service.GetMyBookingsAsync(userId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
        }

        #endregion

        #region CreateBookingAsync Tests

        [TestMethod]
        public async Task CreateBookingAsync_Success_ReturnsBookingDto()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            var packageId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var apartmentId = Guid.NewGuid();

            var amenity = CreateTestAmenity(amenityId);
            var package = CreateTestPackage(packageId, amenityId);
            var booking = CreateTestBooking(amenityId: amenityId, packageId: packageId, userId: userId, apartmentId: apartmentId);

            var createDto = new CreateAmenityBookingDto
            {
                AmenityId = amenityId,
                PackageId = packageId
            };

            _amenityRepositoryMock
                .Setup(r => r.GetAmenityByIdAsync(amenityId))
                .ReturnsAsync(amenity);

            _packageRepositoryMock
                .Setup(r => r.GetPackageByIdAsync(packageId))
                .ReturnsAsync(package);

            _scheduleRepositoryMock
                .Setup(r => r.IsAssetUnderMaintenanceAsync(It.IsAny<Guid>()))
                .ReturnsAsync(false);

            _bookingRepositoryMock
                .Setup(r => r.GetOverlappingBookingsAsync(
                    amenityId, 
                    userId, 
                    It.IsAny<DateOnly>(), 
                    It.IsAny<DateOnly>(), 
                    It.IsAny<Guid?>()))
                .ReturnsAsync(new List<AmenityBooking>());

            _bookingRepositoryMock
                .Setup(r => r.CreateAsync(It.IsAny<AmenityBooking>()))
                .ReturnsAsync(booking);

            // Act
            var result = await _service.CreateBookingAsync(createDto, userId, apartmentId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(booking.BookingId, result.BookingId);
            _amenityRepositoryMock.Verify(r => r.GetAmenityByIdAsync(amenityId), Times.Once);
            _packageRepositoryMock.Verify(r => r.GetPackageByIdAsync(packageId), Times.Once);
        }

        [TestMethod]
        public async Task CreateBookingAsync_AmenityNotFound_ThrowsArgumentException()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            var packageId = Guid.NewGuid();
            var createDto = new CreateAmenityBookingDto
            {
                AmenityId = amenityId,
                PackageId = packageId
            };

            _amenityRepositoryMock
                .Setup(r => r.GetAmenityByIdAsync(amenityId))
                .ReturnsAsync((Amenity?)null);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await _service.CreateBookingAsync(createDto, Guid.NewGuid(), Guid.NewGuid()),
                "Amenity not found");
        }

        [TestMethod]
        public async Task CreateBookingAsync_AmenityNotActive_ThrowsInvalidOperationException()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            var amenity = CreateTestAmenity(amenityId, status: "INACTIVE");
            var createDto = new CreateAmenityBookingDto
            {
                AmenityId = amenityId,
                PackageId = Guid.NewGuid()
            };

            _amenityRepositoryMock
                .Setup(r => r.GetAmenityByIdAsync(amenityId))
                .ReturnsAsync(amenity);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await _service.CreateBookingAsync(createDto, Guid.NewGuid(), Guid.NewGuid()),
                "Amenity is not available for booking");
        }

        [TestMethod]
        public async Task CreateBookingAsync_PackageNotFound_ThrowsArgumentException()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            var packageId = Guid.NewGuid();
            var amenity = CreateTestAmenity(amenityId);
            var createDto = new CreateAmenityBookingDto
            {
                AmenityId = amenityId,
                PackageId = packageId
            };

            _amenityRepositoryMock
                .Setup(r => r.GetAmenityByIdAsync(amenityId))
                .ReturnsAsync(amenity);

            _packageRepositoryMock
                .Setup(r => r.GetPackageByIdAsync(packageId))
                .ReturnsAsync((AmenityPackage?)null);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await _service.CreateBookingAsync(createDto, Guid.NewGuid(), Guid.NewGuid()),
                "Package not found");
        }

        [TestMethod]
        public async Task CreateBookingAsync_PackageNotBelongToAmenity_ThrowsArgumentException()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            var packageId = Guid.NewGuid();
            var otherAmenityId = Guid.NewGuid();
            var amenity = CreateTestAmenity(amenityId);
            var package = CreateTestPackage(packageId, otherAmenityId);
            var createDto = new CreateAmenityBookingDto
            {
                AmenityId = amenityId,
                PackageId = packageId
            };

            _amenityRepositoryMock
                .Setup(r => r.GetAmenityByIdAsync(amenityId))
                .ReturnsAsync(amenity);

            _packageRepositoryMock
                .Setup(r => r.GetPackageByIdAsync(packageId))
                .ReturnsAsync(package);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await _service.CreateBookingAsync(createDto, Guid.NewGuid(), Guid.NewGuid()),
                "Package does not belong to the specified amenity");
        }

        [TestMethod]
        public async Task CreateBookingAsync_PackageNotActive_ThrowsInvalidOperationException()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            var packageId = Guid.NewGuid();
            var amenity = CreateTestAmenity(amenityId);
            var package = CreateTestPackage(packageId, amenityId, status: "INACTIVE");
            var createDto = new CreateAmenityBookingDto
            {
                AmenityId = amenityId,
                PackageId = packageId
            };

            _amenityRepositoryMock
                .Setup(r => r.GetAmenityByIdAsync(amenityId))
                .ReturnsAsync(amenity);

            _packageRepositoryMock
                .Setup(r => r.GetPackageByIdAsync(packageId))
                .ReturnsAsync(package);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await _service.CreateBookingAsync(createDto, Guid.NewGuid(), Guid.NewGuid()),
                "Package is not available");
        }

        [TestMethod]
        public async Task CreateBookingAsync_AmenityUnderMaintenance_ThrowsInvalidOperationException()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            var packageId = Guid.NewGuid();
            var assetId = Guid.NewGuid();
            var amenity = CreateTestAmenity(amenityId, assetId: assetId);
            var package = CreateTestPackage(packageId, amenityId);
            var createDto = new CreateAmenityBookingDto
            {
                AmenityId = amenityId,
                PackageId = packageId
            };

            _amenityRepositoryMock
                .Setup(r => r.GetAmenityByIdAsync(amenityId))
                .ReturnsAsync(amenity);

            _packageRepositoryMock
                .Setup(r => r.GetPackageByIdAsync(packageId))
                .ReturnsAsync(package);

            _scheduleRepositoryMock
                .Setup(r => r.IsAssetUnderMaintenanceAsync(assetId))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await _service.CreateBookingAsync(createDto, Guid.NewGuid(), Guid.NewGuid()),
                "Tiện ích");
        }

        [TestMethod]
        public async Task CreateBookingAsync_OverlappingBooking_ThrowsInvalidOperationException()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            var packageId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var amenity = CreateTestAmenity(amenityId);
            var package = CreateTestPackage(packageId, amenityId);
            var overlappingBooking = CreateTestBooking(amenityId: amenityId, userId: userId);
            var createDto = new CreateAmenityBookingDto
            {
                AmenityId = amenityId,
                PackageId = packageId
            };

            _amenityRepositoryMock
                .Setup(r => r.GetAmenityByIdAsync(amenityId))
                .ReturnsAsync(amenity);

            _packageRepositoryMock
                .Setup(r => r.GetPackageByIdAsync(packageId))
                .ReturnsAsync(package);

            _scheduleRepositoryMock
                .Setup(r => r.IsAssetUnderMaintenanceAsync(It.IsAny<Guid>()))
                .ReturnsAsync(false);

            _bookingRepositoryMock
                .Setup(r => r.GetOverlappingBookingsAsync(
                    amenityId, 
                    userId, 
                    It.IsAny<DateOnly>(), 
                    It.IsAny<DateOnly>(), 
                    It.IsAny<Guid?>()))
                .ReturnsAsync(new List<AmenityBooking> { overlappingBooking });

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await _service.CreateBookingAsync(createDto, userId, Guid.NewGuid()));
        }

        #endregion

        #region UpdateBookingAsync Tests

        [TestMethod]
        public async Task UpdateBookingAsync_Success_ReturnsUpdatedBookingDto()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var amenityId = Guid.NewGuid();
            var packageId = Guid.NewGuid();
            var newPackageId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var existingBooking = CreateTestBooking(bookingId, amenityId, packageId, userId, status: "Pending");
            var newPackage = CreateTestPackage(newPackageId, amenityId);
            var updateDto = new UpdateAmenityBookingDto
            {
                PackageId = newPackageId
            };

            _bookingRepositoryMock
                .Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync(existingBooking);

            _packageRepositoryMock
                .Setup(r => r.GetPackageByIdAsync(newPackageId))
                .ReturnsAsync(newPackage);

            _bookingRepositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<AmenityBooking>()))
                .ReturnsAsync(existingBooking);

            // Act
            var result = await _service.UpdateBookingAsync(bookingId, updateDto, userId);

            // Assert
            Assert.IsNotNull(result);
            _bookingRepositoryMock.Verify(r => r.GetByIdAsync(bookingId), Times.Once);
            _packageRepositoryMock.Verify(r => r.GetPackageByIdAsync(newPackageId), Times.Once);
        }

        [TestMethod]
        public async Task UpdateBookingAsync_BookingNotFound_ReturnsNull()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var updateDto = new UpdateAmenityBookingDto
            {
                PackageId = Guid.NewGuid()
            };

            _bookingRepositoryMock
                .Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync((AmenityBooking?)null);

            // Act
            var result = await _service.UpdateBookingAsync(bookingId, updateDto, Guid.NewGuid());

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task UpdateBookingAsync_StatusNotPending_ThrowsInvalidOperationException()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var existingBooking = CreateTestBooking(bookingId, status: "Confirmed");
            var updateDto = new UpdateAmenityBookingDto
            {
                PackageId = Guid.NewGuid()
            };

            _bookingRepositoryMock
                .Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync(existingBooking);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await _service.UpdateBookingAsync(bookingId, updateDto, Guid.NewGuid()),
                "Can only update pending bookings");
        }

        #endregion

        #region CancelBookingAsync Tests

        [TestMethod]
        public async Task CancelBookingAsync_Success_ReturnsTrue()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var booking = CreateTestBooking(bookingId, userId: userId, status: "Pending");

            _bookingRepositoryMock
                .Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync(booking);

            _bookingRepositoryMock
                .Setup(r => r.CancelAsync(bookingId, It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.CancelBookingAsync(bookingId, userId);

            // Assert
            Assert.IsTrue(result);
            _bookingRepositoryMock.Verify(r => r.CancelAsync(bookingId, It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task CancelBookingAsync_BookingNotFound_ReturnsFalse()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            _bookingRepositoryMock
                .Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync((AmenityBooking?)null);

            // Act
            var result = await _service.CancelBookingAsync(bookingId, Guid.NewGuid());

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task CancelBookingAsync_UnauthorizedUser_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var booking = CreateTestBooking(bookingId, userId: userId);

            _bookingRepositoryMock
                .Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync(booking);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(
                async () => await _service.CancelBookingAsync(bookingId, otherUserId),
                "You are not authorized to cancel this booking");
        }

        [TestMethod]
        public async Task CancelBookingAsync_InvalidStatus_ThrowsInvalidOperationException()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var booking = CreateTestBooking(bookingId, userId: userId, status: "Completed");

            _bookingRepositoryMock
                .Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync(booking);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await _service.CancelBookingAsync(bookingId, userId),
                "Cannot cancel booking with current status");
        }

        #endregion

        #region ConfirmBookingAsync Tests

        [TestMethod]
        public async Task ConfirmBookingAsync_Success_ReturnsTrue()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var booking = CreateTestBooking(bookingId, status: "Pending");

            _bookingRepositoryMock
                .Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync(booking);

            _bookingRepositoryMock
                .Setup(r => r.ConfirmAsync(bookingId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.ConfirmBookingAsync(bookingId, Guid.NewGuid());

            // Assert
            Assert.IsTrue(result);
            _bookingRepositoryMock.Verify(r => r.ConfirmAsync(bookingId), Times.Once);
        }

        [TestMethod]
        public async Task ConfirmBookingAsync_BookingNotFound_ReturnsFalse()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            _bookingRepositoryMock
                .Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync((AmenityBooking?)null);

            // Act
            var result = await _service.ConfirmBookingAsync(bookingId, Guid.NewGuid());

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task ConfirmBookingAsync_StatusNotPending_ThrowsInvalidOperationException()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var booking = CreateTestBooking(bookingId, status: "Confirmed");

            _bookingRepositoryMock
                .Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync(booking);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await _service.ConfirmBookingAsync(bookingId, Guid.NewGuid()),
                "Can only confirm pending bookings");
        }

        #endregion

        #region CompleteBookingAsync Tests

        [TestMethod]
        public async Task CompleteBookingAsync_Success_ReturnsTrue()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var booking = CreateTestBooking(bookingId, status: "Confirmed");

            _bookingRepositoryMock
                .Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync(booking);

            _bookingRepositoryMock
                .Setup(r => r.CompleteAsync(bookingId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.CompleteBookingAsync(bookingId, Guid.NewGuid());

            // Assert
            Assert.IsTrue(result);
            _bookingRepositoryMock.Verify(r => r.CompleteAsync(bookingId), Times.Once);
        }

        [TestMethod]
        public async Task CompleteBookingAsync_StatusNotConfirmed_ThrowsInvalidOperationException()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var booking = CreateTestBooking(bookingId, status: "Pending");

            _bookingRepositoryMock
                .Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync(booking);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await _service.CompleteBookingAsync(bookingId, Guid.NewGuid()),
                "Can only complete confirmed bookings");
        }

        #endregion

        #region UpdatePaymentStatusAsync Tests

        [TestMethod]
        public async Task UpdatePaymentStatusAsync_Success_ReturnsTrue()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var booking = CreateTestBooking(bookingId, paymentStatus: "Unpaid");

            _bookingRepositoryMock
                .Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync(booking);

            _bookingRepositoryMock
                .Setup(r => r.UpdatePaymentStatusAsync(bookingId, "Paid"))
                .ReturnsAsync(true);

            _bookingRepositoryMock
                .Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync(booking);

            // Act
            var result = await _service.UpdatePaymentStatusAsync(bookingId, "Paid", Guid.NewGuid());

            // Assert
            Assert.IsTrue(result);
            _bookingRepositoryMock.Verify(r => r.UpdatePaymentStatusAsync(bookingId, "Paid"), Times.Once);
        }

        [TestMethod]
        public async Task UpdatePaymentStatusAsync_PaidStatus_CreatesNotification()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var booking = CreateTestBooking(bookingId, paymentStatus: "Unpaid", status: "Pending");
            var confirmedBooking = CreateTestBooking(bookingId, paymentStatus: "Paid", status: "Confirmed");

            _bookingRepositoryMock
                .Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync(booking);

            _bookingRepositoryMock
                .Setup(r => r.UpdatePaymentStatusAsync(bookingId, "Paid"))
                .ReturnsAsync(true);

            _bookingRepositoryMock
                .SetupSequence(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync(booking)
                .ReturnsAsync(confirmedBooking);

            _notificationServiceMock
                .Setup(s => s.CreateBookingSuccessNotificationAsync(bookingId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdatePaymentStatusAsync(bookingId, "Paid", Guid.NewGuid());

            // Assert
            Assert.IsTrue(result);
            // Wait for delay
            await Task.Delay(1600);
            _notificationServiceMock.Verify(s => s.CreateBookingSuccessNotificationAsync(bookingId), Times.Once);
        }

        #endregion

        #region CheckAvailabilityAsync Tests

        [TestMethod]
        public async Task CheckAvailabilityAsync_AmenityAvailable_ReturnsAvailable()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            var amenity = CreateTestAmenity(amenityId);

            _amenityRepositoryMock
                .Setup(r => r.GetAmenityByIdAsync(amenityId))
                .ReturnsAsync(amenity);

            // Act
            var result = await _service.CheckAvailabilityAsync(amenityId);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsAvailable);
            Assert.AreEqual("Available", result.Message);
        }

        [TestMethod]
        public async Task CheckAvailabilityAsync_AmenityNotFound_ReturnsNotAvailable()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            _amenityRepositoryMock
                .Setup(r => r.GetAmenityByIdAsync(amenityId))
                .ReturnsAsync((Amenity?)null);

            // Act
            var result = await _service.CheckAvailabilityAsync(amenityId);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsAvailable);
            Assert.AreEqual("Amenity not found", result.Message);
        }

        [TestMethod]
        public async Task CheckAvailabilityAsync_AmenityNotActive_ReturnsNotAvailable()
        {
            // Arrange
            var amenityId = Guid.NewGuid();
            var amenity = CreateTestAmenity(amenityId, status: "INACTIVE");

            _amenityRepositoryMock
                .Setup(r => r.GetAmenityByIdAsync(amenityId))
                .ReturnsAsync(amenity);

            // Act
            var result = await _service.CheckAvailabilityAsync(amenityId);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsAvailable);
            Assert.AreEqual("Amenity is not available", result.Message);
        }

        #endregion

        #region CalculatePriceAsync Tests

        [TestMethod]
        public async Task CalculatePriceAsync_Success_MonthPackage_ReturnsPrice()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var package = CreateTestPackage(packageId, periodUnit: "Month", monthCount: 1, price: 100000);
            var request = new CalculatePriceRequest
            {
                PackageId = packageId
            };

            _packageRepositoryMock
                .Setup(r => r.GetPackageByIdAsync(packageId))
                .ReturnsAsync(package);

            // Act
            var result = await _service.CalculatePriceAsync(request);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(100000, result.BasePrice);
            Assert.AreEqual(100000, result.TotalPrice);
            Assert.IsTrue(result.Details.Contains("1 month(s)"));
        }

        [TestMethod]
        public async Task CalculatePriceAsync_Success_DayPackage_ReturnsPrice()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var package = CreateTestPackage(packageId, periodUnit: "Day", durationDays: 7, price: 50000);
            var request = new CalculatePriceRequest
            {
                PackageId = packageId
            };

            _packageRepositoryMock
                .Setup(r => r.GetPackageByIdAsync(packageId))
                .ReturnsAsync(package);

            // Act
            var result = await _service.CalculatePriceAsync(request);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(50000, result.BasePrice);
            Assert.IsTrue(result.Details.Contains("7 day(s)"));
        }

        [TestMethod]
        public async Task CalculatePriceAsync_PackageNotFound_ThrowsArgumentException()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var request = new CalculatePriceRequest
            {
                PackageId = packageId
            };

            _packageRepositoryMock
                .Setup(r => r.GetPackageByIdAsync(packageId))
                .ReturnsAsync((AmenityPackage?)null);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await _service.CalculatePriceAsync(request),
                "Package not found");
        }

        #endregion

        #region GetActiveBookingsByUserAsync Tests

        [TestMethod]
        public async Task GetActiveBookingsByUserAsync_Success_ReturnsActiveBookings()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var bookings = new List<AmenityBooking>
            {
                CreateTestBooking(userId: userId, status: "Confirmed", 
                    startDate: today.AddDays(-5), endDate: today.AddDays(25)),
                CreateTestBooking(userId: userId, status: "Pending",
                    startDate: today.AddDays(-5), endDate: today.AddDays(25))
            };

            _bookingRepositoryMock
                .Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(bookings);

            // Act
            var result = await _service.GetActiveBookingsByUserAsync(userId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual("Confirmed", result.First().Status);
        }

        [TestMethod]
        public async Task GetActiveBookingsByUserAsync_WithAmenityId_FiltersByAmenity()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var amenityId = Guid.NewGuid();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var bookings = new List<AmenityBooking>
            {
                CreateTestBooking(userId: userId, amenityId: amenityId, status: "Confirmed",
                    startDate: today.AddDays(-5), endDate: today.AddDays(25)),
                CreateTestBooking(userId: userId, amenityId: Guid.NewGuid(), status: "Confirmed",
                    startDate: today.AddDays(-5), endDate: today.AddDays(25))
            };

            _bookingRepositoryMock
                .Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(bookings);

            // Act
            var result = await _service.GetActiveBookingsByUserAsync(userId, amenityId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(amenityId, result.First().AmenityId);
        }

        #endregion
    }
}
