using Microsoft.AspNetCore.Http;

namespace SAMS_BE.Helpers
{
    public interface IFileStorageHelper
    {
        Task<SAMS_BE.Models.File> SaveAsync(IFormFile file, string subFolder, string? uploadedBy);
        Task<(Stream stream, string mime, string name)?> OpenAsync(string storagePath);
    }
}
