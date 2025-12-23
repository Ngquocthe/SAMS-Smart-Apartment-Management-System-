using SAMS_BE.Models;

namespace SAMS_BE.Interfaces.IRepository
{
    public interface IDocumentRepository
    {
        IQueryable<Document> Query();
        Task<Document?> GetAsync(Guid id);
        Task<Models.File?> GetFileAsync(Guid fileId);
        Task<DocumentVersion?> GetVersionAsync(Guid documentId, int versionNo);
        Task<List<(Document doc, DocumentVersion ver, SAMS_BE.Models.File file)>> GetLatestVersionsAsync();
        Task AddLogAsync(Guid documentId, string action, Guid? actorId, string? detail);
        Task<List<DTOs.DocumentActionLogDto>> GetLogsAsync(Guid documentId);
        IQueryable<Document> Documents();
        IQueryable<DocumentVersion> DocumentVersions();
        Task AddAsync(Document doc);
        Task AddVersionAsync(DocumentVersion version);
        Task AddFileAsync(Models.File file);
        Task AddActionLogAsync(DocumentActionLog log);
        Task SaveChangesAsync();
    }
}