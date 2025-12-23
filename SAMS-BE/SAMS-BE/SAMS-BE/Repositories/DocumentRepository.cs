using Microsoft.EntityFrameworkCore;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Models;
using SAMS_BE.Helpers;

namespace SAMS_BE.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly BuildingManagementContext _db;
        public DocumentRepository(BuildingManagementContext db) { _db = db; }

        public IQueryable<Document> Query() => _db.Documents.Where(d => !d.IsDelete);
        public IQueryable<Document> Documents() => _db.Documents.Where(d => !d.IsDelete);
        public IQueryable<DocumentVersion> DocumentVersions() => _db.DocumentVersions.AsQueryable();

        public Task<Document?> GetAsync(Guid id) =>
            _db.Documents
               .Where(x => !x.IsDelete)
               .Include(x => x.DocumentVersions).ThenInclude(v => v.File)
               .Include(x => x.DocumentActionLogs)
               .FirstOrDefaultAsync(x => x.DocumentId == id);

        public Task<SAMS_BE.Models.File?> GetFileAsync(Guid fileId) =>
            _db.Files.FirstOrDefaultAsync(f => f.FileId == fileId);

        public Task<DocumentVersion?> GetVersionAsync(Guid documentId, int versionNo) =>
            _db.DocumentVersions
               .Include(v => v.File)
               .FirstOrDefaultAsync(v => v.DocumentId == documentId && v.VersionNo == versionNo);

        public async Task<List<(Document doc, DocumentVersion ver, SAMS_BE.Models.File file)>> GetLatestVersionsAsync()
        {
            // EF-friendly: chọn bản ghi có VersionNo = max(VersionNo) theo từng document
            // Include DocumentVersions để có thể truy cập currentVersion
            var query = from d in _db.Documents.Include(d => d.DocumentVersions).ThenInclude(v => v.File)
                        where !d.IsDelete
                        join v in _db.DocumentVersions on d.DocumentId equals v.DocumentId
                        where v.VersionNo == _db.DocumentVersions
                            .Where(x => x.DocumentId == d.DocumentId)
                            .Max(x => x.VersionNo)
                        join f in _db.Files on v.FileId equals f.FileId
                        select new { d, v, f };

            var list = await query.ToListAsync();
            return list.Select(x => (x.d, x.v, x.f)).ToList();
        }

        public async Task AddLogAsync(Guid documentId, string action, Guid? actorId, string? detail)
        {
            await _db.DocumentActionLogs.AddAsync(new DocumentActionLog
            {
                ActionLogId = Guid.NewGuid(),
                DocumentId = documentId,
                Action = action,
                ActorId = actorId,
                ActionAt = DateTimeHelper.VietnamNow,
                Detail = detail
            });
        }

        public async Task<List<DTOs.DocumentActionLogDto>> GetLogsAsync(Guid documentId)
        {
            var logs = await (from l in _db.DocumentActionLogs
                              where l.DocumentId == documentId
                              join u in _db.Users on l.ActorId equals u.UserId into userJoin
                              from user in userJoin.DefaultIfEmpty()
                              orderby l.ActionAt descending
                              select new
                              {
                                  l.ActionLogId,
                                  l.DocumentId,
                                  l.Action,
                                  l.ActorId,
                                  ActorName = user != null ? user.Username : null,
                                  l.ActionAt,
                                  l.Detail
                              })
                             .ToListAsync();

            // Parse username from Detail if ActorName is null (for old logs)
            return logs.Select(x => new DTOs.DocumentActionLogDto
            {
                ActionLogId = x.ActionLogId,
                DocumentId = x.DocumentId,
                Action = x.Action,
                ActorId = x.ActorId,
                ActorName = x.ActorName ?? ExtractUsernameFromDetail(x.Detail),
                ActionAt = x.ActionAt,
                Detail = x.Detail
            }).ToList();
        }

        private string? ExtractUsernameFromDetail(string? detail)
        {
            if (string.IsNullOrWhiteSpace(detail)) return null;

            // Try to extract username from patterns like:
            // "Created by quocthe"
            // "v1 uploaded by quocthe"
            // "Approved by ngocanh"
            try
            {
                var patterns = new[]
                {
                    @"(?:Created|created)\s+by\s+(\S+)",  // Match any non-whitespace after "by"
                    @"(?:uploaded|Uploaded)\s+by\s+(\S+)",
                    @"(?:Approved|approved)\s+by\s+(\S+)",
                    @"(?:Tạo|tạo)\s+(?:tài\s+liệu\s+)?bởi\s+(\S+)",
                    @"(?:Tải|tải)\s+lên(?:\s+phiên\s+bản)?\s*(?:v\d+)?\s*bởi\s+(\S+)",
                    @"(?:Phê|phê)\s+duyệt\s+bởi\s+(\S+)",
                    @"(?:Từ|từ)\s+chối\s+bởi\s+(\S+)",
                    @"bởi\s+(\S+)",  // General Vietnamese pattern
                    @"by\s+(\S+)"  // General English pattern
                };

                foreach (var pattern in patterns)
                {
                    var match = System.Text.RegularExpressions.Regex.Match(detail, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (match.Success && match.Groups.Count > 1 && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
                    {
                        return match.Groups[1].Value.Trim();
                    }
                }
            }
            catch
            {
                // If regex fails, try simple string search
                var byIndex = detail.IndexOf(" by ", StringComparison.OrdinalIgnoreCase);
                if (byIndex >= 0)
                {
                    var afterBy = detail.Substring(byIndex + 4).Trim();
                    if (!string.IsNullOrWhiteSpace(afterBy))
                    {
                        // Take first word after "by"
                        var parts = afterBy.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 0)
                        {
                            return parts[0];
                        }
                    }
                }
            }

            return null;
        }

        public async Task AddAsync(Document doc) => await _db.Documents.AddAsync(doc);
        public async Task AddVersionAsync(DocumentVersion version) => await _db.DocumentVersions.AddAsync(version);
        public async Task AddFileAsync(SAMS_BE.Models.File file) => await _db.Files.AddAsync(file);
        public async Task AddActionLogAsync(DocumentActionLog log) => await _db.DocumentActionLogs.AddAsync(log);
        public Task SaveChangesAsync() => _db.SaveChangesAsync();
    }
}