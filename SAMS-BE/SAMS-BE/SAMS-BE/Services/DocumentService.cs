using Microsoft.EntityFrameworkCore;
using SAMS_BE.DTOs;
using SAMS_BE.Enums;
using SAMS_BE.Helpers;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Models;
using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _repo;
        private readonly IFileStorageHelper _storage;
        private readonly IUserRepository _userRepo;

        public DocumentService(IDocumentRepository repo, IFileStorageHelper storage, IUserRepository userRepo)
        {
            _repo = repo;
            _storage = storage;
            _userRepo = userRepo;
        }

        public async Task<(IEnumerable<LatestDocumentDto> items, int total)> SearchAsync(DocumentQueryDto dto)
        {
            var q = _repo.Documents();
            if (!string.IsNullOrWhiteSpace(dto.Title)) q = q.Where(x => x.Title.Contains(dto.Title));
            if (!string.IsNullOrWhiteSpace(dto.Category)) q = q.Where(x => x.Category == dto.Category);
            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                var status = dto.Status.Trim();
                var upper = status.ToUpperInvariant();

                // INACTIVE / Ngừng hiển thị -> bao gồm cả đã xóa
                if (upper == "INACTIVE" || status.Equals("Ngừng hiển thị", StringComparison.OrdinalIgnoreCase))
                {
                    q = q.Where(x =>
                        x.Status == "INACTIVE" ||
                        x.Status == "DELETED" ||
                        x.Status == "Ngừng hiển thị" ||
                        x.Status == "Đã xóa");
                }
                // PENDING_APPROVAL từ FE: bao gồm cả chờ duyệt nội dung và chờ duyệt xóa
                else if (upper == "PENDING_APPROVAL" || status.Equals("Chờ duyệt", StringComparison.OrdinalIgnoreCase))
                {
                    q = q.Where(x =>
                        x.Status == "PENDING_APPROVAL" ||
                        x.Status == "PENDING_DELETE" ||
                        x.Status == "Chờ duyệt" ||
                        x.Status == "Chờ duyệt xóa");
                }
                else
                {
                    // So khớp cả code tiếng Anh cũ (ACTIVE, REJECTED, ...) lẫn tiếng Việt nếu sau này đổi
                    q = q.Where(x => x.Status == status || x.Status.ToUpper() == upper);
                }
            }
            if (!string.IsNullOrWhiteSpace(dto.VisibilityScope))
            {
                q = q.Where(x => x.VisibilityScope == dto.VisibilityScope);
            }

            var total = await q.CountAsync();

            var pagedDocs = await q
                .OrderByDescending(x => x.CreatedAt)
                .Skip((dto.Page - 1) * dto.PageSize)
                .Take(dto.PageSize)
                .Include(d => d.DocumentVersions)
                    .ThenInclude(v => v.File)
                .ToListAsync();

            var items = new List<LatestDocumentDto>();
            foreach (var d in pagedDocs)
            {
                var latest = d.DocumentVersions
                    .OrderByDescending(v => v.VersionNo)
                    .FirstOrDefault();
                if (latest == null) continue;

                // Mặc định dùng latestVersion
                var displayVersion = latest;

                // Nếu có currentVersion, dùng currentVersion thay vì latestVersion
                if (d.CurrentVersion.HasValue)
                {
                    var currentVer = d.DocumentVersions.FirstOrDefault(v => v.VersionNo == d.CurrentVersion.Value);
                    if (currentVer != null && currentVer.File != null)
                    {
                        displayVersion = currentVer;
                    }
                }

                items.Add(new LatestDocumentDto
                {
                    DocumentId = d.DocumentId,
                    Category = d.Category,
                    Title = d.Title,
                    VisibilityScope = d.VisibilityScope,
                    Status = d.Status,
                    CreatedAt = d.CreatedAt,
                    CreatedBy = d.CreatedBy,
                    LatestVersionNo = latest.VersionNo,
                    CurrentVersion = d.CurrentVersion,
                    FileId = displayVersion.FileId,
                    VersionNote = displayVersion.Note,
                    ChangedAt = displayVersion.ChangedAt,
                    OriginalFileName = displayVersion.File?.OriginalName ?? string.Empty,
                    MimeType = displayVersion.File?.MimeType ?? "application/octet-stream"
                });
            }

            return (items, total);
        }

        public async Task<Document> CreateAsync(CreateDocumentDto dto, Guid? actorId)
        {
            var normalizedCategory = NormalizeCategory(dto.Category);
            var normalizedTitle = NormalizeTitle(dto.Title);
            var normalizedScope = NormalizeVisibilityScope(dto.VisibilityScope);

            var doc = new Document
            {
                DocumentId = Guid.NewGuid(),
                Category = normalizedCategory,
                Title = normalizedTitle,
                VisibilityScope = normalizedScope,
                Status = "PENDING_APPROVAL",
                CreatedAt = DateTimeHelper.VietnamNow,
                CreatedBy = dto.CreatedBy
            };
            await _repo.AddAsync(doc);
            await _repo.AddActionLogAsync(new DocumentActionLog
            {
                ActionLogId = Guid.NewGuid(),
                DocumentId = doc.DocumentId,
                Action = "CREATE",
                ActorId = actorId,
                ActionAt = DateTimeHelper.VietnamNow,
                Detail = $"Tạo bởi {dto.CreatedBy}"
            });
            await _repo.SaveChangesAsync();
            return doc;
        }

        public async Task<Document> CreateWithFirstVersionAsync(CreateDocumentDto dto, IFormFile file, string? note, Guid? actorId)
        {
            var doc = await CreateAsync(dto, actorId);
            var version = await AddVersionAsync(doc.DocumentId, file, note, dto.CreatedBy, actorId);
            return doc;
        }

        public Task<Document?> GetAsync(Guid id) => _repo.GetAsync(id);

        public async Task<DocumentVersion?> AddVersionAsync(Guid documentId, IFormFile file, string? note, string? createdBy, Guid? actorId)
        {
            var doc = await _repo.GetAsync(documentId);
            if (doc == null) return null;

            // Validate file size/type for documents before upload
            FileValidationHelper.ValidateDocumentFile(file);

            var isPending = string.Equals(doc.Status, "PENDING_APPROVAL", StringComparison.OrdinalIgnoreCase)
                || string.Equals(doc.Status, "Chờ duyệt", StringComparison.OrdinalIgnoreCase);
            var isInactive = string.Equals(doc.Status, "INACTIVE", StringComparison.OrdinalIgnoreCase)
                || string.Equals(doc.Status, "Ngừng hiển thị", StringComparison.OrdinalIgnoreCase);

            // Không cho phép upload version mới khi đang chờ phê duyệt (từ lần 2 trở đi) hoặc khi đã ngừng hiển thị
            if ((doc.DocumentVersions.Any() && isPending) || isInactive)
            {
                var reason = isInactive
                    ? "Tài liệu đang ngừng hiển thị, không thể thêm phiên bản mới."
                    : "Không thể upload phiên bản mới khi tài liệu đang chờ phê duyệt. Vui lòng đợi phê duyệt xong.";

                throw new InvalidOperationException(reason);
            }

            var saved = await _storage.SaveAsync(file, "documents", createdBy);
            await _repo.AddFileAsync(saved);

            var nextVer = (doc.DocumentVersions.Any() ? doc.DocumentVersions.Max(v => v.VersionNo) : 0) + 1;

            // Lấy username để hiển thị trong log
            string actorName = createdBy ?? string.Empty;
            if (string.IsNullOrWhiteSpace(actorName) && actorId.HasValue)
            {
                var user = await _userRepo.GetByIdAsync(actorId.Value);
                if (user != null)
                {
                    actorName = !string.IsNullOrWhiteSpace(user.Username)
                        ? user.Username
                        : (!string.IsNullOrWhiteSpace(user.Email) ? user.Email : actorId.Value.ToString());
                }
                else
                {
                    actorName = actorId.Value.ToString();
                }
            }
            if (string.IsNullOrWhiteSpace(actorName))
            {
                actorName = "Không xác định";
            }

            var version = new DocumentVersion
            {
                DocumentVersionId = Guid.NewGuid(),
                DocumentId = documentId,
                VersionNo = nextVer,
                FileId = saved.FileId,
                Note = note,
                ChangedAt = DateTimeHelper.VietnamNow,
                CreatedBy = createdBy ?? actorName
            };
            await _repo.AddVersionAsync(version);
            await _repo.AddActionLogAsync(new DocumentActionLog
            {
                ActionLogId = Guid.NewGuid(),
                DocumentId = documentId,
                Action = "NEW_VERSION",
                ActorId = actorId,
                ActionAt = DateTimeHelper.VietnamNow,
                Detail = $"v{nextVer} đăng bởi {actorName}"
            });
            if (!string.Equals(doc.Status, "PENDING_APPROVAL", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(doc.Status, "Chờ duyệt", StringComparison.OrdinalIgnoreCase))
            {
                doc.Status = "PENDING_APPROVAL";
            }

            await _repo.SaveChangesAsync();
            return version;
        }

        public async Task<bool> ChangeStatusAsync(Guid id, string status, Guid? actorId, string? detail)
        {
            var doc = await _repo.GetAsync(id);
            if (doc == null) return false;
            var normalized = status?.Trim().ToUpperInvariant() ?? string.Empty;
            if (string.IsNullOrEmpty(normalized)) return false;

            // UC-023: Re-activate Document - Chỉ cho phép từ INACTIVE/Ngừng hiển thị -> PENDING_APPROVAL
            if (normalized == "PENDING_APPROVAL" || normalized == "CHỜ DUYỆT")
            {
                var currentStatus = doc.Status?.Trim().ToUpperInvariant() ?? string.Empty;
                var isInactive = currentStatus == "INACTIVE" ||
                                 currentStatus == "NGỪNG HIỂN THỊ" ||
                                 doc.Status?.Equals("Ngừng hiển thị", StringComparison.OrdinalIgnoreCase) == true;
                var isPending = currentStatus == "PENDING_APPROVAL" ||
                                currentStatus == "CHỜ DUYỆT" ||
                                doc.Status?.Equals("Chờ duyệt", StringComparison.OrdinalIgnoreCase) == true;

                // Nếu đã ở PENDING_APPROVAL thì không cần làm gì
                if (isPending)
                {
                    return true;
                }

                // Chỉ cho phép re-activate từ INACTIVE
                if (!isInactive)
                {
                    throw new InvalidOperationException("Chỉ có thể yêu cầu bật hiển thị lại tài liệu đang ở trạng thái 'Ngừng hiển thị'.");
                }

                doc.Status = "PENDING_APPROVAL";
                var logDetail = detail ?? "Yêu cầu bật hiển thị lại tài liệu";
                await _repo.AddActionLogAsync(new DocumentActionLog
                {
                    ActionLogId = Guid.NewGuid(),
                    DocumentId = id,
                    Action = "REQUEST_REACTIVATE",
                    ActorId = actorId,
                    ActionAt = DateTimeHelper.VietnamNow,
                    Detail = logDetail
                });
                await _repo.SaveChangesAsync();
                return true;
            }

            // Nếu từ chối, rollback metadata nếu đang chờ duyệt metadata update
            if (normalized == "REJECTED" || normalized == "BỊ TỪ CHỐI")
            {
                // Chỉ rollback nếu status hiện tại là PENDING_APPROVAL (đang chờ duyệt metadata)
                if (string.Equals(doc.Status, "PENDING_APPROVAL", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(doc.Status, "Chờ duyệt", StringComparison.OrdinalIgnoreCase))
                {
                    // Tìm log UPDATE_METADATA gần nhất để rollback
                    var logs = await _repo.GetLogsAsync(id);
                    var lastMetadataUpdate = logs.FirstOrDefault(l => l.Action == "UPDATE_METADATA");

                    if (lastMetadataUpdate != null && !string.IsNullOrWhiteSpace(lastMetadataUpdate.Detail))
                    {
                        // Parse detail để rollback metadata
                        // Format: "Tiêu đề: \"old\" → \"new\"; Phân loại: \"old\" → \"new\"; Phạm vi: \"old\" → \"new\""
                        var changes = lastMetadataUpdate.Detail.Split(';');
                        foreach (var change in changes)
                        {
                            if (change.Contains("→"))
                            {
                                var parts = change.Split('→');
                                if (parts.Length == 2)
                                {
                                    var fieldPart = parts[0].Trim();

                                    // Extract field name and old value (phần trước dấu →)
                                    if (fieldPart.Contains("Tiêu đề:"))
                                    {
                                        var oldValue = ExtractQuotedValue(parts[0]);
                                        if (!string.IsNullOrEmpty(oldValue) && oldValue != "(trống)")
                                        {
                                            doc.Title = oldValue;
                                        }
                                    }
                                    else if (fieldPart.Contains("Phân loại:"))
                                    {
                                        var oldValue = ExtractQuotedValue(parts[0]);
                                        if (!string.IsNullOrEmpty(oldValue) && oldValue != "(trống)")
                                        {
                                            doc.Category = oldValue;
                                        }
                                    }
                                    else if (fieldPart.Contains("Phạm vi:"))
                                    {
                                        var oldValue = ExtractQuotedValue(parts[0]);
                                        if (oldValue != "(trống)")
                                        {
                                            doc.VisibilityScope = string.IsNullOrEmpty(oldValue) ? null : oldValue;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Kiểm tra xem có CurrentVersion không (tức là đã có version được publish trước đó)
                if (doc.CurrentVersion.HasValue)
                {
                    // Quay lại ACTIVE và giữ nguyên CurrentVersion (không cập nhật lên version mới)
                    doc.Status = "ACTIVE";
                    // CurrentVersion giữ nguyên, không thay đổi
                }
                else
                {
                    // Nếu chưa có version nào được publish, thì giữ nguyên REJECTED
                    doc.Status = normalized;
                }
            }
            else if (normalized == "ACTIVE" || normalized == "HOẠT ĐỘNG")
            {
                // Khi approve, chỉ cập nhật CurrentVersion nếu có version mới (không phải chỉ sửa metadata)
                doc.Status = "ACTIVE";

                // Kiểm tra xem có version mới hơn CurrentVersion không
                var latestVersion = doc.DocumentVersions
                    .OrderByDescending(v => v.VersionNo)
                    .FirstOrDefault();

                if (latestVersion != null)
                {
                    // Chỉ cập nhật CurrentVersion nếu có version mới hơn version hiện tại
                    if (!doc.CurrentVersion.HasValue || latestVersion.VersionNo > doc.CurrentVersion.Value)
                    {
                        doc.CurrentVersion = latestVersion.VersionNo;
                    }
                    // Nếu chỉ sửa metadata (không có version mới), giữ nguyên CurrentVersion
                }
            }
            else
            {
                doc.Status = normalized;
            }

            await _repo.AddActionLogAsync(new DocumentActionLog
            {
                ActionLogId = Guid.NewGuid(),
                DocumentId = id,
                Action = "CHANGE_STATUS",
                ActorId = actorId,
                ActionAt = DateTimeHelper.VietnamNow,
                Detail = detail
            });
            await _repo.SaveChangesAsync();
            return true;
        }

        private static string? NormalizeVisibilityScope(string? scope)
        {
            if (string.IsNullOrWhiteSpace(scope)) return null;

            var cleaned = string.Join(' ',
                scope
                    .Trim()
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries));

            if (Enum.TryParse<VisibilityScope>(cleaned, true, out var parsed))
            {
                return parsed.ToString();
            }

            throw new ValidationException($"VisibilityScope không hợp lệ. Giá trị hợp lệ: {string.Join(", ", Enum.GetNames(typeof(VisibilityScope)))}");
        }

        private static string NormalizeCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                throw new ArgumentException("Category là bắt buộc", nameof(category));
            }

            return category.Trim();
        }

        private static string NormalizeTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Title không được để trống", nameof(title));
            }

            // Chuẩn hóa: loại bỏ thừa đầu/cuối và gộp nhiều khoảng trắng ở giữa
            var cleaned = string.Join(' ',
                title
                    .Trim()
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries));

            if (cleaned.Length < 3)
            {
                throw new ArgumentException("Title phải có ít nhất 3 ký tự", nameof(title));
            }

            if (string.IsNullOrWhiteSpace(cleaned))
            {
                throw new ArgumentException("Title không được để trống", nameof(title));
            }

            return cleaned;
        }

        private static string? ExtractQuotedValue(string text)
        {
            // Extract value from format: "Field: \"value\" → ..."
            var startIndex = text.IndexOf('"');
            if (startIndex < 0) return null;
            startIndex++;
            var endIndex = text.IndexOf('"', startIndex);
            if (endIndex < 0) return null;
            return text.Substring(startIndex, endIndex - startIndex);
        }

        public async Task<(Stream stream, string mime, string name)?> DownloadAsync(Guid fileId)
        {
            var f = await _repo.GetFileAsync(fileId);
            if (f == null) return null;
            return await _storage.OpenAsync(f.StoragePath);
        }

        public Task<SAMS_BE.Models.File?> GetFileAsync(Guid fileId)
        {
            return _repo.GetFileAsync(fileId);
        }

        public Task<DocumentVersion?> GetVersionAsync(Guid documentId, int versionNo)
        {
            return _repo.GetVersionAsync(documentId, versionNo);
        }

        public async Task<IEnumerable<LatestDocumentDto>> GetAllWithLatestVersionAsync(string? status = null)
        {
            var rows = await _repo.GetLatestVersionsAsync();
            if (string.IsNullOrWhiteSpace(status))
            {
                // Mặc định chỉ lấy tài liệu đang hoạt động
                rows = rows.Where(x =>
                    x.doc.Status == "ACTIVE" ||
                    x.doc.Status == "Hoạt động").ToList();
            }
            else
            {
                var s = status.Trim();
                var upper = s.ToUpperInvariant();

                // INACTIVE / Ngừng hiển thị: bao gồm cả đã xóa
                if (upper == "INACTIVE" || s.Equals("Ngừng hiển thị", StringComparison.OrdinalIgnoreCase))
                {
                    rows = rows.Where(x =>
                        x.doc.Status == "INACTIVE" ||
                        x.doc.Status == "DELETED"
                        ).ToList();
                }
                else
                {
                    rows = rows.Where(x =>
                        x.doc.Status == s ||
                        x.doc.Status.ToUpper() == upper).ToList();
                }
            }
            return rows.Select(x => new LatestDocumentDto
            {
                DocumentId = x.doc.DocumentId,
                Category = x.doc.Category,
                Title = x.doc.Title,
                VisibilityScope = x.doc.VisibilityScope,
                Status = x.doc.Status,
                CreatedAt = x.doc.CreatedAt,
                CreatedBy = x.doc.CreatedBy,
                LatestVersionNo = x.ver.VersionNo,
                CurrentVersion = x.doc.CurrentVersion,
                FileId = x.ver.FileId,
                VersionNote = x.ver.Note,
                ChangedAt = x.ver.ChangedAt,
                OriginalFileName = x.file.OriginalName,
                MimeType = x.file.MimeType
            });
        }

        public async Task<bool> UpdateMetadataAsync(Guid id, UpdateDocumentDto dto, Guid? actorId)
        {
            var doc = await _repo.GetAsync(id);
            if (doc == null) return false;

            // Không cho phép sửa metadata khi đang chờ phê duyệt
            if (string.Equals(doc.Status, "PENDING_APPROVAL", StringComparison.OrdinalIgnoreCase)
                || string.Equals(doc.Status, "Chờ duyệt", StringComparison.OrdinalIgnoreCase)
                || string.Equals(doc.Status, "INACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Không thể sửa thông tin khi tài liệu đang chờ phê duyệt hoặc đã ngừng hiển thị. Vui lòng đợi phê duyệt xong.");
            }

            var changes = new List<string>();

            static string FormatValue(string? value) => string.IsNullOrWhiteSpace(value) ? "(trống)" : value!;

            if (!string.IsNullOrWhiteSpace(dto.Title))
            {
                var normalizedTitle = NormalizeTitle(dto.Title);
                if (!string.Equals(normalizedTitle, doc.Title, StringComparison.Ordinal))
                {
                    changes.Add($"Tiêu đề: \"{FormatValue(doc.Title)}\" → \"{normalizedTitle}\"");
                    doc.Title = normalizedTitle;
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.Category))
            {
                var normalizedCategory = NormalizeCategory(dto.Category);
                if (!string.Equals(normalizedCategory, doc.Category, StringComparison.Ordinal))
                {
                    changes.Add($"Phân loại: \"{FormatValue(doc.Category)}\" → \"{normalizedCategory}\"");
                    doc.Category = normalizedCategory;
                }
            }

            if (dto.VisibilityScope != null)
            {
                var normalizedScope = NormalizeVisibilityScope(dto.VisibilityScope);
                if (!string.Equals(normalizedScope, doc.VisibilityScope, StringComparison.Ordinal))
                {
                    changes.Add($"Phạm vi: \"{FormatValue(doc.VisibilityScope)}\" → \"{normalizedScope}\"");
                    doc.VisibilityScope = normalizedScope;
                }
            }

            if (changes.Count == 0)
            {
                return true;
            }

            var detail = string.Join("; ", changes);

            await _repo.AddLogAsync(id, "UPDATE_METADATA", actorId, detail);
            // Sau khi chỉnh sửa metadata, tài liệu quay về trạng thái chờ duyệt
            doc.Status = "PENDING_APPROVAL";

            await _repo.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SoftDeleteAsync(Guid id, Guid? actorId, string? reason)
        {
            var doc = await _repo.GetAsync(id);
            if (doc == null) return false;

            var isInactive = string.Equals(doc.Status, "INACTIVE", StringComparison.OrdinalIgnoreCase)
                || string.Equals(doc.Status, "Ngừng hiển thị", StringComparison.OrdinalIgnoreCase);

            // Nếu đã ngừng hiển thị, cho phép đánh dấu xóa hẳn bằng cột is_delete
            if (isInactive)
            {
                doc.IsDelete = true;
                await _repo.AddLogAsync(id, "HARD_DELETE", actorId, reason ?? "Xóa tài liệu sau khi ngừng hiển thị");
            }
            else
            {
                // Bước 1: chuyển sang trạng thái ngừng hiển thị
                doc.Status = "INACTIVE";
                doc.IsDelete = false;
                await _repo.AddLogAsync(id, "SOFT_DELETE", actorId, reason);
            }

            await _repo.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestoreAsync(Guid id, Guid? actorId, string? reason)
        {
            var doc = await _repo.GetAsync(id);
            if (doc == null) return false;

            // Nếu đang chờ duyệt thì không cần đổi
            if (string.Equals(doc.Status, "PENDING_APPROVAL", StringComparison.OrdinalIgnoreCase)
                || string.Equals(doc.Status, "Chờ duyệt", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Chỉ cho restore nếu đang ở trạng thái "đã xóa"
            if (!string.Equals(doc.Status, "DELETED", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(doc.Status, "Đã xóa", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            doc.Status = "PENDING_APPROVAL";
            await _repo.AddLogAsync(id, "REQUEST_RESTORE", actorId, reason ?? "Yêu cầu hiển thị lại tài liệu");
            await _repo.SaveChangesAsync();
            return true;
        }

        public Task<List<DocumentActionLogDto>> GetLogsAsync(Guid id)
        {
            return _repo.GetLogsAsync(id);
        }

        public async Task<IEnumerable<DocumentVersion>> GetAllVersionsAsync(Guid documentId)
        {
            return await _repo.DocumentVersions()
                .Include(v => v.File)
                .Where(v => v.DocumentId == documentId)
                .OrderByDescending(v => v.VersionNo)
                .ToListAsync();
        }

        public async Task<IEnumerable<ResidentDocumentDto>> GetResidentDocumentsAsync(DocumentQueryDto dto)
        {
            var allowedScopes = new[]
            {
                VisibilityScope.Public.ToString(),
                VisibilityScope.Resident.ToString(),
            };

            var rows = await _repo.GetLatestVersionsAsync();
            rows = rows
                .Where(x => (x.doc.Status == "ACTIVE" || x.doc.Status == "Hoạt động")
                    && (string.IsNullOrEmpty(x.doc.VisibilityScope) || allowedScopes.Contains(x.doc.VisibilityScope)))
                .ToList();

            if (!string.IsNullOrWhiteSpace(dto.Title))
            {
                rows = rows.Where(x => x.doc.Title.Contains(dto.Title, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(dto.Category))
            {
                rows = rows.Where(x => x.doc.Category == dto.Category).ToList();
            }

            // Query riêng để lấy currentVersion nếu cần
            var result = new List<ResidentDocumentDto>();
            foreach (var x in rows)
            {
                // Mặc định dùng latestVersion
                Guid fileId = x.ver.FileId;
                string originalFileName = x.file.OriginalName;
                string mimeType = x.file.MimeType;
                string? versionNote = x.ver.Note;
                DateTime changedAt = x.ver.ChangedAt;

                // Nếu có currentVersion, luôn dùng currentVersion (không phải latestVersion)
                if (x.doc.CurrentVersion.HasValue)
                {
                    var currentVer = await _repo.GetVersionAsync(x.doc.DocumentId, x.doc.CurrentVersion.Value);
                    if (currentVer != null && currentVer.File != null)
                    {
                        fileId = currentVer.FileId;
                        originalFileName = currentVer.File.OriginalName;
                        mimeType = currentVer.File.MimeType;
                        versionNote = currentVer.Note;
                        changedAt = currentVer.ChangedAt;
                    }
                }

                result.Add(new ResidentDocumentDto
                {
                    DocumentId = x.doc.DocumentId,
                    Category = x.doc.Category,
                    Title = x.doc.Title,
                    VisibilityScope = x.doc.VisibilityScope,
                    ChangedAt = changedAt,
                    CreatedBy = x.doc.CreatedBy,
                    LatestVersionNo = x.ver.VersionNo,
                    CurrentVersion = x.doc.CurrentVersion,
                    FileId = fileId,
                    VersionNote = versionNote,
                    OriginalFileName = originalFileName,
                    MimeType = mimeType
                });
            }
            return result;
        }
        public async Task<IEnumerable<ResidentDocumentDto>> GetReceptionistDocumentsAsync(DocumentQueryDto dto)
        {
            var allowedScopes = new[]
            {
                VisibilityScope.Public.ToString(),
                VisibilityScope.Receptionist.ToString()
            };

            var rows = await _repo.GetLatestVersionsAsync();
            rows = rows
                .Where(x => (x.doc.Status == "ACTIVE" || x.doc.Status == "Hoạt động")
                    && (string.IsNullOrEmpty(x.doc.VisibilityScope) || allowedScopes.Contains(x.doc.VisibilityScope)))
                .ToList();

            if (!string.IsNullOrWhiteSpace(dto.Title))
            {
                rows = rows.Where(x =>
                    x.doc.Title.Contains(dto.Title, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(dto.Category))
            {
                rows = rows.Where(x => x.doc.Category == dto.Category).ToList();
            }

            // Query riêng để lấy currentVersion nếu cần
            var result = new List<ResidentDocumentDto>();
            foreach (var x in rows)
            {
                // Mặc định dùng latestVersion
                Guid fileId = x.ver.FileId;
                string originalFileName = x.file.OriginalName;
                string mimeType = x.file.MimeType;
                string? versionNote = x.ver.Note;
                DateTime changedAt = x.ver.ChangedAt;

                // Nếu có currentVersion, luôn dùng currentVersion (không phải latestVersion)
                if (x.doc.CurrentVersion.HasValue)
                {
                    var currentVer = await _repo.GetVersionAsync(x.doc.DocumentId, x.doc.CurrentVersion.Value);
                    if (currentVer != null && currentVer.File != null)
                    {
                        fileId = currentVer.FileId;
                        originalFileName = currentVer.File.OriginalName;
                        mimeType = currentVer.File.MimeType;
                        versionNote = currentVer.Note;
                        changedAt = currentVer.ChangedAt;
                    }
                }

                result.Add(new ResidentDocumentDto
                {
                    DocumentId = x.doc.DocumentId,
                    Category = x.doc.Category,
                    Title = x.doc.Title,
                    VisibilityScope = x.doc.VisibilityScope,
                    ChangedAt = changedAt,
                    CreatedBy = x.doc.CreatedBy,
                    LatestVersionNo = x.ver.VersionNo,
                    CurrentVersion = x.doc.CurrentVersion,
                    FileId = fileId,
                    VersionNote = versionNote,
                    OriginalFileName = originalFileName,
                    MimeType = mimeType
                });
            }
            return result;
        }

        public async Task<IEnumerable<ResidentDocumentDto>> GetAccountingDocumentsAsync(DocumentQueryDto dto)
        {
            var allowedScopes = new[]
            {
                VisibilityScope.Public.ToString(),
                VisibilityScope.Accounting.ToString()
            };

            var rows = await _repo.GetLatestVersionsAsync();
            rows = rows
                .Where(x => (x.doc.Status == "ACTIVE" || x.doc.Status == "Hoạt động")
                    && (string.IsNullOrEmpty(x.doc.VisibilityScope) || allowedScopes.Contains(x.doc.VisibilityScope)))
                .ToList();

            if (!string.IsNullOrWhiteSpace(dto.Title))
            {
                rows = rows.Where(x =>
                    x.doc.Title.Contains(dto.Title, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(dto.Category))
            {
                rows = rows.Where(x => x.doc.Category == dto.Category).ToList();
            }

            // Query riêng để lấy currentVersion nếu cần
            var result = new List<ResidentDocumentDto>();
            foreach (var x in rows)
            {
                // Lấy fileId từ currentVersion nếu có, nếu không thì dùng latestVersion
                Guid fileId = x.ver.FileId;
                string originalFileName = x.file.OriginalName;
                string mimeType = x.file.MimeType;
                string? versionNote = x.ver.Note;
                DateTime changedAt = x.ver.ChangedAt;

                if (x.doc.CurrentVersion.HasValue && x.doc.CurrentVersion.Value != x.ver.VersionNo)
                {
                    // Nếu có currentVersion và khác latestVersion, query riêng để lấy fileId từ currentVersion
                    var currentVer = await _repo.GetVersionAsync(x.doc.DocumentId, x.doc.CurrentVersion.Value);
                    if (currentVer != null && currentVer.File != null)
                    {
                        fileId = currentVer.FileId;
                        originalFileName = currentVer.File.OriginalName;
                        mimeType = currentVer.File.MimeType;
                        versionNote = currentVer.Note;
                        changedAt = currentVer.ChangedAt;
                    }
                }

                result.Add(new ResidentDocumentDto
                {
                    DocumentId = x.doc.DocumentId,
                    Category = x.doc.Category,
                    Title = x.doc.Title,
                    VisibilityScope = x.doc.VisibilityScope,
                    ChangedAt = changedAt,
                    CreatedBy = x.doc.CreatedBy,
                    LatestVersionNo = x.ver.VersionNo,
                    CurrentVersion = x.doc.CurrentVersion,
                    FileId = fileId,
                    VersionNote = versionNote,
                    OriginalFileName = originalFileName,
                    MimeType = mimeType
                });
            }
            return result;
        }

    }
}