using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Interfaces.IService
{
    public interface IDocumentService
    {
        Task<(IEnumerable<LatestDocumentDto> items, int total)> SearchAsync(DocumentQueryDto dto);
        Task<Document> CreateAsync(CreateDocumentDto dto, Guid? actorId);
        Task<Document> CreateWithFirstVersionAsync(CreateDocumentDto dto, IFormFile file, string? note, Guid? actorId);
        Task<Document?> GetAsync(Guid id);
        Task<DocumentVersion?> AddVersionAsync(Guid documentId, IFormFile file, string? note, string? createdBy, Guid? actorId);
        Task<DocumentVersion?> GetVersionAsync(Guid documentId, int versionNo);
        Task<bool> UpdateMetadataAsync(Guid id, UpdateDocumentDto dto, Guid? actorId);
        Task<bool> SoftDeleteAsync(Guid id, Guid? actorId, string? reason);
        Task<bool> RestoreAsync(Guid id, Guid? actorId, string? reason);
        Task<List<DocumentActionLogDto>> GetLogsAsync(Guid id);
        Task<bool> ChangeStatusAsync(Guid id, string status, Guid? actorId, string? detail);
        Task<(Stream stream, string mime, string name)?> DownloadAsync(Guid fileId);
        Task<SAMS_BE.Models.File?> GetFileAsync(Guid fileId);
        Task<IEnumerable<LatestDocumentDto>> GetAllWithLatestVersionAsync(string? status = null);
        Task<IEnumerable<DocumentVersion>> GetAllVersionsAsync(Guid documentId);
        Task<IEnumerable<ResidentDocumentDto>> GetResidentDocumentsAsync(DocumentQueryDto dto);
        Task<IEnumerable<ResidentDocumentDto>> GetReceptionistDocumentsAsync(DocumentQueryDto dto);
        Task<IEnumerable<ResidentDocumentDto>> GetAccountingDocumentsAsync(DocumentQueryDto dto);
    }
}