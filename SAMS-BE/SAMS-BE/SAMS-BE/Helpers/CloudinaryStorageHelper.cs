using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SAMS_BE.Models;

namespace SAMS_BE.Helpers
{
    public class CloudinaryStorageHelper : IFileStorageHelper
    {
        private readonly Cloudinary _cloudinary;
        private static readonly HttpClient _httpClient = new HttpClient();

        public CloudinaryStorageHelper(IConfiguration configuration)
        {
            var cloudName = configuration["Cloudinary:CloudName"];
            var apiKey = configuration["Cloudinary:ApiKey"];
            var apiSecret = configuration["Cloudinary:ApiSecret"];

            if (string.IsNullOrWhiteSpace(cloudName) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
            {
                throw new InvalidOperationException("Cloudinary configuration is missing. Please set Cloudinary:CloudName, ApiKey, ApiSecret in appsettings.");
            }

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
        }

        public async Task<SAMS_BE.Models.File> SaveAsync(IFormFile file, string subFolder, string? uploadedBy)
        {
            // Validate file based on context
            if (subFolder.Equals("tickets", StringComparison.OrdinalIgnoreCase))
            {
                FileValidationHelper.ValidateTicketFile(file);
            }
            else if (subFolder.Equals("documents", StringComparison.OrdinalIgnoreCase))
            {
                FileValidationHelper.ValidateDocumentFile(file);
            }
            else
            {
                FileValidationHelper.ValidateGeneralFile(file);
            }

            await using var stream = file.OpenReadStream();
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            // Kiểm tra loại file
            bool isImage = FileValidationHelper.AllowedImageExtensions.Contains(extension);

            RawUploadResult uploadResult;

            if (isImage)
            {
                // Ảnh → dùng ImageUploadParams (có preview & transform)
                var imageParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = $"{subFolder}",
                    PublicId = Guid.NewGuid().ToString(),
                    Type = "upload",
                    AccessMode = "public"
                };
                uploadResult = await _cloudinary.UploadAsync(imageParams);
            }
            else
            {
                // File khác (PDF, DOCX, ZIP...) → dùng RawUploadParams
                var rawParams = new RawUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = $"{subFolder}",
                    PublicId = Guid.NewGuid().ToString(),
                    Type = "upload",
                    AccessMode = "public"
                };
                uploadResult = await _cloudinary.UploadAsync(rawParams);
            }

            if ((int)uploadResult.StatusCode >= 300 || uploadResult.Error != null)
                throw new InvalidOperationException($"Cloudinary upload failed: {uploadResult.Error?.Message}");

            return new SAMS_BE.Models.File
            {
                FileId = Guid.NewGuid(),
                OriginalName = file.FileName,
                MimeType = file.ContentType,
                StoragePath = uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString() ?? string.Empty,
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.UtcNow
            };
        }

        public async Task<(Stream stream, string mime, string name)?> OpenAsync(string storagePath)
        {
            if (string.IsNullOrWhiteSpace(storagePath)) return null;

            try
            {
                var response = await _httpClient.GetAsync(storagePath);
                if (!response.IsSuccessStatusCode) return null;
                var stream = await response.Content.ReadAsStreamAsync();
                var mime = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                var fileName = Path.GetFileName(new Uri(storagePath).LocalPath);
                return (stream, mime, fileName);
            }
            catch
            {
                return null;
            }
        }
    }
}


