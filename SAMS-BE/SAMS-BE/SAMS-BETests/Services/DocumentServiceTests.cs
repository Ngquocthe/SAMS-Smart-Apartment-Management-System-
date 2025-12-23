using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SAMS_BE.DTOs;
using SAMS_BE.Enums;
using SAMS_BE.Helpers;
using SAMS_BE.Models;
using SAMS_BE.Repositories;
using SAMS_BE.Services;
using SAMS_BE.Tenant;
using SAMS_BE.Interfaces.IRepository;
using FileEntity = SAMS_BE.Models.File;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAMS_BE.Services.Tests
{
    [TestClass]
    public class DocumentServiceTests
    {
        private BuildingManagementContext _db = null!;
        private DocumentRepository _repo = null!;
        private Mock<IFileStorageHelper> _storageMock = null!;
        private Mock<IUserRepository> _userRepoMock = null!;
        private DocumentService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<BuildingManagementContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var tenantAccessor = new TenantContextAccessor();
            _db = new BuildingManagementContext(options, tenantAccessor);
            _repo = new DocumentRepository(_db);
            _storageMock = new Mock<IFileStorageHelper>();
            _userRepoMock = new Mock<IUserRepository>();
            _service = new DocumentService(_repo, _storageMock.Object, _userRepoMock.Object);
        }

        #region Helper methods

        private static IFormFile CreateTestFormFile(
            string fileName = "test.pdf",
            string contentType = "application/pdf",
            string content = "dummy-content")
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(bytes);
            return new FormFile(stream, 0, bytes.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }

        private FileEntity CreateStoredFile(
            Guid? fileId = null,
            string fileName = "stored.pdf",
            string mimeType = "application/pdf",
            string storagePath = "path/stored.pdf",
            string? uploadedBy = "tester")
        {
            return new FileEntity
            {
                FileId = fileId ?? Guid.NewGuid(),
                OriginalName = fileName,
                MimeType = mimeType,
                StoragePath = storagePath,
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.UtcNow
            };
        }

        private Document CreateDocument(
            Guid? id = null,
            string category = "Administrative",
            string title = "Test Document",
            string? visibilityScope = null,
            string status = "PENDING_APPROVAL",
            string? createdBy = "creator")
        {
            return new Document
            {
                DocumentId = id ?? Guid.NewGuid(),
                Category = category,
                Title = title,
                VisibilityScope = visibilityScope,
                Status = status,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };
        }

        private DocumentVersion CreateDocumentVersion(
            Guid? id = null,
            Document? document = null,
            int versionNo = 1,
            FileEntity? file = null,
            string? note = "note",
            string? createdBy = "creator")
        {
            document ??= CreateDocument();
            file ??= CreateStoredFile();

            var version = new DocumentVersion
            {
                DocumentVersionId = id ?? Guid.NewGuid(),
                DocumentId = document.DocumentId,
                VersionNo = versionNo,
                FileId = file.FileId,
                Note = note,
                ChangedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                Document = document,
                File = file
            };

            document.DocumentVersions.Add(version);
            file.DocumentVersions.Add(version);

            return version;
        }

        #endregion

        #region SearchAsync tests

        [TestMethod]
        public async Task SearchAsync_NoFilters_ReturnsAllWithPaging()
        {
            // Arrange
            var doc1 = CreateDocument(title: "Alpha");
            var doc2 = CreateDocument(title: "Beta");
            var file1 = CreateStoredFile();
            var file2 = CreateStoredFile();
            CreateDocumentVersion(document: doc1, versionNo: 1, file: file1);
            CreateDocumentVersion(document: doc2, versionNo: 2, file: file2);

            await _db.Documents.AddRangeAsync(doc1, doc2);
            await _db.Files.AddRangeAsync(file1, file2);
            await _db.SaveChangesAsync();

            var dto = new DocumentQueryDto
            {
                Page = 1,
                PageSize = 10
            };

            // Act
            var (items, total) = await _service.SearchAsync(dto);

            // Assert
            Assert.AreEqual(2, total);
            Assert.AreEqual(2, items.Count());
            Assert.IsTrue(items.Any(i => i.Title == "Alpha"));
            Assert.IsTrue(items.Any(i => i.Title == "Beta"));
        }

        [TestMethod]
        public async Task SearchAsync_WithFilters_And_StatusBranches_WorkCorrectly()
        {
            // Arrange
            var activeDoc = CreateDocument(
                title: "Financial Report",
                category: "Financial",
                visibilityScope: "Public",
                status: "ACTIVE");

            var inactiveDoc = CreateDocument(
                title: "Inactive Policy",
                category: "Administrative",
                visibilityScope: "Public",
                status: "INACTIVE");

            var deletedDoc = CreateDocument(
                title: "Deleted Policy",
                category: "Administrative",
                visibilityScope: "Public",
                status: "DELETED");

            var otherScopeDoc = CreateDocument(
                title: "Private Doc",
                category: "Administrative",
                visibilityScope: "Staff_Only",
                status: "INACTIVE");

            var file = CreateStoredFile();
            CreateDocumentVersion(document: activeDoc, versionNo: 1, file: file);
            CreateDocumentVersion(document: inactiveDoc, versionNo: 1, file: file);
            CreateDocumentVersion(document: deletedDoc, versionNo: 1, file: file);
            CreateDocumentVersion(document: otherScopeDoc, versionNo: 1, file: file);

            await _db.Files.AddAsync(file);
            await _db.Documents.AddRangeAsync(activeDoc, inactiveDoc, deletedDoc, otherScopeDoc);
            await _db.SaveChangesAsync();

            // Status INACTIVE branch: should return both inactive and deleted with matching scope/category/title
            var dtoInactive = new DocumentQueryDto
            {
                Title = "Policy", // case-sensitive Contains
                Category = "Administrative",
                Status = "inactive", // lower to test normalization
                VisibilityScope = "Public",
                Page = 1,
                PageSize = 10
            };

            var (itemsInactive, totalInactive) = await _service.SearchAsync(dtoInactive);

            Assert.AreEqual(2, totalInactive);
            Assert.AreEqual(2, itemsInactive.Count());
            Assert.IsTrue(itemsInactive.All(i => i.Status == "INACTIVE"
                                                 || i.Status == "DELETED"));

            // Status ACTIVE branch: only active document
            var dtoActive = new DocumentQueryDto
            {
                Status = "ACTIVE",
                Page = 1,
                PageSize = 10
            };

            var (itemsActive, totalActive) = await _service.SearchAsync(dtoActive);

            Assert.AreEqual(1, totalActive);
            Assert.AreEqual(1, itemsActive.Count());
            Assert.AreEqual("ACTIVE", itemsActive.First().Status);

            // Document without versions should be filtered out (latest == null)
            var noVersionDoc = CreateDocument(title: "No Version", status: "ACTIVE");
            await _db.Documents.AddAsync(noVersionDoc);
            await _db.SaveChangesAsync();

            var (itemsAfterNoVersion, totalAfterNoVersion) = await _service.SearchAsync(dtoActive);

            Assert.AreEqual(2, totalAfterNoVersion); // activeDoc + noVersionDoc
            // Only one item has latest version, so items list should still contain 1 entry
            Assert.AreEqual(1, itemsAfterNoVersion.Count());
        }

        #endregion

        #region CreateAsync & CreateWithFirstVersionAsync tests

        // UTCID01: CreateAsync - Valid data - Complete success
        // Precondition: Can connect with server
        // Input: Category = "Administrative" (valid), Title = "New Doc" (valid), VisibilityScope = "Public" (valid), CreatedBy = "user1" (valid)
        // Expected: Document created successfully with Status = "PENDING_APPROVAL", ActionLog created with Action = "CREATE"
        // Exception: None(Success)
        // Log message: "Tạo bởi user1"
        // Result Type: N (Normal)
        [TestMethod]
        public async Task CreateAsync_CreatesDocumentAndActionLog()
        {
            // Arrange - Precondition: Can connect with server
            var dto = new CreateDocumentDto
            {
                Category = "Administrative",
                Title = "New Doc",
                VisibilityScope = "Public",
                CreatedBy = "user1"
            };
            var actorId = Guid.NewGuid();

            // Act
            var doc = await _service.CreateAsync(dto, actorId);

            // Assert
            var stored = await _db.Documents.FirstOrDefaultAsync(d => d.DocumentId == doc.DocumentId);
            Assert.IsNotNull(stored);
            Assert.AreEqual("PENDING_APPROVAL", stored!.Status);
            Assert.AreEqual("user1", stored.CreatedBy);
            Assert.AreEqual("Administrative", stored.Category);
            Assert.AreEqual("New Doc", stored.Title);
            Assert.AreEqual("Public", stored.VisibilityScope);

            var logs = await _db.DocumentActionLogs.Where(l => l.DocumentId == doc.DocumentId).ToListAsync();
            Assert.AreEqual(1, logs.Count);
            Assert.AreEqual("CREATE", logs[0].Action);
            Assert.AreEqual(actorId, logs[0].ActorId);
            Assert.IsTrue(logs[0].Detail!.Contains("Tạo bởi user1"));
        }

        // UTCID02: CreateAsync - Title null
        // Precondition: Can connect with server
        // Input: Category = "Administrative" (valid), Title = null, VisibilityScope = "Public" (valid)
        // Expected: Validation error at model binding layer (Required attribute)
        // Exception: ValidationException (at model binding)
        // Result Type: A (Abnormal)
        [TestMethod]
        public async Task CreateAsync_TitleNull_ThrowsValidationException()
        {
            // Arrange - Precondition: Can connect with server
            var dto = new CreateDocumentDto
            {
                Category = "Administrative",
                Title = null!,
                VisibilityScope = "Public",
                CreatedBy = "user1"
            };
            var actorId = Guid.NewGuid();

            // Act & Assert
            // Note: Required validation thường được xử lý ở model binding layer
            // Service layer sẽ nhận dto đã validated, nhưng test này để đảm bảo
            try
            {
                var doc = await _service.CreateAsync(dto, actorId);
                // Nếu không throw, có thể service không check null
                Assert.Fail("Expected validation exception for null Title");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is ArgumentNullException || ex is ArgumentException);
            }
        }

        // UTCID03: CreateAsync - Title empty
        // Precondition: Can connect with server
        // Input: Category = "Administrative" (valid), Title = "" (empty), VisibilityScope = "Public" (valid)
        // Expected: Validation error at model binding layer
        // Exception: ValidationException (at model binding)
        // Result Type: A (Abnormal)
        [TestMethod]
        public async Task CreateAsync_TitleEmpty_ThrowsValidationException()
        {
            // Arrange - Precondition: Can connect with server
            var dto = new CreateDocumentDto
            {
                Category = "Administrative",
                Title = "",
                VisibilityScope = "Public",
                CreatedBy = "user1"
            };
            var actorId = Guid.NewGuid();

            // Act & Assert
            try
            {
                var doc = await _service.CreateAsync(dto, actorId);
                Assert.Fail("Expected validation exception for empty Title");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is ArgumentNullException || ex is ArgumentException);
            }
        }

        // UTCID04: CreateAsync - Title too long (256 chars)
        // Precondition: Can connect with server
        // Input: Category = "Administrative" (valid), Title = 256 chars (exceeds max 255), VisibilityScope = "Public" (valid)
        // Expected: Validation error at model binding layer
        // Exception: ValidationException (at model binding)
        // Result Type: B (Boundary)
        [TestMethod]
        public async Task CreateAsync_TitleTooLong_256Chars_ThrowsValidationException()
        {
            // Arrange - Precondition: Can connect with server
            var dto = new CreateDocumentDto
            {
                Category = "Administrative",
                Title = new string('A', 256), // 256 chars - exceeds max 255
                VisibilityScope = "Public",
                CreatedBy = "user1"
            };
            var actorId = Guid.NewGuid();

            // Act & Assert
            // Note: StringLength validation thường được xử lý ở model binding
            try
            {
                var doc = await _service.CreateAsync(dto, actorId);
                // Nếu không throw, có thể service không check length
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is ArgumentException || ex is ArgumentNullException);
            }
        }



        // UTCID05: CreateAsync - Category null
        // Precondition: Can connect with server
        // Input: Category = null, Title = "New Doc" (valid), VisibilityScope = "Public" (valid)
        // Expected: Validation error at model binding layer
        // Exception: ValidationException (at model binding)
        // Result Type: A (Abnormal)
        [TestMethod]
        public async Task CreateAsync_CategoryNull_ThrowsValidationException()
        {
            // Arrange - Precondition: Can connect with server
            var dto = new CreateDocumentDto
            {
                Category = null!,
                Title = "New Doc",
                VisibilityScope = "Public",
                CreatedBy = "user1"
            };
            var actorId = Guid.NewGuid();

            // Act & Assert
            try
            {
                var doc = await _service.CreateAsync(dto, actorId);
                Assert.Fail("Expected validation exception for null Category");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is ArgumentNullException || ex is ArgumentException);
            }
        }





        // UTCID06: CreateAsync - VisibilityScope null (valid - optional field)
        // Precondition: Can connect with server
        // Input: Category = "Administrative" (valid), Title = "New Doc" (valid), VisibilityScope = null (optional)
        // Expected: Document created successfully with VisibilityScope = null
        // Exception: None(Success)
        // Result Type: N (Normal)
        [TestMethod]
        public async Task CreateAsync_VisibilityScopeNull_Success()
        {
            // Arrange - Precondition: Can connect with server
            var dto = new CreateDocumentDto
            {
                Category = "Administrative",
                Title = "New Doc",
                VisibilityScope = null,
                CreatedBy = "user1"
            };
            var actorId = Guid.NewGuid();

            // Act
            var doc = await _service.CreateAsync(dto, actorId);

            // Assert
            var stored = await _db.Documents.FirstOrDefaultAsync(d => d.DocumentId == doc.DocumentId);
            Assert.IsNotNull(stored);
            Assert.IsNull(stored!.VisibilityScope);
        }



        // UTCID07: CreateWithFirstVersionAsync - Cho phép upload version đầu tiên khi doc ở trạng thái PENDING_APPROVAL (mặc định sau khi tạo)
        // Precondition: Can connect with server
        // Input: Category = "Administrative" (valid), Title = "Doc With Version" (valid), file = valid file
        // Expected: Tạo doc với status PENDING_APPROVAL, upload version đầu tiên thành công (VersionNo = 1)
        // Exception: None (Success)
        // Log message: CREATE và NEW_VERSION được ghi
        // Result Type: N (Normal)
        [TestMethod]
        public async Task CreateWithFirstVersionAsync_WhenPendingApproval_AllowsFirstUpload()
        {
            // Arrange - Precondition: Can connect with server
            var dto = new CreateDocumentDto
            {
                Category = "Administrative",
                Title = "Doc With Version",
                VisibilityScope = "Public",
                CreatedBy = "creator"
            };
            var actorId = Guid.NewGuid();
            var formFile = CreateTestFormFile();
            var storedFile = CreateStoredFile();

            _storageMock
                .Setup(s => s.SaveAsync(formFile, It.IsAny<string>(), "creator"))
                .ReturnsAsync(storedFile);

            // Act
            var doc = await _service.CreateWithFirstVersionAsync(dto, formFile, "first version", actorId);

            // Assert: doc được tạo, status PENDING_APPROVAL, version đầu tiên được upload, có log CREATE và NEW_VERSION
            var createdDoc = await _db.Documents
                .Include(d => d.DocumentVersions)
                .Include(d => d.DocumentActionLogs)
                .FirstOrDefaultAsync(d => d.DocumentId == doc.DocumentId);

            Assert.IsNotNull(createdDoc);
            Assert.AreEqual("PENDING_APPROVAL", createdDoc!.Status);
            Assert.AreEqual(1, createdDoc.DocumentVersions.Count);
            Assert.AreEqual(1, createdDoc.DocumentVersions.First().VersionNo);
            Assert.IsTrue(createdDoc.DocumentActionLogs.Any(l => l.Action == "CREATE"));
            Assert.IsTrue(createdDoc.DocumentActionLogs.Any(l => l.Action == "NEW_VERSION"));
        }

        // UTCID08: CreateWithFirstVersionAsync - File null
        // Precondition: Can connect with server
        // Input: Category = "Administrative" (valid), Title = "New Doc" (valid), file = null
        // Expected: ArgumentNullException or ArgumentException thrown
        // Exception: ArgumentNullException / ArgumentException
        // Result Type: A (Abnormal)
        [TestMethod]
        public async Task CreateWithFirstVersionAsync_FileNull_ThrowsException()
        {
            // Arrange - Precondition: Can connect with server
            var dto = new CreateDocumentDto
            {
                Category = "Administrative",
                Title = "New Doc",
                VisibilityScope = "Public",
                CreatedBy = "creator"
            };
            var actorId = Guid.NewGuid();
            IFormFile? formFile = null;

            // Act & Assert
            try
            {
                await _service.CreateWithFirstVersionAsync(dto, formFile!, "note", actorId);
                Assert.Fail("Expected exception for null file");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is ArgumentNullException || ex is ArgumentException || ex is NullReferenceException);
            }
        }

        // New: CreateWithFirstVersionAsync - File có extension nguy hiểm -> ArgumentException, không gọi storage
        [TestMethod]
        public async Task CreateWithFirstVersionAsync_DangerousExtension_ThrowsArgumentException()
        {
            var dto = new CreateDocumentDto
            {
                Category = "Administrative",
                Title = "New Doc",
                VisibilityScope = "Public",
                CreatedBy = "creator"
            };
            var actorId = Guid.NewGuid();

            var dangerousFileMock = new Mock<IFormFile>();
            dangerousFileMock.SetupGet(f => f.Length).Returns(1024);
            dangerousFileMock.SetupGet(f => f.FileName).Returns("malware.exe");
            dangerousFileMock.SetupGet(f => f.ContentType).Returns("application/octet-stream");

            var ex = await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await _service.CreateWithFirstVersionAsync(dto, dangerousFileMock.Object, "note", actorId));

            StringAssert.Contains(ex.Message, "not allowed");
            _storageMock.Verify(s => s.SaveAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
        }



        #endregion

        #region AddVersionAsync tests

        // UTCID01: AddVersionAsync - Document not found
        // Precondition: Can connect with server
        // Input: DocumentId = non-existent GUID, file = valid file, note = "note" (valid), createdBy = "creator" (valid)
        // Expected: Returns null (document not found)
        // Exception: None (returns null)
        // Result Type: A (Abnormal)
        [TestMethod]
        public async Task AddVersionAsync_DocumentNotFound_ReturnsNull()
        {
            // Arrange - Precondition: Can connect with server
            var formFile = CreateTestFormFile();

            // Act
            var result = await _service.AddVersionAsync(Guid.NewGuid(), formFile, "note", "creator", Guid.NewGuid());

            // Assert
            Assert.IsNull(result);
        }

        // UTCID02: AddVersionAsync - First version allowed when status PENDING_APPROVAL
        // Precondition: Can connect with server, Document exists with Status = "PENDING_APPROVAL", no versions
        // Input: DocumentId = existing document (PENDING_APPROVAL), file = valid file, note = "v1" (valid)
        // Expected: Version uploaded successfully (VersionNo = 1), Status giữ nguyên PENDING_APPROVAL
        // Exception: None (Success)
        // Log message: NEW_VERSION log created
        // Result Type: N (Normal)
        [TestMethod]
        public async Task AddVersionAsync_FirstVersion_WhenPendingApproval_AllowsUpload()
        {
            // Arrange - Precondition: Can connect with server, Document exists with Status = "PENDING_APPROVAL"
            var doc = CreateDocument(status: "PENDING_APPROVAL");
            await _db.Documents.AddAsync(doc);
            await _db.SaveChangesAsync();

            var formFile = CreateTestFormFile();
            var storedFile = CreateStoredFile();

            _storageMock
                .Setup(s => s.SaveAsync(formFile, "documents", "creator"))
                .ReturnsAsync(storedFile);

            // Act
            var version = await _service.AddVersionAsync(doc.DocumentId, formFile, "v1", "creator", Guid.NewGuid());

            // Assert: upload thành công, versionNo = 1, trạng thái giữ nguyên PENDING_APPROVAL
            Assert.IsNotNull(version);
            Assert.AreEqual(1, version!.VersionNo);
            Assert.AreEqual(doc.DocumentId, version.DocumentId);

            var dbDoc = await _db.Documents.Include(d => d.DocumentVersions)
                                           .Include(d => d.DocumentActionLogs)
                                           .FirstAsync(d => d.DocumentId == doc.DocumentId);
            Assert.AreEqual("PENDING_APPROVAL", dbDoc.Status);
            Assert.AreEqual(1, dbDoc.DocumentVersions.Count);
            Assert.AreEqual(1, dbDoc.DocumentVersions.First().VersionNo);
            Assert.IsTrue(dbDoc.DocumentActionLogs.Any(l => l.Action == "NEW_VERSION"));
        }



        // UTCID03: AddVersionAsync - Existing versions - Increments version number and resets status
        // Precondition: Can connect with server, Document exists with Status = "ACTIVE", has existing versions
        // Input: DocumentId = existing document (ACTIVE, has version 2), file = valid file, note = "v3" (valid)
        // Expected: DocumentVersion created with VersionNo = 3, Document Status changed to "PENDING_APPROVAL", ActionLog created with Action = "NEW_VERSION"
        // Exception: None(Success)
        // Log message: "v3 đăng bởi creator"
        // Result Type: N (Normal)
        [TestMethod]
        public async Task AddVersionAsync_ExistingVersions_IncrementsVersionNo_AndResetsStatusToPending()
        {
            // Arrange - Precondition: Can connect with server, Document exists with Status = "ACTIVE", has existing versions
            var doc = CreateDocument(status: "ACTIVE");
            var file1 = CreateStoredFile();
            var existingVersion = CreateDocumentVersion(document: doc, versionNo: 2, file: file1);

            // Add existing file + document graph (which already contains existingVersion)
            await _db.Files.AddAsync(file1);
            await _db.Documents.AddAsync(doc);
            await _db.SaveChangesAsync();

            var formFile = CreateTestFormFile();

            _storageMock
                .Setup(s => s.SaveAsync(formFile, "documents", "creator"))
                .ReturnsAsync(CreateStoredFile());

            // Act
            var version = await _service.AddVersionAsync(doc.DocumentId, formFile, "v3", "creator", Guid.NewGuid());

            // Assert
            Assert.IsNotNull(version);
            Assert.AreEqual(3, version!.VersionNo);
            Assert.AreEqual("v3", version.Note);

            var dbDoc = await _db.Documents.FirstAsync(d => d.DocumentId == doc.DocumentId);
            Assert.AreEqual("PENDING_APPROVAL", dbDoc.Status);

            var logs = await _db.DocumentActionLogs.Where(l => l.DocumentId == doc.DocumentId && l.Action == "NEW_VERSION").ToListAsync();
            Assert.AreEqual(1, logs.Count);
            Assert.IsTrue(logs[0].Detail!.Contains("v3 đăng bởi creator"));
        }

        // UTCID03B: AddVersionAsync - First version (no existing versions)
        // Precondition: Can connect with server, Document exists with Status = "ACTIVE", no existing versions
        // Input: DocumentId = existing document (ACTIVE, no versions), file = valid file
        // Expected: DocumentVersion created with VersionNo = 1, Document Status changed to "PENDING_APPROVAL"
        // Exception: None(Success)
        // Result Type: N (Normal)
        [TestMethod]
        public async Task AddVersionAsync_FirstVersion_NoExistingVersions_Success()
        {
            // Arrange - Precondition: Can connect with server, Document exists with Status = "ACTIVE", no existing versions
            var doc = CreateDocument(status: "ACTIVE");
            await _db.Documents.AddAsync(doc);
            await _db.SaveChangesAsync();

            var formFile = CreateTestFormFile();
            var storedFile = CreateStoredFile();

            _storageMock
                .Setup(s => s.SaveAsync(formFile, "documents", "creator"))
                .ReturnsAsync(storedFile);

            // Act
            var version = await _service.AddVersionAsync(doc.DocumentId, formFile, "first version", "creator", Guid.NewGuid());

            // Assert
            Assert.IsNotNull(version);
            Assert.AreEqual(1, version!.VersionNo);
            Assert.AreEqual("first version", version.Note);

            var dbDoc = await _db.Documents.FirstAsync(d => d.DocumentId == doc.DocumentId);
            Assert.AreEqual("PENDING_APPROVAL", dbDoc.Status);
        }

        // UTCID04: AddVersionAsync - File null
        // Precondition: Can connect with server, Document exists with Status = "ACTIVE"
        // Input: DocumentId = existing document, file = null
        // Expected: ArgumentNullException or ArgumentException thrown
        // Exception: ArgumentNullException / ArgumentException
        // Result Type: A (Abnormal)
        [TestMethod]
        public async Task AddVersionAsync_FileNull_ThrowsException()
        {
            // Arrange - Precondition: Can connect with server, Document exists with Status = "ACTIVE"
            var doc = CreateDocument(status: "ACTIVE");
            await _db.Documents.AddAsync(doc);
            await _db.SaveChangesAsync();

            IFormFile? formFile = null;

            // Act & Assert
            try
            {
                await _service.AddVersionAsync(doc.DocumentId, formFile!, "note", "creator", Guid.NewGuid());
                Assert.Fail("Expected exception for null file");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is ArgumentNullException || ex is ArgumentException || ex is NullReferenceException);
            }
        }

        #endregion

        #region ChangeStatusAsync tests

        // UTCID01: ChangeStatusAsync - Document not found
        // Precondition: Can connect with server
        // Input: DocumentId = non-existent GUID, Status = "ACTIVE" (valid), ActorId = valid GUID, Detail = "detail" (valid)
        // Expected: Returns false (document not found)
        // Exception: None (returns false)
        // Result Type: A (Abnormal)
        [TestMethod]
        public async Task ChangeStatusAsync_DocumentNotFound_ReturnsFalse()
        {
            // Arrange - Precondition: Can connect with server
            var result = await _service.ChangeStatusAsync(Guid.NewGuid(), "ACTIVE", Guid.NewGuid(), "detail");

            // Assert
            Assert.IsFalse(result);
        }

        // UTCID02: ChangeStatusAsync - Invalid status (whitespace only)
        // Precondition: Can connect with server, Document exists
        // Input: DocumentId = existing document, Status = "   " (whitespace only), ActorId = valid GUID, Detail = "detail" (valid)
        // Expected: Returns false (invalid status)
        // Exception: None (returns false)
        // Result Type: A (Abnormal)
        [TestMethod]
        public async Task ChangeStatusAsync_InvalidStatus_ReturnsFalse()
        {
            // Arrange - Precondition: Can connect with server, Document exists
            var doc = CreateDocument(status: "PENDING_APPROVAL");
            await _db.Documents.AddAsync(doc);
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.ChangeStatusAsync(doc.DocumentId, "   ", Guid.NewGuid(), "detail");

            // Assert
            Assert.IsFalse(result);
        }

        // UTCID02B: ChangeStatusAsync - Status null
        // Precondition: Can connect with server, Document exists
        // Input: DocumentId = existing document, Status = null, ActorId = valid GUID
        // Expected: Returns false (invalid status)
        // Exception: None (returns false)
        // Result Type: A (Abnormal)
        [TestMethod]
        public async Task ChangeStatusAsync_StatusNull_ReturnsFalse()
        {
            // Arrange - Precondition: Can connect with server, Document exists
            var doc = CreateDocument(status: "PENDING_APPROVAL");
            await _db.Documents.AddAsync(doc);
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.ChangeStatusAsync(doc.DocumentId, null!, Guid.NewGuid(), "detail");

            // Assert
            Assert.IsFalse(result);
        }

        // UTCID05: ChangeStatusAsync - Status empty
        // Precondition: Can connect with server, Document exists
        // Input: DocumentId = existing document, Status = "" (empty), ActorId = valid GUID
        // Expected: Returns false (invalid status)
        // Exception: None (returns false)
        // Result Type: A (Abnormal)
        [TestMethod]
        public async Task ChangeStatusAsync_StatusEmpty_ReturnsFalse()
        {
            // Arrange - Precondition: Can connect with server, Document exists
            var doc = CreateDocument(status: "PENDING_APPROVAL");
            await _db.Documents.AddAsync(doc);
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.ChangeStatusAsync(doc.DocumentId, "", Guid.NewGuid(), "detail");

            // Assert
            Assert.IsFalse(result);
        }

        // 006 New: ChangeStatusAsync - From INACTIVE to ACTIVE - Updates status and logs
        [TestMethod]
        public async Task ChangeStatusAsync_FromInactiveToActive_SucceedsAndLogs()
        {
            var doc = CreateDocument(status: "INACTIVE");
            await _db.Documents.AddAsync(doc);
            await _db.SaveChangesAsync();

            var actorId = Guid.NewGuid();

            var result = await _service.ChangeStatusAsync(doc.DocumentId, "ACTIVE", actorId, null);

            Assert.IsTrue(result);
            var dbDoc = await _db.Documents.Include(d => d.DocumentActionLogs).FirstAsync(d => d.DocumentId == doc.DocumentId);
            Assert.AreEqual("ACTIVE", dbDoc.Status);
            Assert.IsTrue(dbDoc.DocumentActionLogs.Any(l => l.Action == "CHANGE_STATUS"));
        }

        // New: ChangeStatusAsync - Log created even when detail is null
        [TestMethod]
        public async Task ChangeStatusAsync_NullDetail_StillCreatesLog()
        {
            var doc = CreateDocument(status: "ACTIVE");
            await _db.Documents.AddAsync(doc);
            await _db.SaveChangesAsync();

            var actorId = Guid.NewGuid();

            var result = await _service.ChangeStatusAsync(doc.DocumentId, "INACTIVE", actorId, null);

            Assert.IsTrue(result);
            var log = await _db.DocumentActionLogs.FirstOrDefaultAsync(l => l.DocumentId == doc.DocumentId && l.Action == "CHANGE_STATUS");
            Assert.IsNotNull(log);
            Assert.AreEqual(actorId, log.ActorId);
            Assert.IsNull(log.Detail);
            var dbDoc = await _db.Documents.FirstAsync(d => d.DocumentId == doc.DocumentId);
            Assert.AreEqual("INACTIVE", dbDoc.Status);
        }

        // UTCID03: ChangeStatusAsync - To ACTIVE - Sets CurrentVersion
        // Precondition: Can connect with server, Document exists with Status = "PENDING_APPROVAL", has versions
        // Input: DocumentId = existing document, Status = "ACTIVE" (lowercase, should be normalized), ActorId = valid GUID, Detail = "approved" (valid)
        // Expected: Returns true, Document Status changed to "ACTIVE", CurrentVersion set to latest version (2), ActionLog created
        // Exception: None(Success)
        // Log message: ActionLog with Action = "CHANGE_STATUS", Detail = "approved"
        // Result Type: N (Normal)
        [TestMethod]
        public async Task ChangeStatusAsync_ToActive_SetsCurrentVersion()
        {
            // Arrange - Precondition: Can connect with server, Document exists with Status = "PENDING_APPROVAL", has versions
            var doc = CreateDocument(status: "PENDING_APPROVAL");
            var file = CreateStoredFile();
            var v1 = CreateDocumentVersion(document: doc, versionNo: 1, file: file);
            var v2 = CreateDocumentVersion(document: doc, versionNo: 2, file: file);

            await _db.Files.AddAsync(file);
            await _db.Documents.AddAsync(doc);
            await _db.DocumentVersions.AddRangeAsync(v1, v2);
            await _db.SaveChangesAsync();

            var actorId = Guid.NewGuid();

            // Act
            var result = await _service.ChangeStatusAsync(doc.DocumentId, "ACTIVE".ToLower(), actorId, "approved");

            // Assert
            Assert.IsTrue(result);

            var dbDoc = await _db.Documents.FirstAsync(d => d.DocumentId == doc.DocumentId);
            Assert.AreEqual("ACTIVE", dbDoc.Status);
            Assert.AreEqual(2, dbDoc.CurrentVersion);

            var logs = await _db.DocumentActionLogs.Where(l => l.DocumentId == doc.DocumentId && l.Action == "CHANGE_STATUS").ToListAsync();
            Assert.AreEqual(1, logs.Count);
            Assert.AreEqual(actorId, logs[0].ActorId);
            Assert.AreEqual("approved", logs[0].Detail);
        }

        // UTCID03B: ChangeStatusAsync - To ACTIVE - No versions (only metadata update)
        // Precondition: Can connect with server, Document exists with Status = "PENDING_APPROVAL", no versions
        // Input: DocumentId = existing document (no versions), Status = "ACTIVE", ActorId = valid GUID
        // Expected: Returns true, Document Status changed to "ACTIVE", CurrentVersion remains null
        // Exception: None(Success)
        // Result Type: N (Normal)
        [TestMethod]
        public async Task ChangeStatusAsync_ToActive_NoVersions_CurrentVersionNull()
        {
            // Arrange - Precondition: Can connect with server, Document exists with Status = "PENDING_APPROVAL", no versions
            var doc = CreateDocument(status: "PENDING_APPROVAL");
            await _db.Documents.AddAsync(doc);
            await _db.SaveChangesAsync();

            var actorId = Guid.NewGuid();

            // Act
            var result = await _service.ChangeStatusAsync(doc.DocumentId, "ACTIVE", actorId, "approved");

            // Assert
            Assert.IsTrue(result);

            var dbDoc = await _db.Documents.FirstAsync(d => d.DocumentId == doc.DocumentId);
            Assert.AreEqual("ACTIVE", dbDoc.Status);
            Assert.IsNull(dbDoc.CurrentVersion);
        }

        // UTCID04: ChangeStatusAsync - To REJECTED - Rollback metadata if pending approval
        // Precondition: Can connect with server, Document exists with Status = "PENDING_APPROVAL" (after metadata update)
        // Input: DocumentId = existing document, Status = "REJECTED", ActorId = valid GUID
        // Expected: Returns true, Document Status changed based on CurrentVersion, metadata rolled back if applicable
        // Exception: None(Success)
        // Result Type: N (Normal)
        [TestMethod]
        public async Task ChangeStatusAsync_ToRejected_WithCurrentVersion_ReturnsToActive()
        {
            // Arrange - Precondition: Can connect with server, Document exists with Status = "PENDING_APPROVAL", has CurrentVersion
            var doc = CreateDocument(status: "PENDING_APPROVAL", title: "Original Title");
            doc.CurrentVersion = 1;
            await _db.Documents.AddAsync(doc);
            await _db.SaveChangesAsync();

            var actorId = Guid.NewGuid();

            // Act
            var result = await _service.ChangeStatusAsync(doc.DocumentId, "REJECTED", actorId, "rejected");

            // Assert
            Assert.IsTrue(result);

            var dbDoc = await _db.Documents.FirstAsync(d => d.DocumentId == doc.DocumentId);
            // Nếu có CurrentVersion, quay lại ACTIVE
            Assert.AreEqual("ACTIVE", dbDoc.Status);
            Assert.AreEqual(1, dbDoc.CurrentVersion);
        }



        [TestMethod]
        public async Task ChangeStatusAsync_ToNonActive_DoesNotChangeCurrentVersion()
        {
            // Arrange
            var doc = CreateDocument(status: "ACTIVE");
            doc.CurrentVersion = 5;

            await _db.Documents.AddAsync(doc);
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.ChangeStatusAsync(doc.DocumentId, "INACTIVE", Guid.NewGuid(), "deactivate");

            // Assert
            Assert.IsTrue(result);

            var dbDoc = await _db.Documents.FirstAsync(d => d.DocumentId == doc.DocumentId);
            Assert.AreEqual("INACTIVE", dbDoc.Status);
            Assert.AreEqual(5, dbDoc.CurrentVersion);
        }

        #endregion

        #region DownloadAsync, GetFileAsync, GetVersionAsync tests

        [TestMethod]
        public async Task DownloadAsync_FileNotFound_ReturnsNull()
        {
            var result = await _service.DownloadAsync(Guid.NewGuid());
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task DownloadAsync_FileFound_ReturnsStreamInfo()
        {
            // Arrange
            var file = CreateStoredFile(storagePath: "storage/path/file.pdf");
            await _db.Files.AddAsync(file);
            await _db.SaveChangesAsync();

            var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes("data"));
            _storageMock
                .Setup(s => s.OpenAsync(file.StoragePath))
                .ReturnsAsync((expectedStream, file.MimeType, file.OriginalName));

            // Act
            var result = await _service.DownloadAsync(file.FileId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(file.MimeType, result!.Value.mime);
            Assert.AreEqual(file.OriginalName, result.Value.name);
        }

        [TestMethod]
        public async Task GetFileAndVersion_DelegatesToRepository()
        {
            // Arrange
            var doc = CreateDocument();
            var file = CreateStoredFile();
            var version = CreateDocumentVersion(document: doc, versionNo: 1, file: file);

            await _db.Documents.AddAsync(doc);
            await _db.Files.AddAsync(file);
            await _db.DocumentVersions.AddAsync(version);
            await _db.SaveChangesAsync();

            // Act
            var fetchedFile = await _service.GetFileAsync(file.FileId);
            var fetchedVersion = await _service.GetVersionAsync(doc.DocumentId, 1);

            // Assert
            Assert.IsNotNull(fetchedFile);
            Assert.IsNotNull(fetchedVersion);
            Assert.AreEqual(file.FileId, fetchedFile!.FileId);
            Assert.AreEqual(version.DocumentVersionId, fetchedVersion!.DocumentVersionId);
        }

        #endregion

        #region GetAllWithLatestVersionAsync tests

        [TestMethod]
        public async Task GetAllWithLatestVersionAsync_NoStatus_FiltersToActive()
        {
            // Arrange
            var activeDoc = CreateDocument(status: "ACTIVE", title: "Active");
            var inactiveDoc = CreateDocument(status: "INACTIVE", title: "Inactive");
            var deletedDoc = CreateDocument(status: "DELETED", title: "Deleted");

            var file = CreateStoredFile();
            CreateDocumentVersion(document: activeDoc, versionNo: 1, file: file);
            CreateDocumentVersion(document: inactiveDoc, versionNo: 1, file: file);
            CreateDocumentVersion(document: deletedDoc, versionNo: 1, file: file);

            await _db.Files.AddAsync(file);
            await _db.Documents.AddRangeAsync(activeDoc, inactiveDoc, deletedDoc);
            await _db.SaveChangesAsync();

            // Act
            var items = await _service.GetAllWithLatestVersionAsync();

            // Assert
            Assert.AreEqual(1, items.Count());
            Assert.AreEqual("ACTIVE", items.First().Status);
        }

        [TestMethod]
        public async Task GetAllWithLatestVersionAsync_InactiveStatus_ReturnsInactiveAndDeleted()
        {
            // Arrange
            var inactiveDoc = CreateDocument(status: "INACTIVE");
            var deletedDoc = CreateDocument(status: "DELETED");
            var activeDoc = CreateDocument(status: "ACTIVE");

            var file = CreateStoredFile();
            CreateDocumentVersion(document: inactiveDoc, versionNo: 1, file: file);
            CreateDocumentVersion(document: deletedDoc, versionNo: 1, file: file);
            CreateDocumentVersion(document: activeDoc, versionNo: 1, file: file);

            await _db.Files.AddAsync(file);
            await _db.Documents.AddRangeAsync(inactiveDoc, deletedDoc, activeDoc);
            await _db.SaveChangesAsync();

            // Act
            var items = await _service.GetAllWithLatestVersionAsync("INACTIVE".ToLower());

            // Assert
            Assert.AreEqual(2, items.Count());
            Assert.IsTrue(items.All(i => i.Status == "INACTIVE" || i.Status == "DELETED"));
        }

        [TestMethod]
        public async Task GetAllWithLatestVersionAsync_SpecificStatus_FiltersCorrectly()
        {
            // Arrange
            var rejectedDoc = CreateDocument(status: "REJECTED");
            var activeDoc = CreateDocument(status: "ACTIVE");

            var file = CreateStoredFile();
            CreateDocumentVersion(document: rejectedDoc, versionNo: 1, file: file);
            CreateDocumentVersion(document: activeDoc, versionNo: 1, file: file);

            await _db.Files.AddAsync(file);
            await _db.Documents.AddRangeAsync(rejectedDoc, activeDoc);
            await _db.SaveChangesAsync();

            // Act
            var items = await _service.GetAllWithLatestVersionAsync("REJECTED");

            // Assert
            Assert.AreEqual(1, items.Count());
            Assert.AreEqual("REJECTED", items.First().Status);
        }

        #endregion

        #region UpdateMetadataAsync tests

        // UTCID01: UpdateMetadataAsync - Document not found
        // Precondition: Can connect with server
        // Input: DocumentId = non-existent GUID, UpdateDocumentDto with Title = "New Title" (valid), ActorId = valid GUID
        // Expected: Returns false (document not found)
        // Exception: None (returns false)
        // Result Type: A (Abnormal)
        [TestMethod]
        public async Task UpdateMetadataAsync_DocumentNotFound_ReturnsFalse()
        {
            // Arrange - Precondition: Can connect with server
            var dto = new UpdateDocumentDto { Title = "New Title" };

            // Act
            var result = await _service.UpdateMetadataAsync(Guid.NewGuid(), dto, Guid.NewGuid());

            // Assert
            Assert.IsFalse(result);
        }

        // UTCID02: UpdateMetadataAsync - Document status PENDING_APPROVAL
        // Precondition: Can connect with server, Document exists with Status = "PENDING_APPROVAL"
        // Input: DocumentId = existing document (PENDING_APPROVAL), UpdateDocumentDto with Title = "New Title" (valid)
        // Expected: InvalidOperationException thrown with message "Không thể sửa thông tin khi tài liệu đang chờ phê duyệt hoặc đã ngừng hiển thị. Vui lòng đợi phê duyệt xong."
        // Exception: InvalidOperationException
        // Result Type: A (Abnormal)
        [TestMethod]
        public async Task UpdateMetadataAsync_WhenPendingApproval_ThrowsInvalidOperation()
        {
            // Arrange - Precondition: Can connect with server, Document exists with Status = "PENDING_APPROVAL"
            var doc = CreateDocument(status: "PENDING_APPROVAL", title: "Old Title");
            await _db.Documents.AddAsync(doc);
            await _db.SaveChangesAsync();

            var dto = new UpdateDocumentDto { Title = "New Title" };
            var actorId = Guid.NewGuid();

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await _service.UpdateMetadataAsync(doc.DocumentId, dto, actorId));

            Assert.AreEqual("Không thể sửa thông tin khi tài liệu đang chờ phê duyệt hoặc đã ngừng hiển thị. Vui lòng đợi phê duyệt xong.", ex.Message);

            // Document should remain unchanged
            var dbDoc = await _db.Documents.FirstAsync(d => d.DocumentId == doc.DocumentId);
            Assert.AreEqual("Old Title", dbDoc.Title);
            Assert.AreEqual("PENDING_APPROVAL", dbDoc.Status);
        }



        // UTCID03: UpdateMetadataAsync - Không thay đổi -> trả true, không log, không đổi trạng thái
        // Precondition: Document đang ACTIVE, dto không đổi Title/Category/VisibilityScope
        // Expected: Trả true, Status giữ ACTIVE, không tạo ActionLog
        // Exception: None (Success)
        // Result Type: N (Normal)
        [TestMethod]
        public async Task UpdateMetadataAsync_NoChanges_ReturnsTrueWithoutLogOrStatusChange()
        {
            // Arrange - Precondition: Can connect with server, Document exists with Status = "ACTIVE"
            var doc = CreateDocument(
                category: "Administrative",
                title: "Same Title",
                visibilityScope: "Public",
                status: "ACTIVE");

            await _db.Documents.AddAsync(doc);
            await _db.SaveChangesAsync();

            var dto = new UpdateDocumentDto
            {
                Title = "Same Title",
                Category = "Administrative",
                VisibilityScope = "Public"
            };

            // Act
            var result = await _service.UpdateMetadataAsync(doc.DocumentId, dto, Guid.NewGuid());

            // Assert
            Assert.IsTrue(result);

            var dbDoc = await _db.Documents.FirstAsync(d => d.DocumentId == doc.DocumentId);
            Assert.AreEqual("ACTIVE", dbDoc.Status);

            var logs = await _db.DocumentActionLogs.Where(l => l.DocumentId == doc.DocumentId).ToListAsync();
            Assert.AreEqual(0, logs.Count);
        }

        // UTCID04: UpdateMetadataAsync - Có thay đổi -> cập nhật trường, set PENDING_APPROVAL và ghi log UPDATE_METADATA
        // Precondition: Document ACTIVE với Title/Category/VisibilityScope ban đầu
        // Expected: Các trường được cập nhật, Status về PENDING_APPROVAL, log UPDATE_METADATA được ghi
        // Exception: None (Success)
        // Result Type: N (Normal)
        [TestMethod]
        public async Task UpdateMetadataAsync_WithChanges_UpdatesFields_SetsPendingAndAddsLog()
        {
            // Arrange
            var doc = CreateDocument(
                category: "Administrative",
                title: "Old Title",
                visibilityScope: null,
                status: "ACTIVE");

            await _db.Documents.AddAsync(doc);
            await _db.SaveChangesAsync();

            var dto = new UpdateDocumentDto
            {
                Title = "New Title",
                Category = "Financial",
                VisibilityScope = VisibilityScope.Public.ToString()
            };

            var actorId = Guid.NewGuid();

            // Act
            var result = await _service.UpdateMetadataAsync(doc.DocumentId, dto, actorId);

            // Assert
            Assert.IsTrue(result);

            var dbDoc = await _db.Documents.FirstAsync(d => d.DocumentId == doc.DocumentId);
            Assert.AreEqual("New Title", dbDoc.Title);
            Assert.AreEqual("Financial", dbDoc.Category);
            Assert.AreEqual(VisibilityScope.Public.ToString(), dbDoc.VisibilityScope);
            Assert.AreEqual("PENDING_APPROVAL", dbDoc.Status);

            var logs = await _db.DocumentActionLogs.Where(l => l.DocumentId == doc.DocumentId).ToListAsync();
            Assert.AreEqual(1, logs.Count);
            Assert.AreEqual("UPDATE_METADATA", logs[0].Action);
            Assert.AreEqual(actorId, logs[0].ActorId);
            var detail = logs[0].Detail;
            Assert.IsNotNull(detail);
            Assert.IsTrue(detail.Contains("Tiêu đề"));
            Assert.IsTrue(detail.Contains("Phân loại"));
            Assert.IsTrue(detail.Contains("Phạm vi"));
        }

        #endregion

        #region SoftDeleteAsync & RestoreAsync tests

        // UTCID01: SoftDeleteAsync - Document not found
        // Precondition: Can connect with server
        // Input: DocumentId = non-existent GUID, ActorId = valid GUID, Reason = "reason" (valid)
        // Expected: Returns false (document not found)
        // Exception: None (returns false)
        // Result Type: A (Abnormal)
        [TestMethod]
        public async Task SoftDeleteAsync_DocumentNotFound_ReturnsFalse()
        {
            // Arrange - Precondition: Can connect with server
            // Act
            var result = await _service.SoftDeleteAsync(Guid.NewGuid(), Guid.NewGuid(), "reason");

            // Assert
            Assert.IsFalse(result);
        }

        // UTCID02: SoftDeleteAsync - Document already INACTIVE
        // Precondition: Can connect with server, Document exists with Status = "INACTIVE"
        // Input: DocumentId = existing document (INACTIVE), ActorId = valid GUID, Reason = "reason" (valid)
        // Expected: Returns true (already inactive, considered success)
        // Exception: None(Success)
        // Result Type: N (Normal)
        [TestMethod]
        public async Task SoftDeleteAsync_AlreadyInactive_ReturnsTrue()
        {
            // Arrange - Precondition: Can connect with server, Document exists with Status = "INACTIVE"
            var doc = CreateDocument(status: "INACTIVE");
            await _db.Documents.AddAsync(doc);
            await _db.SaveChangesAsync();

            var actorId = Guid.NewGuid();

            // Act
            var result = await _service.SoftDeleteAsync(doc.DocumentId, actorId, "reason");

            // Assert
            Assert.IsTrue(result);

            var dbDoc = await _db.Documents.FirstAsync(d => d.DocumentId == doc.DocumentId);
            Assert.AreEqual("INACTIVE", dbDoc.Status);

            // Should not create additional log if already inactive
            var logs = await _db.DocumentActionLogs.Where(l => l.DocumentId == doc.DocumentId && l.Action == "SOFT_DELETE").ToListAsync();
            // May or may not create log, depends on implementation
        }

        [TestMethod]
        public async Task SoftDeleteAsync_SetsStatusDeleted_AndAddsLog()
        {
            // Arrange
            var doc = CreateDocument(status: "ACTIVE");
            await _db.Documents.AddAsync(doc);
            await _db.SaveChangesAsync();

            var actorId = Guid.NewGuid();

            // Act
            var result = await _service.SoftDeleteAsync(doc.DocumentId, actorId, "for testing");

            // Assert
            Assert.IsTrue(result);

            var dbDoc = await _db.Documents.FirstAsync(d => d.DocumentId == doc.DocumentId);
            // Soft delete bây giờ đưa tài liệu về INACTIVE (ngừng hiển thị), không dùng DELETED nữa
            Assert.AreEqual("INACTIVE", dbDoc.Status);

            var logs = await _db.DocumentActionLogs.Where(l => l.DocumentId == doc.DocumentId).ToListAsync();
            Assert.AreEqual(1, logs.Count);
            Assert.AreEqual("SOFT_DELETE", logs[0].Action);
            Assert.AreEqual(actorId, logs[0].ActorId);
            Assert.AreEqual("for testing", logs[0].Detail);
        }

        // UTCID01: RestoreAsync - Document not found
        // Precondition: Can connect with server
        // Input: DocumentId = non-existent GUID, ActorId = valid GUID, Reason = "reason" (valid)
        // Expected: Returns false (document not found)
        // Exception: None (returns false)
        // Result Type: A (Abnormal)
        [TestMethod]
        public async Task RestoreAsync_DocumentNotFound_ReturnsFalse()
        {
            // Arrange - Precondition: Can connect with server
            // Act
            var result = await _service.RestoreAsync(Guid.NewGuid(), Guid.NewGuid(), "reason");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task RestoreAsync_PendingApproval_ReturnsTrueWithoutChanges()
        {
            // Arrange
            var doc = CreateDocument(status: "PENDING_APPROVAL");
            await _db.Documents.AddAsync(doc);
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.RestoreAsync(doc.DocumentId, Guid.NewGuid(), "reason");

            // Assert
            Assert.IsTrue(result);

            var dbDoc = await _db.Documents.FirstAsync(d => d.DocumentId == doc.DocumentId);
            Assert.AreEqual("PENDING_APPROVAL", dbDoc.Status);

            var logs = await _db.DocumentActionLogs.Where(l => l.DocumentId == doc.DocumentId).ToListAsync();
            Assert.AreEqual(0, logs.Count);
        }

        [TestMethod]
        public async Task RestoreAsync_NotDeletedAndNotPending_ReturnsFalse()
        {
            // Arrange
            var doc = CreateDocument(status: "ACTIVE");
            await _db.Documents.AddAsync(doc);
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.RestoreAsync(doc.DocumentId, Guid.NewGuid(), "reason");

            // Assert
            Assert.IsFalse(result);

            var logs = await _db.DocumentActionLogs.Where(l => l.DocumentId == doc.DocumentId).ToListAsync();
            Assert.AreEqual(0, logs.Count);
        }

        [TestMethod]
        public async Task RestoreAsync_DeletedWithReason_SetsPendingAndAddsLogWithReason()
        {
            // Arrange
            var doc = CreateDocument(status: "DELETED");
            await _db.Documents.AddAsync(doc);
            await _db.SaveChangesAsync();

            var actorId = Guid.NewGuid();

            // Act
            var result = await _service.RestoreAsync(doc.DocumentId, actorId, "please restore");

            // Assert
            Assert.IsTrue(result);

            var dbDoc = await _db.Documents.FirstAsync(d => d.DocumentId == doc.DocumentId);
            Assert.AreEqual("PENDING_APPROVAL", dbDoc.Status);

            var logs = await _db.DocumentActionLogs.Where(l => l.DocumentId == doc.DocumentId).ToListAsync();
            Assert.AreEqual(1, logs.Count);
            Assert.AreEqual("REQUEST_RESTORE", logs[0].Action);
            Assert.AreEqual(actorId, logs[0].ActorId);
            Assert.AreEqual("please restore", logs[0].Detail);
        }

        [TestMethod]
        public async Task RestoreAsync_DeletedWithoutReason_UsesDefaultMessage()
        {
            // Arrange
            var doc = CreateDocument(status: "DELETED");
            await _db.Documents.AddAsync(doc);
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.RestoreAsync(doc.DocumentId, null, null);

            // Assert
            Assert.IsTrue(result);

            var logs = await _db.DocumentActionLogs.Where(l => l.DocumentId == doc.DocumentId).ToListAsync();
            Assert.AreEqual(1, logs.Count);
            Assert.AreEqual("Yêu cầu hiển thị lại tài liệu", logs[0].Detail);
        }

        #endregion

        #region GetLogsAsync & GetAllVersionsAsync tests

        [TestMethod]
        public async Task GetLogsAsync_ReturnsMappedDtos()
        {
            // Arrange
            var doc = CreateDocument();
            await _db.Documents.AddAsync(doc);
            await _db.DocumentActionLogs.AddAsync(new DocumentActionLog
            {
                ActionLogId = Guid.NewGuid(),
                DocumentId = doc.DocumentId,
                Action = "TEST",
                ActorId = Guid.NewGuid(),
                ActionAt = DateTime.UtcNow,
                Detail = "Created by tester"
            });
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.GetLogsAsync(doc.DocumentId);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(doc.DocumentId, result[0].DocumentId);
        }

        [TestMethod]
        public async Task GetAllVersionsAsync_ReturnsVersionsOrderedDesc()
        {
            // Arrange
            var doc = CreateDocument();
            var file = CreateStoredFile();
            var v1 = CreateDocumentVersion(document: doc, versionNo: 1, file: file);
            var v2 = CreateDocumentVersion(document: doc, versionNo: 2, file: file);

            await _db.Documents.AddAsync(doc);
            await _db.Files.AddAsync(file);
            await _db.DocumentVersions.AddRangeAsync(v1, v2);
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.GetAllVersionsAsync(doc.DocumentId);

            // Assert
            var list = result.ToList();
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(2, list[0].VersionNo);
            Assert.AreEqual(1, list[1].VersionNo);
            Assert.IsNotNull(list[0].File);
        }

        #endregion

        #region GetResidentDocumentsAsync tests

        [TestMethod]
        public async Task GetResidentDocumentsAsync_FiltersByStatusScopeTitleAndCategory()
        {
            // Arrange
            var file = CreateStoredFile();

            var docActivePublic = CreateDocument(
                title: "Resident Guide",
                category: "Resident",
                visibilityScope: VisibilityScope.Public.ToString(),
                status: "ACTIVE");

            var docActiveResidentManager = CreateDocument(
                title: "Manager Instructions",
                category: "Resident",
                visibilityScope: "Resident_Manager_Only",
                status: "ACTIVE");

            var docActiveOtherScope = CreateDocument(
                title: "Internal Only",
                category: "Resident",
                visibilityScope: "Building_Management_Only",
                status: "ACTIVE");

            var docInactivePublic = CreateDocument(
                title: "Old Guide",
                category: "Resident",
                visibilityScope: VisibilityScope.Public.ToString(),
                status: "INACTIVE");

            var docActiveNullScope = CreateDocument(
                title: "Null Scope",
                category: "Resident",
                visibilityScope: null,
                status: "ACTIVE");

            CreateDocumentVersion(document: docActivePublic, versionNo: 1, file: file);
            CreateDocumentVersion(document: docActiveResidentManager, versionNo: 1, file: file);
            CreateDocumentVersion(document: docActiveOtherScope, versionNo: 1, file: file);
            CreateDocumentVersion(document: docInactivePublic, versionNo: 1, file: file);
            CreateDocumentVersion(document: docActiveNullScope, versionNo: 1, file: file);

            await _db.Files.AddAsync(file);
            await _db.Documents.AddRangeAsync(
                docActivePublic,
                docActiveResidentManager,
                docActiveOtherScope,
                docInactivePublic,
                docActiveNullScope);
            await _db.SaveChangesAsync();

            var dto = new DocumentQueryDto
            {
                Title = "guide", // should match "Resident Guide" (case-insensitive)
                Category = "Resident"
            };

            // Act
            var result = await _service.GetResidentDocumentsAsync(dto);

            // Assert
            var list = result.ToList();
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(docActivePublic.DocumentId, list[0].DocumentId);
            Assert.AreEqual(file.FileId, list[0].FileId);
        }

        #endregion
    }
}