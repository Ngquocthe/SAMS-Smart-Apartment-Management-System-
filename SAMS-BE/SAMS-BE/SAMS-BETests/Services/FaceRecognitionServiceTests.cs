using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SAMS_BE.DTOs;
using SAMS_BE.Models;
using SAMS_BE.Services;
using SAMS_BE.Helpers;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace SAMS_BE.Services.Tests
{
    [TestClass]
    public class FaceRecognitionServiceTests
    {
        private BuildingManagementContext _db = null!;
        private Mock<IFileStorageHelper> _fileStorageMock = null!;
        private Mock<ILogger<FaceRecognitionService>> _loggerMock = null!;

        // Fake service override GetEmbedding để không cần ONNX
        private class TestableFaceRecognitionService : FaceRecognitionService
        {
            private readonly float[] _fixedEmbedding;

            public TestableFaceRecognitionService(
                BuildingManagementContext context,
                IFileStorageHelper fileStorage,
                ILogger<FaceRecognitionService> logger,
                float[] fixedEmbedding)
                : base(context, fileStorage, logger)
            {
                _fixedEmbedding = fixedEmbedding;
            }

            public override float[] GetEmbedding(string imagePath) => _fixedEmbedding;
        }

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<BuildingManagementContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _db = new BuildingManagementContext(options, new Tenant.TenantContextAccessor());
            _fileStorageMock = new Mock<IFileStorageHelper>();
            _loggerMock = new Mock<ILogger<FaceRecognitionService>>();
        }

        private static IFormFile CreateFakeImage(string name = "face.jpg")
        {
            var bytes = Encoding.UTF8.GetBytes("fake-image");
            var stream = new MemoryStream(bytes);
            return new FormFile(stream, 0, bytes.Length, "file", name)
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/jpeg"
            };
        }

        #region VerifyFaceAsync

        // UTCID01: User không tồn tại -> trả về NotVerified
        // Precondition: Database trống, không có User
        // Input: UserId = GUID random, Image = fake image (hợp lệ)
        // Expected: IsVerified = false, Message = "User không tồn tại"
        // Exception: None (normal flow)
        // Result Type: N (Normal)
        // Log message: None
        [TestMethod]
        public async Task VerifyFaceAsync_UserNotFound_ReturnsNotVerified()
        {
            // Arrange
            var embedding = new float[] { 0.1f, 0.2f, 0.3f };
            var service = new TestableFaceRecognitionService(_db, _fileStorageMock.Object, _loggerMock.Object, embedding);

            var dto = new FaceVerifyRequestDto
            {
                UserId = Guid.NewGuid(),
                Image = CreateFakeImage()
            };

            // Act
            var result = await service.VerifyFaceAsync(dto);

            // Assert
            Assert.IsFalse(result.IsVerified);
            Assert.AreEqual("User không tồn tại", result.Message);
        }

        // UTCID01B: User tồn tại nhưng chưa đăng ký khuôn mặt -> NotVerified
        // Precondition: User tồn tại, FaceEmbedding = null
        // Input: UserId = GUID tồn tại, Image = fake image (hợp lệ)
        // Expected: IsVerified = false, Message = "User chưa đăng ký khuôn mặt"
        // Exception: None
        // Result Type: N (Normal)
        // Log message: None
        [TestMethod]
        public async Task VerifyFaceAsync_UserHasNoEmbedding_ReturnsNotVerified()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                UserId = userId,
                FirstName = "Test",
                LastName = "User",
                Username = "testuser",
                Email = "test@example.com",
                Phone = "0123456789",
                FaceEmbedding = null
            };
            await _db.Users.AddAsync(user);
            await _db.SaveChangesAsync();

            var embedding = new float[] { 0.1f, 0.2f, 0.3f };
            var service = new TestableFaceRecognitionService(_db, _fileStorageMock.Object, _loggerMock.Object, embedding);

            var dto = new FaceVerifyRequestDto
            {
                UserId = userId,
                Image = CreateFakeImage()
            };

            // Act
            var result = await service.VerifyFaceAsync(dto);

            // Assert
            Assert.IsFalse(result.IsVerified);
            Assert.AreEqual("User chưa đăng ký khuôn mặt", result.Message);
        }

        // UTCID02: Có embedding, giống nhau vượt ngưỡng -> Verified
        // Precondition: User tồn tại với FaceEmbedding, threshold mặc định trong service
        // Input: UserId = GUID tồn tại, Image = fake image (hợp lệ), live embedding = db embedding
        // Expected: IsVerified = true, Similarity > 0.9, Message = "Xác thực thành công"
        // Exception: None (Success)
        // Result Type: N (Normal)
        // Log message: None
        [TestMethod]
        public async Task VerifyFaceAsync_HasEmbedding_AboveThreshold_ReturnsVerified()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var embedding = new float[] { 1f, 0f, 0f }; // live
            var dbEmbedding = new float[] { 1f, 0f, 0f }; // giống hệt -> cosine = 1

            var user = new User
            {
                UserId = userId,
                FirstName = "Test",            // bắt buộc
                LastName = "User",            // bắt buộc
                Username = "testuser",        // bắt buộc
                Email = "test@example.com",// bắt buộc
                Phone = "0123456789",      // bắt buộc
                FaceEmbedding = Encoding.UTF8.GetBytes(
        JsonSerializer.Serialize(dbEmbedding)
    )
            };

            await _db.Users.AddAsync(user);
            await _db.SaveChangesAsync();

            var service = new TestableFaceRecognitionService(_db, _fileStorageMock.Object, _loggerMock.Object, embedding);

            var dto = new FaceVerifyRequestDto
            {
                UserId = userId,
                Image = CreateFakeImage()
            };

            // Act
            var result = await service.VerifyFaceAsync(dto);

            // Assert
            Assert.IsTrue(result.IsVerified);
            Assert.IsTrue(result.Similarity > 0.9f);
            Assert.AreEqual("Xác thực thành công", result.Message);
        }

        // UTCID02B: Có embedding nhưng similarity dưới threshold -> thất bại
        // Precondition: User tồn tại với FaceEmbedding, threshold = 0.7
        // Input: live embedding khác xa db embedding (cosine thấp)
        // Expected: IsVerified = false, Message = "Xác thực thất bại"
        // Exception: None
        // Result Type: N (Normal)
        // Log message: None
        [TestMethod]
        public async Task VerifyFaceAsync_BelowThreshold_ReturnsFailed()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var liveEmbedding = new float[] { 1f, 0f, 0f };
            var dbEmbedding = new float[] { -1f, 0f, 0f }; // cosine = -1

            var user = new User
            {
                UserId = userId,
                FirstName = "Test",
                LastName = "User",
                Username = "testuser",
                Email = "test@example.com",
                Phone = "0123456789",
                FaceEmbedding = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dbEmbedding))
            };
            await _db.Users.AddAsync(user);
            await _db.SaveChangesAsync();

            var service = new TestableFaceRecognitionService(_db, _fileStorageMock.Object, _loggerMock.Object, liveEmbedding);

            var dto = new FaceVerifyRequestDto
            {
                UserId = userId,
                Image = CreateFakeImage()
            };

            // Act
            var result = await service.VerifyFaceAsync(dto);

            // Assert
            Assert.IsFalse(result.IsVerified);
            Assert.AreEqual("Xác thực thất bại", result.Message);
        }

        // UTCID02C: Embedding trong DB bị corrupt -> trả về lỗi đọc dữ liệu
        // Precondition: User tồn tại với FaceEmbedding không phải JSON hợp lệ
        // Input: UserId = GUID tồn tại, Image = fake image
        // Expected: IsVerified = false, Message chứa "Lỗi khi đọc dữ liệu khuôn mặt từ database"
        // Exception: None (đã được catch)
        // Result Type: A (Abnormal)
        // Log message: Có lỗi log
        [TestMethod]
        public async Task VerifyFaceAsync_CorruptedEmbedding_ReturnsErrorMessage()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                UserId = userId,
                FirstName = "Test",
                LastName = "User",
                Username = "testuser",
                Email = "test@example.com",
                Phone = "0123456789",
                // Không phải JSON float[] hợp lệ
                FaceEmbedding = Encoding.UTF8.GetBytes("not-a-valid-json")
            };
            await _db.Users.AddAsync(user);
            await _db.SaveChangesAsync();

            var embedding = new float[] { 0.1f, 0.2f, 0.3f };
            var service = new TestableFaceRecognitionService(_db, _fileStorageMock.Object, _loggerMock.Object, embedding);

            var dto = new FaceVerifyRequestDto
            {
                UserId = userId,
                Image = CreateFakeImage()
            };

            // Act
            var result = await service.VerifyFaceAsync(dto);

            // Assert
            Assert.IsFalse(result.IsVerified);
            Assert.AreEqual("Lỗi khi đọc dữ liệu khuôn mặt từ database", result.Message);
        }

        #endregion

        #region RegisterFaceAsync

        // UTCID03: Đăng ký khuôn mặt cho user không tồn tại -> Fail
        // Precondition: Database trống, không có User
        // Input: UserId = GUID random, Image = fake image (hợp lệ)
        // Expected: Success = false, Message = "User không tồn tại"
        // Exception: None (normal flow)
        // Result Type: N (Normal)
        // Log message: None
        [TestMethod]
        public async Task RegisterFaceAsync_UserNotFound_ReturnsFail()
        {
            // Arrange
            var embedding = new float[] { 0.1f, 0.2f, 0.3f };
            var service = new TestableFaceRecognitionService(_db, _fileStorageMock.Object, _loggerMock.Object, embedding);

            var dto = new FaceRegisterRequestDto
            {
                UserId = Guid.NewGuid(),
                Image = CreateFakeImage()
            };

            // Act
            var result = await service.RegisterFaceAsync(dto);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.AreEqual("User không tồn tại", result.Message);
        }

        // UTCID04: Đăng ký khuôn mặt thành công -> lưu embedding + ảnh
        // Precondition: User tồn tại, storage helper save thành công
        // Input: UserId = GUID hợp lệ, Image = fake image (hợp lệ)
        // Expected: Success = true, FaceEmbedding không null, CheckinPhotoUrl = storage path
        // Exception: None (Success)
        // Result Type: N (Normal)
        // Log message: None
        [TestMethod]
        public async Task RegisterFaceAsync_Success_StoresEmbeddingAndPhoto()
        {
            // Arrange
            var user = new User
            {
                UserId = Guid.NewGuid(),
                FirstName = "Test",
                LastName = "User",              // thêm
                Username = "testuser",          // thêm
                Email = "test@example.com",     // thêm
                Phone = "0123456789"            // thêm
                                                // các field khác nếu cần
            };
            await _db.Users.AddAsync(user);
            await _db.SaveChangesAsync();

            var embedding = new float[] { 0.1f, 0.2f, 0.3f };
            var service = new TestableFaceRecognitionService(_db, _fileStorageMock.Object, _loggerMock.Object, embedding);

            var img = CreateFakeImage();
            var storedFile = new SAMS_BE.Models.File
            {
                FileId = Guid.NewGuid(),
                StoragePath = "face-registration/test.jpg",
                OriginalName = "test.jpg",
                MimeType = "image/jpeg",
                UploadedAt = DateTime.UtcNow
            };

            _fileStorageMock
                .Setup(f => f.SaveAsync(img, "face-registration", user.UserId.ToString()))
                .ReturnsAsync(storedFile);

            var dto = new FaceRegisterRequestDto
            {
                UserId = user.UserId,
                Image = img
            };

            // Act
            var result = await service.RegisterFaceAsync(dto);

            // Assert
            Assert.IsTrue(result.Success);
            var updatedUser = await _db.Users.FirstAsync(u => u.UserId == user.UserId);
            Assert.IsNotNull(updatedUser.FaceEmbedding);
            Assert.AreEqual(storedFile.StoragePath, updatedUser.CheckinPhotoUrl);
        }

        // UTCID04B: Đăng ký khuôn mặt - lỗi khi upload file -> trả về thất bại
        // Precondition: User tồn tại, _fileStorage.SaveAsync ném Exception
        // Input: UserId = hợp lệ, Image = fake image
        // Expected: Success = false, Message bắt đầu với "Lỗi:"
        // Exception: Đã được catch bên trong service
        // Result Type: A (Abnormal)
        [TestMethod]
        public async Task RegisterFaceAsync_FileStorageThrowsException_ReturnsError()
        {
            // Arrange
            var user = new User
            {
                UserId = Guid.NewGuid(),
                FirstName = "Test",
                LastName = "User",
                Username = "testuser",
                Email = "test@example.com",
                Phone = "0123456789"
            };
            await _db.Users.AddAsync(user);
            await _db.SaveChangesAsync();

            var embedding = new float[] { 0.1f, 0.2f, 0.3f };
            var service = new TestableFaceRecognitionService(_db, _fileStorageMock.Object, _loggerMock.Object, embedding);

            var img = CreateFakeImage();

            _fileStorageMock
                .Setup(f => f.SaveAsync(img, "face-registration", user.UserId.ToString()))
                .ThrowsAsync(new Exception("Upload error"));

            var dto = new FaceRegisterRequestDto
            {
                UserId = user.UserId,
                Image = img
            };

            // Act
            var result = await service.RegisterFaceAsync(dto);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsTrue(result.Message.StartsWith("Lỗi:"));
        }

        #endregion

        #region IdentifyFaceAsync

        // UTCID05: Không có user nào đã đăng ký -> NotIdentified
        // Precondition: Database Users trống
        // Input: Image = fake image (hợp lệ)
        // Expected: IsIdentified = false, Message = "Chưa có cư dân nào đăng ký khuôn mặt trong hệ thống."
        // Exception: None (Normal)
        // Result Type: N (Normal)
        // Log message: None
        [TestMethod]
        public async Task IdentifyFaceAsync_NoUsersRegistered_ReturnsNotIdentified()
        {
            // Arrange
            var embedding = new float[] { 0.1f, 0.2f, 0.3f };
            var service = new TestableFaceRecognitionService(_db, _fileStorageMock.Object, _loggerMock.Object, embedding);

            var img = CreateFakeImage();

            // Act
            var result = await service.IdentifyFaceAsync(img);

            // Assert
            Assert.IsFalse(result.IsIdentified);
            Assert.AreEqual("Chưa có cư dân nào đăng ký khuôn mặt trong hệ thống.", result.Message);
        }

        // UTCID05B: Ảnh null hoặc empty -> trả về lỗi bắt buộc ảnh
        // Precondition: Không quan trọng database
        // Input: image = null
        // Expected: IsIdentified = false, Message = "Ảnh khuôn mặt là bắt buộc."
        // Exception: None
        // Result Type: N (Normal)
        [TestMethod]
        public async Task IdentifyFaceAsync_NullImage_ReturnsError()
        {
            // Arrange
            var embedding = new float[] { 0.1f, 0.2f, 0.3f };
            var service = new TestableFaceRecognitionService(_db, _fileStorageMock.Object, _loggerMock.Object, embedding);

            // Act
            var result = await service.IdentifyFaceAsync(null!);

            // Assert
            Assert.IsFalse(result.IsIdentified);
            Assert.AreEqual("Ảnh khuôn mặt là bắt buộc.", result.Message);
        }

        // UTCID06: Có user đã đăng ký, embedding trùng vượt ngưỡng -> Identified
        // Precondition: Có 1 user với FaceEmbedding, CheckinPhotoUrl
        // Input: Image = fake image (hợp lệ), live embedding = db embedding
        // Expected: IsIdentified = true, UserId khớp, Similarity > 0.9
        // Exception: None (Success)
        // Result Type: N (Normal)
        // Log message: None
        [TestMethod]
        public async Task IdentifyFaceAsync_FindsBestMatchAboveThreshold_ReturnsIdentified()
        {
            // Arrange
            var fixedEmbedding = new float[] { 1f, 0f, 0f }; // live
            var dbEmbedding = new float[] { 1f, 0f, 0f };    // same

            var user = new User
            {
                UserId = Guid.NewGuid(),
                FirstName = "Nguyen",
                LastName = "A",
                // THÊM 3 FIELD BẮT BUỘC
                Username = "testuser",
                Email = "test@example.com",
                Phone = "0123456789",
                FaceEmbedding = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dbEmbedding)),
                CheckinPhotoUrl = "photo.jpg"
            };

            await _db.Users.AddAsync(user);
            await _db.SaveChangesAsync();

            var service = new TestableFaceRecognitionService(_db, _fileStorageMock.Object, _loggerMock.Object, fixedEmbedding);

            var img = CreateFakeImage();

            // Act
            var result = await service.IdentifyFaceAsync(img);

            // Assert
            Assert.IsTrue(result.IsIdentified);
            Assert.AreEqual(user.UserId, result.UserId);
            Assert.IsTrue(result.Similarity > 0.9f);
        }

        // UTCID06B: Có nhiều user nhưng tất cả similarity < threshold -> Không nhận diện
        // Precondition: Có ít nhất 1 user với FaceEmbedding, threshold = 0.7
        // Input: live embedding gần với user nhưng similarity < 0.7
        // Expected: IsIdentified = false, Message = "Không tìm thấy cư dân phù hợp.", Similarity = bestSimilarity
        // Exception: None
        // Result Type: N (Normal)
        [TestMethod]
        public async Task IdentifyFaceAsync_AllBelowThreshold_ReturnsNotIdentified()
        {
            // Arrange
            var liveEmbedding = new float[] { 1f, 0f, 0f };
            var dbEmbedding = new float[] { 0.5f, 0.5f, 0f }; // similarity ~ 0.707, gần ngưỡng; chỉnh xuống thấp hơn
            dbEmbedding = new float[] { 0.6f, 0.2f, 0.1f };   // similarity chắc chắn < 0.7

            var user = new User
            {
                UserId = Guid.NewGuid(),
                FirstName = "Nguyen",
                LastName = "B",
                Username = "testuser2",
                Email = "test2@example.com",
                Phone = "0987654321",
                FaceEmbedding = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dbEmbedding)),
                CheckinPhotoUrl = "photo2.jpg"
            };

            await _db.Users.AddAsync(user);
            await _db.SaveChangesAsync();

            var service = new TestableFaceRecognitionService(_db, _fileStorageMock.Object, _loggerMock.Object, liveEmbedding);
            var img = CreateFakeImage();

            // Act
            var result = await service.IdentifyFaceAsync(img);

            // Assert
            Assert.IsFalse(result.IsIdentified);
            Assert.AreEqual("Không tìm thấy cư dân phù hợp.", result.Message);
        }

        #endregion

        #region CosineSimilarity

        // UTCID07: So sánh cùng vector -> trả về 1
        // Precondition: Service khởi tạo với embedding bất kỳ
        // Input: v1 = v2 = {1,2,3}
        // Expected: CosineSimilarity = 1
        // Exception: None
        // Result Type: N (Normal)
        // Log message: None
        [TestMethod]
        public void CosineSimilarity_SameVector_ReturnsOne()
        {
            var service = new TestableFaceRecognitionService(_db, _fileStorageMock.Object, _loggerMock.Object, new float[] { 1f });
            var v = new float[] { 1f, 2f, 3f };
            var sim = service.CosineSimilarity(v, v);
            Assert.AreEqual(1f, sim, 1e-6);
        }

        // UTCID08: Hai vector trực giao -> Cosine = 0
        // Precondition: Service khởi tạo bình thường
        // Input: v1 = (1,0), v2 = (0,1)
        // Expected: CosineSimilarity gần 0
        // Exception: None
        [TestMethod]
        public void CosineSimilarity_OrthogonalVectors_ReturnsZero()
        {
            var service = new TestableFaceRecognitionService(_db, _fileStorageMock.Object, _loggerMock.Object, new float[] { 1f });
            var v1 = new float[] { 1f, 0f };
            var v2 = new float[] { 0f, 1f };
            var sim = service.CosineSimilarity(v1, v2);
            Assert.AreEqual(0f, sim, 1e-6);
        }

        // UTCID09: Khác độ dài -> ném ArgumentException
        // Precondition: Service khởi tạo bình thường
        // Input: v1 length = 2, v2 length = 3
        // Expected: ArgumentException
        [TestMethod]
        public void CosineSimilarity_DifferentLengths_ThrowsArgumentException()
        {
            var service = new TestableFaceRecognitionService(_db, _fileStorageMock.Object, _loggerMock.Object, new float[] { 1f });
            var v1 = new float[] { 1f, 0f };
            var v2 = new float[] { 1f, 0f, 0f };

            Assert.ThrowsException<ArgumentException>(() => service.CosineSimilarity(v1, v2));
        }

        #endregion
    }
}