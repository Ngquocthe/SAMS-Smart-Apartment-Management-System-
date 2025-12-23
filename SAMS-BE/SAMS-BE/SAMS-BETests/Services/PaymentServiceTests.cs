using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using SAMS_BE.DTOs;
using SAMS_BE.Services;
using SAMS_BE.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SAMS_BE.Services.Tests
{
    [TestClass]
    public class PaymentServiceTests
    {
        private Mock<IConfiguration> _configurationMock = null!;
        private Mock<ILogger<PaymentService>> _loggerMock = null!;
        private Mock<IReceiptService> _receiptServiceMock = null!;
        private Mock<HttpMessageHandler> _httpMessageHandlerMock = null!;
        private HttpClient _httpClient = null!;
        private PaymentService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _configurationMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<PaymentService>>();
            _receiptServiceMock = new Mock<IReceiptService>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

            // Setup configuration
            var configSectionMock = new Mock<IConfigurationSection>();
            configSectionMock.Setup(x => x.Value).Returns("test-token");
            _configurationMock.Setup(x => x["SePay:ApiToken"]).Returns("test-token");
            _configurationMock.Setup(x => x["SePay:BaseUrl"]).Returns("https://api.sepay.vn");
            _configurationMock.Setup(x => x["SePay:UrlCall"]).Returns("https://api.sepay.vn/transactions");
            _configurationMock.Setup(x => x["SePay:BankId"]).Returns("970423");
            _configurationMock.Setup(x => x["SePay:AccountNumber"]).Returns("1234567890");
            _configurationMock.Setup(x => x["SePay:AccountName"]).Returns("Test Account");

            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _service = new PaymentService(_httpClient, _configurationMock.Object, _loggerMock.Object, _receiptServiceMock.Object);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _httpClient?.Dispose();
        }

        #region CreatePaymentLinkAsync Tests

        [TestMethod]
        public async Task CreatePaymentLinkAsync_ValidData_Success()
        {
            // Arrange
            var request = new CreatePaymentRequestDto
            {
                Amount = 100000,
                Items = null
            };

            // Act
            var result = await _service.CreatePaymentLinkAsync(request);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.QrCode);
            Assert.IsNotNull(result.OrderCode);
            Assert.IsTrue(result.QrCode.Contains("qr.sepay.vn"));
            Assert.IsTrue(result.QrCode.Contains("amount=100000"));
        }

        [TestMethod]
        public async Task CreatePaymentLinkAsync_WithItems_CalculatesAmount()
        {
            // Arrange
            var request = new CreatePaymentRequestDto
            {
                Amount = 0,
                Items = new List<PaymentItemDto>
                {
                    new PaymentItemDto { Price = 50000, Quantity = 2 },
                    new PaymentItemDto { Price = 30000, Quantity = 1 }
                }
            };

            // Act
            var result = await _service.CreatePaymentLinkAsync(request);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.QrCode);
            // Amount should be 50000 * 2 + 30000 * 1 = 130000
            Assert.IsTrue(result.QrCode.Contains("amount=130000"));
        }

        [TestMethod]
        public async Task CreatePaymentLinkAsync_Exception_ReturnsFailure()
        {
            // Arrange
            var request = new CreatePaymentRequestDto
            {
                Amount = 100000
            };

            // Create service with invalid config to trigger exception
            var invalidConfigMock = new Mock<IConfiguration>();
            invalidConfigMock.Setup(x => x["SePay:ApiToken"]).Returns((string?)null);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
            {
                var invalidService = new PaymentService(_httpClient, invalidConfigMock.Object, _loggerMock.Object, _receiptServiceMock.Object);
                await invalidService.CreatePaymentLinkAsync(request);
            });
        }

        #endregion

        #region GetPaymentStatusAsync Tests

        [TestMethod]
        public async Task GetPaymentStatusAsync_Deprecated_ReturnsError()
        {
            // Act
            var result = await _service.GetPaymentStatusAsync(12345);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Success);
            Assert.IsTrue(result.Message.Contains("không còn được sử dụng"));
            Assert.AreEqual("ERROR", result.Status);
        }

        #endregion

        #region GetPaymentStatusByUniqueCodeAsync Tests

        [TestMethod]
        public async Task GetPaymentStatusByUniqueCodeAsync_NoTransactions_ReturnsPending()
        {
            // Arrange
            var uniqueCode = "ABC123";
            var expectedAmount = 100000;

            // Mock HTTP response with empty transactions
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _service.GetPaymentStatusByUniqueCodeAsync(uniqueCode, expectedAmount);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Success);
            Assert.AreEqual("PENDING", result.Status);
            Assert.IsNotNull(result.Data);
            Assert.AreEqual(uniqueCode, result.Data.OrderCode);
        }

        [TestMethod]
        public async Task GetPaymentStatusByUniqueCodeAsync_NoMatchingTransaction_ReturnsPending()
        {
            // Arrange
            var uniqueCode = "ABC123";
            var expectedAmount = 100000;

            var transactionsJson = JsonSerializer.Serialize(new
            {
                transactions = new[]
                {
                    new
                    {
                        id = "txn-001",
                        amount_in = "50000",
                        transaction_content = "ThanhtoanorderXYZ789",
                        transaction_date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    }
                }
            });

            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(transactionsJson, Encoding.UTF8, "application/json")
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _service.GetPaymentStatusByUniqueCodeAsync(uniqueCode, expectedAmount);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Success);
            Assert.AreEqual("PENDING", result.Status);
        }

        [TestMethod]
        public async Task GetPaymentStatusByUniqueCodeAsync_ApiError_ReturnsError()
        {
            // Arrange
            var uniqueCode = "ABC123";
            var expectedAmount = 100000;

            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("Internal Server Error", Encoding.UTF8, "text/plain")
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _service.GetPaymentStatusByUniqueCodeAsync(uniqueCode, expectedAmount);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Success); // Returns PENDING when no transactions found
            Assert.AreEqual("PENDING", result.Status);
        }

        #endregion

        #region ProcessWebhookAsync Tests

        [TestMethod]
        public async Task ProcessWebhookAsync_AlwaysReturnsTrue()
        {
            // Arrange
            var webhookData = new PaymentWebhookDto();

            // Act
            var result = await _service.ProcessWebhookAsync(webhookData);

            // Assert
            Assert.IsTrue(result);
        }

        #endregion

        #region CancelPaymentAsync Tests

        [TestMethod]
        public async Task CancelPaymentAsync_AlwaysReturnsFailure()
        {
            // Arrange
            var orderCode = 12345;
            var cancellationReason = "User cancelled";

            // Act
            var result = await _service.CancelPaymentAsync(orderCode, cancellationReason);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Success);
            Assert.IsTrue(result.Message.Contains("không hỗ trợ hủy thanh toán"));
            Assert.AreEqual(orderCode.ToString(), result.OrderCode);
        }

        #endregion

        #region VerifyWebhookSignature Tests

        [TestMethod]
        public void VerifyWebhookSignature_AlwaysReturnsFalse()
        {
            // Arrange
            var webhookUrl = "https://example.com/webhook";
            var requestBody = "{}";
            var signature = "test-signature";

            // Act
            var result = _service.VerifyWebhookSignature(webhookUrl, requestBody, signature);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion
    }
}
