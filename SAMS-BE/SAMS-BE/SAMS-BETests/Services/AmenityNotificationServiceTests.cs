using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Models;
using SAMS_BE.Services;
using SAMS_BE.Tenant;
using System;
using System.Threading.Tasks;

namespace SAMS_BETests.Services
{
    [TestClass]
    public class AmenityNotificationServiceTests
    {
        private Mock<IAmenityBookingRepository> _bookingRepositoryMock = null!;
        private Mock<IAnnouncementRepository> _announcementRepositoryMock = null!;
        private Mock<ITenantContextAccessor> _tenantContextAccessorMock = null!;
        private Mock<ILogger<AmenityNotificationService>> _loggerMock = null!;
        private AmenityNotificationService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _bookingRepositoryMock = new Mock<IAmenityBookingRepository>();
            _announcementRepositoryMock = new Mock<IAnnouncementRepository>();
            _tenantContextAccessorMock = new Mock<ITenantContextAccessor>();
            _loggerMock = new Mock<ILogger<AmenityNotificationService>>();

            _service = new AmenityNotificationService(
                _bookingRepositoryMock.Object,
                _announcementRepositoryMock.Object,
                _tenantContextAccessorMock.Object,
                _loggerMock.Object);
        }

        #region Helper Methods

        private AmenityBooking CreateTestBooking(
            Guid? bookingId = null,
            Guid? amenityId = null,
            Guid? userId = null,
            string? paymentStatus = "Paid",
            string? status = "Confirmed",
            string? amenityName = "Swimming Pool")
        {
            var booking = new AmenityBooking
            {
                BookingId = bookingId ?? Guid.NewGuid(),
                AmenityId = amenityId ?? Guid.NewGuid(),
                PackageId = Guid.NewGuid(),
                ApartmentId = Guid.NewGuid(),
                UserId = userId ?? Guid.NewGuid(),
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
                Price = 100000,
                TotalPrice = 100000,
                Status = status ?? "Confirmed",
                PaymentStatus = paymentStatus ?? "Paid",
                Notes = "Test booking",
                CreatedAt = DateTime.UtcNow,
                IsDelete = false,
                Amenity = new Amenity
                {
                    AmenityId = amenityId ?? Guid.NewGuid(),
                    Name = amenityName ?? "Swimming Pool",
                    Code = "AMENITY-001",
                    Status = "ACTIVE",
                    FeeType = "Paid"
                }
            };

            return booking;
        }

        #endregion

        #region Constructor Test

        [TestMethod]
        public void AmenityNotificationServiceTest()
        {
            // Arrange & Act
            var service = new AmenityNotificationService(
                _bookingRepositoryMock.Object,
                _announcementRepositoryMock.Object,
                _tenantContextAccessorMock.Object,
                _loggerMock.Object);

            // Assert
            Assert.IsNotNull(service);
        }

        #endregion

        #region CreateBookingSuccessNotificationAsync Tests

        [TestMethod]
        public async Task CreateBookingSuccessNotificationAsync_AmenityNameIsNull_UsesDefaultName()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var booking = CreateTestBooking(bookingId, paymentStatus: "Paid", status: "Confirmed", amenityName: "Swimming Pool");
            booking.Amenity = null; // Simulate null amenity

            _tenantContextAccessorMock
                .Setup(t => t.SetSchema("building"))
                .Verifiable();

            _bookingRepositoryMock
                .Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync(booking);

            _announcementRepositoryMock
                .Setup(r => r.ExistsAnnouncementByBookingIdAndTypeAsync(bookingId, "AMENITY_BOOKING_SUCCESS"))
                .ReturnsAsync(false);

            _announcementRepositoryMock
                .Setup(r => r.CreateAnnouncementAsync(It.IsAny<Announcement>()))
                .ReturnsAsync((Announcement a) => a);

            // Act
            await _service.CreateBookingSuccessNotificationAsync(bookingId);

