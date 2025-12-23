using Microsoft.AspNetCore.Http;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using System.Linq;
using System.Text;
using System.Text.Json;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Models;
using Microsoft.EntityFrameworkCore;
using SAMS_BE.Helpers;

namespace SAMS_BE.Services;

public class FaceRecognitionService : IFaceRecognitionService
{
    private readonly InferenceSession _session;
    private readonly BuildingManagementContext _context;
    private readonly IFileStorageHelper _fileStorage;
    private readonly ILogger<FaceRecognitionService> _logger;
    private readonly string _cascadePath;

    public FaceRecognitionService(
        BuildingManagementContext context,
        IFileStorageHelper fileStorage,
        ILogger<FaceRecognitionService> logger)
    {
        _context = context;
        _fileStorage = fileStorage;
        _logger = logger;

        // Load ONNX model
        // Sử dụng AppContext.BaseDirectory để luôn trỏ tới thư mục output (bin/Debug|Release/netX)
        var baseDir = AppContext.BaseDirectory;
        var modelPath = Path.Combine(baseDir, "Models", "arcface_r100.onnx");
        if (!System.IO.File.Exists(modelPath))
        {
            _logger.LogWarning($"Model not found at: {modelPath}. Face recognition will not work until model is added.");
            // Tạo session rỗng để tránh crash, nhưng sẽ throw exception khi sử dụng
            _session = null!;
        }
        else
        {
            try
            {
                // Cấu hình SessionOptions để tối ưu bộ nhớ
                var sessionOptions = new Microsoft.ML.OnnxRuntime.SessionOptions
                {
                    // Sử dụng CPU provider (mặc định)
                    // Có thể thêm GPU provider nếu có
                    GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                    // Giới hạn số thread để tránh quá tải
                    IntraOpNumThreads = Environment.ProcessorCount > 4 ? 4 : Environment.ProcessorCount,
                    InterOpNumThreads = 1,
                    // Tối ưu bộ nhớ
                    EnableMemoryPattern = true,
                    EnableCpuMemArena = true,
                };

                // Load model với options
                _session = new InferenceSession(modelPath, sessionOptions);
                _logger.LogInformation($"ONNX model loaded successfully from: {modelPath}");
            }
            catch (OnnxRuntimeException ex)
            {
                _logger.LogError(ex, $"Failed to load ONNX model from {modelPath}. Error: {ex.Message}");

                // Thử load với cấu hình tối thiểu
                try
                {
                    _logger.LogWarning("Attempting to load model with minimal configuration...");
                    var minimalOptions = new Microsoft.ML.OnnxRuntime.SessionOptions
                    {
                        GraphOptimizationLevel = GraphOptimizationLevel.ORT_DISABLE_ALL,
                        IntraOpNumThreads = 1,
                        InterOpNumThreads = 1,
                        EnableMemoryPattern = false,
                        EnableCpuMemArena = false,
                    };
                    _session = new InferenceSession(modelPath, minimalOptions);
                    _logger.LogInformation("Model loaded with minimal configuration");
                }
                catch (Exception ex2)
                {
                    _logger.LogError(ex2, "Failed to load model even with minimal configuration. Face recognition will be disabled.");
                    _session = null!;
                    // DON'T throw - just disable face recognition
                    // This prevents breaking other endpoints that don't need face recognition
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error loading ONNX model from {modelPath}. Face recognition will be disabled.");
                _session = null!;
                // DON'T throw - just disable face recognition
            }
        }

        // Load face cascade
        _cascadePath = Path.Combine(baseDir, "Models", "haarcascade_frontalface_default.xml");
        if (!System.IO.File.Exists(_cascadePath))
        {
            _logger.LogWarning($"Cascade not found at: {_cascadePath}. Face detection will not work until cascade is added.");
        }
    }

    public async Task<FaceVerifyResponseDto> VerifyFaceAsync(FaceVerifyRequestDto request)
    {
        try
        {
            // Lấy user từ database
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == request.UserId);

            if (user == null)
                return new FaceVerifyResponseDto
                {
                    IsVerified = false,
                    Similarity = 0f,
                    Message = "User không tồn tại"
                };

            if (user.FaceEmbedding == null)
                return new FaceVerifyResponseDto
                {
                    IsVerified = false,
                    Similarity = 0f,
                    Message = "User chưa đăng ký khuôn mặt"
                };

            // Lưu file tạm để xử lý
            var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jpg");
            await using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await request.Image.CopyToAsync(stream);
            }

            try
            {
                // Lấy embedding từ ảnh mới
                var liveVec = GetEmbedding(tempPath);

                // Deserialize embedding từ database
                float[]? dbVec = null;
                try
                {
                    var json = System.Text.Encoding.UTF8.GetString(user.FaceEmbedding);
                    dbVec = System.Text.Json.JsonSerializer.Deserialize<float[]>(json);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deserializing face embedding for user {UserId}", request.UserId);
                    return new FaceVerifyResponseDto
                    {
                        IsVerified = false,
                        Similarity = 0f,
                        Message = "Lỗi khi đọc dữ liệu khuôn mặt từ database"
                    };
                }

                if (dbVec == null || dbVec.Length != liveVec.Length)
                    return new FaceVerifyResponseDto
                    {
                        IsVerified = false,
                        Similarity = 0f,
                        Message = "Lỗi khi so sánh embedding"
                    };

                // Tính similarity
                float sim = CosineSimilarity(liveVec, dbVec);
                float threshold = 0.7f;

                return new FaceVerifyResponseDto
                {
                    IsVerified = sim >= threshold,
                    Similarity = sim,
                    Message = sim >= threshold ? "Xác thực thành công" : "Xác thực thất bại"
                };
            }
            finally
            {
                // Xóa file tạm
                if (System.IO.File.Exists(tempPath))
                    System.IO.File.Delete(tempPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying face for user {UserId}", request.UserId);
            return new FaceVerifyResponseDto
            {
                IsVerified = false,
                Similarity = 0f,
                Message = $"Lỗi: {ex.Message}"
            };
        }
    }

    public async Task<FaceRegisterResponseDto> RegisterFaceAsync(FaceRegisterRequestDto request)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == request.UserId);