            // Assert
            _announcementRepositoryMock.Verify(
                r => r.CreateAnnouncementAsync(It.Is<Announcement>(a =>
                    a.Content.Contains("Tiện ích"))),
                Times.Once);
        }

        [TestMethod]
        public async Task CreateBookingSuccessNotificationAsync_SetsCorrectDates()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
            var endDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1));
            var booking = CreateTestBooking(bookingId, paymentStatus: "Paid", status: "Confirmed");
            booking.StartDate = startDate;
            booking.EndDate = endDate;

            _tenantContextAccessorMock
                .Setup(t => t.SetSchema("building"))
                .Verifiable();

            _bookingRepositoryMock
                .Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync(booking);

            _announcementRepositoryMock
                .Setup(r => r.ExistsAnnouncementByBookingIdAndTypeAsync(bookingId, "AMENITY_BOOKING_SUCCESS"))
                .ReturnsAsync(false);

            _announcementRepositoryMock
                .Setup(r => r.CreateAnnouncementAsync(It.IsAny<Announcement>()))
                .ReturnsAsync((Announcement a) => a);

            // Act
            await _service.CreateBookingSuccessNotificationAsync(bookingId);

            // Assert
            _announcementRepositoryMock.Verify(
                r => r.CreateAnnouncementAsync(It.Is<Announcement>(a =>
                    a.Content.Contains(startDate.ToString("dd/MM/yyyy")) &&
                    a.Content.Contains(endDate.ToString("dd/MM/yyyy")))),
                Times.Once);
        }

        [TestMethod]
        public async Task CreateBookingSuccessNotificationAsync_SetsCorrectVisibilityDates()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var booking = CreateTestBooking(bookingId, paymentStatus: "Paid", status: "Confirmed");

            _tenantContextAccessorMock
                .Setup(t => t.SetSchema("building"))
                .Verifiable();

            _bookingRepositoryMock
                .Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync(booking);

            _announcementRepositoryMock
                .Setup(r => r.ExistsAnnouncementByBookingIdAndTypeAsync(bookingId, "AMENITY_BOOKING_SUCCESS"))
                .ReturnsAsync(false);

            _announcementRepositoryMock
                .Setup(r => r.CreateAnnouncementAsync(It.IsAny<Announcement>()))
                .ReturnsAsync((Announcement a) => a);

            // Act
            await _service.CreateBookingSuccessNotificationAsync(bookingId);

            // Assert
            _announcementRepositoryMock.Verify(
                r => r.CreateAnnouncementAsync(It.Is<Announcement>(a =>
                    a.VisibleFrom.Date == DateTime.UtcNow.AddHours(7).Date &&
                    a.VisibleTo.HasValue &&
                    a.VisibleTo.Value.Date == DateTime.UtcNow.AddHours(7).Date.AddDays(1).AddSeconds(-1).Date)),
                Times.Once);
        }

        [TestMethod]
        public async Task CreateBookingSuccessNotificationAsync_SetsCorrectCreatedBy()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var booking = CreateTestBooking(bookingId, userId: userId, paymentStatus: "Paid", status: "Confirmed");

            _tenantContextAccessorMock
                .Setup(t => t.SetSchema("building"))
                .Verifiable();

            _bookingRepositoryMock
                .Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync(booking);

            _announcementRepositoryMock
                .Setup(r => r.ExistsAnnouncementByBookingIdAndTypeAsync(bookingId, "AMENITY_BOOKING_SUCCESS"))
                .ReturnsAsync(false);

            _announcementRepositoryMock
                .Setup(r => r.CreateAnnouncementAsync(It.IsAny<Announcement>()))
                .ReturnsAsync((Announcement a) => a);

            // Act
            await _service.CreateBookingSuccessNotificationAsync(bookingId);

            // Assert
            _announcementRepositoryMock.Verify(
                r => r.CreateAnnouncementAsync(It.Is<Announcement>(a =>
                    a.CreatedBy == userId.ToString())),
                Times.Once);
        }

        [TestMethod]
        public async Task CreateBookingSuccessNotificationAsync_CreateAnnouncementException_ThrowsAndLogs()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var booking = CreateTestBooking(bookingId, paymentStatus: "Paid", status: "Confirmed");
            var exception = new Exception("Database error");

            _tenantContextAccessorMock
                .Setup(t => t.SetSchema("building"))
                .Verifiable();

            _bookingRepositoryMock
                .Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync(booking);

            _announcementRepositoryMock
                .Setup(r => r.ExistsAnnouncementByBookingIdAndTypeAsync(bookingId, "AMENITY_BOOKING_SUCCESS"))
                .ReturnsAsync(false);

            _announcementRepositoryMock
                .Setup(r => r.CreateAnnouncementAsync(It.IsAny<Announcement>()))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(
                async () => await _service.CreateBookingSuccessNotificationAsync(bookingId));

            _announcementRepositoryMock.Verify(
                r => r.CreateAnnouncementAsync(It.IsAny<Announcement>()),
                Times.Once);
        }

        #endregion
    }
}