            if (user == null)
                return new FaceRegisterResponseDto
                {
                    Success = false,
                    Message = "User không tồn tại"
                };

            // Lưu file tạm để xử lý
            var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jpg");
            await using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await request.Image.CopyToAsync(stream);
            }

            try
            {
                // Lấy embedding
                var embedding = GetEmbedding(tempPath);

                // Serialize và lưu vào database
                var json = System.Text.Json.JsonSerializer.Serialize(embedding);
                user.FaceEmbedding = System.Text.Encoding.UTF8.GetBytes(json);

                // Upload ảnh lên Cloudinary (nếu cần)
                // IFormFile stream sẽ được reset tự động khi gọi SaveAsync
                var uploadedFile = await _fileStorage.SaveAsync(request.Image, "face-registration", user.UserId.ToString());
                user.CheckinPhotoUrl = uploadedFile.StoragePath;

                // Nếu người dùng chưa có avatar, dùng luôn ảnh này làm mặc định
                if (string.IsNullOrWhiteSpace(user.AvatarUrl))
                {
                    user.AvatarUrl = uploadedFile.StoragePath;
                }

                await _context.SaveChangesAsync();

                return new FaceRegisterResponseDto
                {
                    Success = true,
                    Message = "Đăng ký khuôn mặt thành công"
                };
            }
            finally
            {
                if (System.IO.File.Exists(tempPath))
                    System.IO.File.Delete(tempPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering face for user {UserId}", request.UserId);
            return new FaceRegisterResponseDto
            {
                Success = false,
                Message = $"Lỗi: {ex.Message}"
            };
        }
    }

    public async Task<FaceIdentifyResponseDto> IdentifyFaceAsync(IFormFile image)
    {
        if (image == null || image.Length == 0)
        {
            return new FaceIdentifyResponseDto
            {
                IsIdentified = false,
                Similarity = 0f,
                Message = "Ảnh khuôn mặt là bắt buộc."
            };
        }

        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jpg");
        await using (var stream = new FileStream(tempPath, FileMode.Create))
        {
            await image.CopyToAsync(stream);
        }

        try
        {
            var liveVec = GetEmbedding(tempPath);

            var users = await _context.Users
                .AsNoTracking()
                .Include(u => u.ResidentProfile)
                .Where(u => u.FaceEmbedding != null)
                .ToListAsync();

            users = users
                .Where(u => u.FaceEmbedding is { Length: > 0 })
                .ToList();

            if (!users.Any())
            {
                return new FaceIdentifyResponseDto
                {
                    IsIdentified = false,
                    Similarity = 0f,
                    Message = "Chưa có cư dân nào đăng ký khuôn mặt trong hệ thống."
                };
            }

            User? bestUser = null;
            float bestSimilarity = 0f;

            foreach (var user in users)
            {
                try
                {
                    float[]? dbVec = null;
                    try
                    {
                        var json = Encoding.UTF8.GetString(user.FaceEmbedding!);
                        dbVec = JsonSerializer.Deserialize<float[]>(json);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Không thể deserialize embedding cho user {UserId}. Có thể dữ liệu bị corrupt.", user.UserId);
                        continue;
                    }

                    if (dbVec == null || dbVec.Length != liveVec.Length)
                    {
                        continue;
                    }

                    var similarity = CosineSimilarity(liveVec, dbVec);
                    if (similarity > bestSimilarity)
                    {
                        bestSimilarity = similarity;
                        bestUser = user;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không thể xử lý embedding cho user {UserId}", user.UserId);
                }
            }

            float threshold = 0.7f;
            if (bestUser != null && bestSimilarity >= threshold)
            {
                return new FaceIdentifyResponseDto
                {
                    IsIdentified = true,
                    Similarity = bestSimilarity,
                    UserId = bestUser.UserId,
                    FullName = bestUser.ResidentProfile?.FullName
                               ?? $"{bestUser.FirstName} {bestUser.LastName}".Trim(),
                    AvatarUrl = bestUser.CheckinPhotoUrl ?? bestUser.AvatarUrl,
                    Message = "Nhận diện thành công."
                };
            }

            return new FaceIdentifyResponseDto
            {
                IsIdentified = false,
                Similarity = bestSimilarity,
                Message = "Không tìm thấy cư dân phù hợp."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error identifying face");
            return new FaceIdentifyResponseDto
            {
                IsIdentified = false,
                Similarity = 0f,
                Message = $"Lỗi: {ex.Message}"
            };
        }
        finally
        {
            if (System.IO.File.Exists(tempPath))
                System.IO.File.Delete(tempPath);
        }
    }

    public virtual float[] GetEmbedding(string imagePath)
    {
        if (_session == null)
            throw new InvalidOperationException("ONNX model chưa được load. Vui lòng đảm bảo file arcface_r100.onnx tồn tại trong thư mục Models.");

        if (!System.IO.File.Exists(_cascadePath))
            throw new FileNotFoundException($"Cascade file không tồn tại: {_cascadePath}");

       
        string tempImagePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + Path.GetExtension(imagePath));
        string tempCascadePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + Path.GetExtension(_cascadePath));
        try
        {
            System.IO.File.Copy(imagePath, tempImagePath, true);
            System.IO.File.Copy(_cascadePath, tempCascadePath, true);

            using var img = Cv2.ImRead(tempImagePath);
            if (img.Empty())
                throw new Exception("Không thể đọc ảnh (ImRead trả về rỗng).");

            var gray = new Mat();
            Cv2.CvtColor(img, gray, ColorConversionCodes.BGR2GRAY);

            // Dò khuôn mặt bằng cascade copy
            using var faceDetector = new CascadeClassifier(tempCascadePath);
            var faces = faceDetector.DetectMultiScale(gray, 1.1, 4);

            if (faces.Length == 0)
                throw new Exception("Không phát hiện khuôn mặt trong ảnh");

            // Cắt vùng mặt đầu tiên
            var faceRect = faces[0];
            using var face = new Mat(img, faceRect);
            Cv2.Resize(face, face, new Size(112, 112));
            Cv2.CvtColor(face, face, ColorConversionCodes.BGR2RGB);
            face.ConvertTo(face, MatType.CV_32F, 1.0 / 255);

            // NHWC: [1, 112, 112, 3]
            var input = new DenseTensor<float>(new[] { 1, 112, 112, 3 });

            for (int y = 0; y < 112; y++)
            {
                for (int x = 0; x < 112; x++)
                {
                    var px = face.At<Vec3f>(y, x);
                    input[0, y, x, 0] = px[0]; // R
                    input[0, y, x, 1] = px[1]; // G
                    input[0, y, x, 2] = px[2]; // B
                }
            }

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_1", input)
            };


            using var results = _session.Run(inputs);
            return results.First().AsEnumerable<float>().ToArray();
        }
        finally
        {
            // Xóa file tạm (nếu tồn tại) - không throw nếu lỗi xóa
            try { if (System.IO.File.Exists(tempImagePath)) System.IO.File.Delete(tempImagePath); } catch { }
            try { if (System.IO.File.Exists(tempCascadePath)) System.IO.File.Delete(tempCascadePath); } catch { }
        }
    }


    public virtual float CosineSimilarity(float[] vec1, float[] vec2)
    {
        if (vec1.Length != vec2.Length)
            throw new ArgumentException("Vectors must have the same length");

        float dotProduct = 0f;
        float norm1 = 0f;
        float norm2 = 0f;

        for (int i = 0; i < vec1.Length; i++)
        {
            dotProduct += vec1[i] * vec2[i];
            norm1 += vec1[i] * vec1[i];
            norm2 += vec2[i] * vec2[i];
        }

        return dotProduct / (float)(Math.Sqrt(norm1) * Math.Sqrt(norm2));
    }
}

