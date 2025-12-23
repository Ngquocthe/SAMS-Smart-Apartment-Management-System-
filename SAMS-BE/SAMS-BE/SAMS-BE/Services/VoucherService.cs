using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SAMS_BE.DTOs;
using SAMS_BE.Helpers;
using SAMS_BE.Interfaces;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Models;

namespace SAMS_BE.Services
{
    public class VoucherService : Interfaces.IService.IVoucherService
    {
        private readonly Interfaces.IRepository.IVoucherRepository _voucherRepo;
        private readonly IVoucherItemRepository _voucherItemRepo;
        private readonly ITicketRepository _ticketRepo;
        private readonly IAssetMaintenanceHistoryRepository _historyRepo;
        private readonly BuildingManagementContext _context;
        private readonly IJournalEntryService _journalEntryService;
        private readonly ILogger<VoucherService> _logger;

        private async Task<ServiceType?> FindDefaultMaintenanceServiceTypeAsync()
        {
            // Tìm tất cả service types active
            var allActiveServiceTypes = await _context.ServiceTypes
                .Where(st => st.IsActive && (st.IsDelete == null || st.IsDelete == false))
                .ToListAsync();

            // Ưu tiên 1: Tìm theo tên chứa "Phí bảo trì" hoặc "Phí bảo trì/sửa chữa" (case-insensitive)
            var exactMatch = allActiveServiceTypes
                .FirstOrDefault(st =>
                    st.Name.Contains("Phí bảo trì", StringComparison.OrdinalIgnoreCase) ||
                    st.Name.Contains("Phí bảo trì/sửa chữa", StringComparison.OrdinalIgnoreCase) ||
                    st.Name.Contains("phí bảo trì/sửa chữa", StringComparison.OrdinalIgnoreCase));

            if (exactMatch != null)
                return exactMatch;

            // Ơi tiên 2: Tìm theo "Bảo trì" hoặc "Sửa chữa" (case-insensitive)
            var maintenanceMatch = allActiveServiceTypes
                .FirstOrDefault(st =>
                    st.Name.Contains("Bảo trì", StringComparison.OrdinalIgnoreCase) ||
                    st.Name.Contains("Sửa chữa", StringComparison.OrdinalIgnoreCase) ||
                    st.Name.Contains("bảo trì", StringComparison.OrdinalIgnoreCase) ||
                    st.Name.Contains("sửa chữa", StringComparison.OrdinalIgnoreCase));

            if (maintenanceMatch != null)
                return maintenanceMatch;

            // Ơi tiên 3: Tìm theo code
            var codeMatch = allActiveServiceTypes
                .FirstOrDefault(st =>
                    (st.Code != null && st.Code.Contains("MAINTENANCE", StringComparison.OrdinalIgnoreCase)) ||
                    (st.Code != null && st.Code.Contains("REPAIR", StringComparison.OrdinalIgnoreCase)));

            return codeMatch;
        }

        private async Task<ServiceType> EnsureDefaultMaintenanceServiceTypeAsync()
        {
            var existing = await FindDefaultMaintenanceServiceTypeAsync();
            if (existing != null) return existing;

            // Auto-seed Category
            var categoryName = "Chi phí bảo trì & sửa chữa";
            var category = await _context.ServiceTypeCategories
                .FirstOrDefaultAsync(c => c.Name == categoryName);

            if (category == null)
            {
                category = new ServiceTypeCategory
                {
                    CategoryId = Guid.NewGuid(),
                    Name = categoryName,
                    Description = "Danh mục chi phí dành cho bảo trì và sửa chữa tài sản",
                    CreatedAt = DateTime.UtcNow
                };
                _context.ServiceTypeCategories.Add(category);
                await _context.SaveChangesAsync();
            }

            // Auto-seed ServiceType
            var newServiceType = new ServiceType
            {
                ServiceTypeId = Guid.NewGuid(),
                Code = "MAINTENANCE_FEE",
                Name = "Phí bảo trì/sửa chữa thiết bị",
                CategoryId = category.CategoryId,
                Unit = "Lần",
                IsMandatory = false,
                IsRecurring = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                IsDelete = false
            };

            _context.ServiceTypes.Add(newServiceType);
            await _context.SaveChangesAsync();
            return newServiceType;
        }

        public VoucherService(
            Interfaces.IRepository.IVoucherRepository voucherRepo,
            IVoucherItemRepository voucherItemRepo,
            ITicketRepository ticketRepo,
            IAssetMaintenanceHistoryRepository historyRepo,
            BuildingManagementContext context,
            IJournalEntryService journalEntryService,
            ILogger<VoucherService> logger)
        {
            _voucherRepo = voucherRepo;
            _voucherItemRepo = voucherItemRepo;
            _ticketRepo = ticketRepo;
            _historyRepo = historyRepo;
            _context = context;
            _journalEntryService = journalEntryService;
            _logger = logger;
        }

        // =========================
        // 1️⃣ Create Voucher from DTO
        // =========================
        public async Task<VoucherDto> CreateAsync(CreateVoucherDto dto)
        {
            if (dto.TotalAmount < 0)
                throw new ArgumentException("TotalAmount must be greater than or equal to 0.", nameof(dto.TotalAmount));

            var status = ResolveManualStatus(dto.Status);
            var voucherNumber = await EnsureUniqueVoucherNumberAsync(dto.VoucherNumber);

            var voucher = new Voucher
            {
                VoucherId = Guid.NewGuid(),
                VoucherNumber = voucherNumber,
                Type = VoucherHelper.TYPE_PAYMENT,
                Date = dto.Date,
                CompanyInfo = dto.CompanyInfo,
                TotalAmount = dto.TotalAmount,
                Description = string.IsNullOrWhiteSpace(dto.Description) ? "Phiếu chi" : dto.Description.Trim(),
                Status = status,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                var createdVoucher = await _voucherRepo.CreateAsync(voucher);
                await CreateDefaultVoucherItemAsync(createdVoucher.VoucherId, dto.TotalAmount, dto.Description);
                await RecalculateVoucherTotalAsync(createdVoucher.VoucherId);
                return await BuildVoucherDtoAsync(createdVoucher.VoucherId);
            }
            catch (DbUpdateException ex) when (IsUniqueNumberViolation(ex))
            {
                _logger.LogWarning(ex, "Duplicate voucher number {VoucherNumber}, retrying with a new number", voucherNumber);

                voucher.VoucherId = Guid.NewGuid();
                voucher.VoucherNumber = await GenerateUniqueVoucherNumberAsync();

                var createdVoucher = await _voucherRepo.CreateAsync(voucher);
                await CreateDefaultVoucherItemAsync(createdVoucher.VoucherId, dto.TotalAmount, dto.Description);
                await RecalculateVoucherTotalAsync(createdVoucher.VoucherId);
                return await BuildVoucherDtoAsync(createdVoucher.VoucherId);
            }
        }

        private static string ResolveManualStatus(string? status)
        {
            var normalized = string.IsNullOrWhiteSpace(status)
                ? VoucherHelper.STATUS_DRAFT
                : status.Trim().ToUpperInvariant();

            if (normalized != VoucherHelper.STATUS_DRAFT)
                throw new InvalidOperationException("Manual vouchers must start in DRAFT status.");

            return VoucherHelper.STATUS_DRAFT;
        }

        private async Task<string> EnsureUniqueVoucherNumberAsync(string? requestedNumber)
        {
            if (string.IsNullOrWhiteSpace(requestedNumber))
                return await GenerateUniqueVoucherNumberAsync();

            var trimmed = requestedNumber.Trim();
            var exists = await _context.Vouchers.AnyAsync(v => v.VoucherNumber == trimmed);
            if (!exists) return trimmed;

            _logger.LogWarning("Voucher number {VoucherNumber} already exists, generating a unique number", trimmed);
            return await GenerateUniqueVoucherNumberAsync();
        }

        private async Task CreateDefaultVoucherItemAsync(Guid voucherId, decimal amount, string? description)
        {
            var now = DateTime.UtcNow;
            var item = new VoucherItem
            {
                VoucherItemsId = Guid.NewGuid(),
                VoucherId = voucherId,
                Description = string.IsNullOrWhiteSpace(description) ? "Chi phí" : description.Trim(),
                Quantity = 1,
                UnitPrice = amount,
                Amount = amount,
                CreatedAt = now
            };

            await _context.VoucherItems.AddAsync(item);
            await _context.SaveChangesAsync();
        }

        private async Task RecalculateVoucherTotalAsync(Guid voucherId)
        {
            var items = await _context.VoucherItems
                .Where(i => i.VoucherId == voucherId)
                .ToListAsync();

            var voucher = await _context.Vouchers.FirstAsync(v => v.VoucherId == voucherId);
            voucher.TotalAmount = items.Sum(i => i.Amount ?? 0);
            await _context.SaveChangesAsync();
        }

        private async Task<VoucherDto> BuildVoucherDtoAsync(Guid voucherId)
        {
            var voucher = await _voucherRepo.GetByIdAsync(voucherId)
                ?? throw new KeyNotFoundException($"Voucher with ID {voucherId} not found.");
            return MapToDto(voucher, includeItems: true);
        }

        private async Task<string> GenerateUniqueVoucherNumberAsync()
        {
            for (var attempt = 0; attempt < 5; attempt++)
            {
                var candidate = $"VOU-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Random.Shared.Next(100, 999)}";
                var exists = await _context.Vouchers.AnyAsync(v => v.VoucherNumber == candidate);
                if (!exists) return candidate;
                await Task.Delay(5);
            }

            return $"VOU-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}";
        }

        private static bool IsUniqueNumberViolation(DbUpdateException ex)
        {
            return ex.InnerException is SqlException sqlEx && sqlEx.Number == 2627;
        }

        private static VoucherDto MapToDto(Voucher voucher, bool includeItems)
        {
            return new VoucherDto
            {
                VoucherId = voucher.VoucherId,
                VoucherNumber = voucher.VoucherNumber,
                Type = voucher.Type,
                Date = voucher.Date,
                FiscalPeriod = $"{voucher.Date.Year:D4}/{voucher.Date.Month:D2}",
                CompanyInfo = voucher.CompanyInfo,
                Status = voucher.Status,
                TotalAmount = voucher.TotalAmount,
                Description = voucher.Description,
                TicketId = voucher.TicketId,
                HistoryId = voucher.HistoryId,
                Items = includeItems
                    ? voucher.VoucherItems.Select(MapVoucherItem).ToList()
                    : new List<VoucherItemResponseDto>()
            };
        }

        private static VoucherItemResponseDto MapVoucherItem(VoucherItem item)
        {
            return new VoucherItemResponseDto
            {
                VoucherItemsId = item.VoucherItemsId,
                ServiceTypeId = item.ServiceTypeId,
                ServiceTypeName = item.ServiceType?.Name ?? string.Empty,
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Amount = item.Amount,
                ApartmentId = item.ApartmentId
            };
        }

        // =========================
        // 2️⃣ Create Voucher from Ticket (Building Scope)
        // =========================
        public async Task<(Guid VoucherId, string VoucherNumber)> CreateFromTicketAsync(CreateVoucherRequest request)
        {
            var ticket = await _ticketRepo.GetByIdAsync(request.TicketId)
                ?? throw new ArgumentException("Ticket không tồn tại");

            if (string.Equals(ticket.Status, "CLOSED", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Ticket đã CLOSED, không thể tạo phiếu chi.");

            var quantity = request.Quantity ?? 1;
            if (quantity <= 0)
                throw new ArgumentException("Quantity phải lớn hơn 0.");

            decimal unitPrice;
            if (request.UnitPrice.HasValue)
            {
                if (request.UnitPrice.Value < 0)
                    throw new ArgumentException("UnitPrice không được nhỏ hơn 0.");
                unitPrice = request.UnitPrice.Value;
            }
            else if (request.Amount > 0)
            {
                unitPrice = request.Amount / quantity;
            }
            else
            {
                throw new ArgumentException("UnitPrice phải được cung cấp khi Amount <= 0.");
            }

            var itemAmount = request.Amount > 0 ? request.Amount : quantity * unitPrice;
            if (itemAmount <= 0)
                throw new ArgumentException("Số tiền của voucher phải lớn hơn 0.");

            var now = DateTimeHelper.VietnamNow; // Lưu giờ VN
            var existedVoucherId = await _voucherRepo.GetVoucherIdByTicketAsync(request.TicketId);

            // Lấy tên ServiceType để set Voucher.Description
            string? serviceTypeName = null;
            if (request.ServiceTypeId.HasValue && request.ServiceTypeId.Value != Guid.Empty)
            {
                serviceTypeName = await _context.ServiceTypes
                    .Where(st => st.ServiceTypeId == request.ServiceTypeId.Value)
                    .Select(st => st.Name)
                    .FirstOrDefaultAsync();
            }

            Voucher voucher;
            if (existedVoucherId != null)
            {
                voucher = await _context.Vouchers.FirstAsync(v => v.VoucherId == existedVoucherId.Value);
                // Không cộng thêm, sẽ tính lại từ tất cả items sau
            }
            else
            {
                var number = $"VOU-{now:yyyyMMddHHmmss}";
                // Voucher.Description: ưu tiên InvoiceNo, sau đó ServiceTypeName, cuối cùng là mô tả mặc định
                string voucherDescription;
                if (!string.IsNullOrWhiteSpace(request.InvoiceNo))
                {
                    voucherDescription = $"Voucher từ hóa đơn {request.InvoiceNo}";
                }
                else if (!string.IsNullOrWhiteSpace(serviceTypeName))
                {
                    voucherDescription = $"Chi phí {serviceTypeName}";
                }
                else
                {
                    voucherDescription = "Phiếu chi";
                }

                voucher = new Voucher
                {
                    VoucherId = Guid.NewGuid(),
                    VoucherNumber = number,
                    Type = VoucherHelper.TYPE_PAYMENT, // Force PAYMENT
                    Date = DateOnly.FromDateTime(now),
                    TotalAmount = itemAmount,
                    Description = voucherDescription,
                    Status = VoucherHelper.STATUS_PENDING, // ✅ PENDING - Chờ kế toán duyệt
                    TicketId = request.TicketId,  // ✅ Set TicketId at Voucher level
                    CreatedAt = now
                };
                await _voucherRepo.AddVoucherAsync(voucher);
            }

            // VoucherItem.Description = ghi chú từ form (request.Note)
            var item = new VoucherItem
            {
                VoucherItemsId = Guid.NewGuid(),
                VoucherId = voucher.VoucherId,
                Description = request.Note,
                Quantity = quantity,
                UnitPrice = unitPrice,
                Amount = itemAmount,
                ServiceTypeId = request.ServiceTypeId,
                ApartmentId = ticket.ApartmentId,
                CreatedAt = now
            };

            await _voucherRepo.AddVoucherItemAsync(item);

            // Tính lại TotalAmount từ tất cả items để đảm bảo chính xác
            var allItems = await _context.VoucherItems
                .Where(i => i.VoucherId == voucher.VoucherId)
                .ToListAsync();
            voucher.TotalAmount = allItems.Sum(i => i.Amount ?? 0);
            await _context.SaveChangesAsync();

            ticket.HasInvoice = true;
            ticket.UpdatedAt = now;
            await _ticketRepo.UpdateAsync(ticket);

            // Ghi comment vào ticket khi tạo phiếu chi từ ticket (giờ Việt Nam UTC+7)
            var comment = new TicketComment
            {
                CommentId = Guid.NewGuid(),
                TicketId = ticket.TicketId,
                Content = $"Đã tạo phiếu chi {voucher.VoucherNumber} (trạng thái {voucher.Status}).",
                CommentTime = DateTimeHelper.VietnamNow,
                CommentedBy = request.CreatedByUserId
            };
            await _ticketRepo.AddCommentAsync(comment);

            return (voucher.VoucherId, voucher.VoucherNumber);
        }

        // =========================
        // 2️⃣ Create Voucher from Maintenance History
        // =========================
        public async Task<(Guid VoucherId, string VoucherNumber)> CreateFromMaintenanceAsync(CreateVoucherFromMaintenanceRequest request)
        {
            try
            {
                // Validate Amount > 0
                if (request.Amount <= 0)
                    throw new ArgumentException("Amount phải lớn hơn 0");

                // Validate history tồn tại
                var history = await _historyRepo.GetHistoryByIdAsync(request.HistoryId);
                if (history == null)
                    throw new ArgumentException($"Maintenance history với ID {request.HistoryId} không tồn tại");

                // Set ServiceTypeId to null to avoid FK errors in multi-tenant environments
                Guid? serviceTypeId = null;


                // Validate chưa có voucher (tránh duplicate)
                var existedVoucherId = await _voucherRepo.GetVoucherIdByHistoryAsync(request.HistoryId);
                if (existedVoucherId != null)
                {
                    var existingVoucher = await _voucherRepo.GetByIdAsync(existedVoucherId.Value);
                    throw new InvalidOperationException(
                        $"History này đã có phiếu chi rồi. " +
                        $"Voucher Number: {existingVoucher?.VoucherNumber ?? "N/A"}, " +
                        $"Status: {existingVoucher?.Status ?? "N/A"}. " +
                        $"Vui lòng kiểm tra lại."
                    );
                }

                var now = DateTime.UtcNow;
                var voucherNumber = $"VOU-{now:yyyyMMddHHmmss}";

                // Tạo voucher mới - chỉ là phiếu chi (PAYMENT)
                var voucher = new Voucher
                {
                    VoucherId = Guid.NewGuid(),
                    VoucherNumber = voucherNumber,
                    Type = VoucherHelper.TYPE_PAYMENT, // Force PAYMENT
                    Date = DateOnly.FromDateTime(now),
                    TotalAmount = request.Amount,
                    Description = request.Note ?? $"Bảo trì tài sản - {history.Action}",
                    Status = VoucherHelper.STATUS_PENDING, // ✅ PENDING - Chờ kế toán duyệt
                    HistoryId = request.HistoryId,  // ✅ Set HistoryId at Voucher level
                    CreatedAt = now
                };

                await _voucherRepo.AddVoucherAsync(voucher);

                // Tạo voucher_item và link với history
                var item = new VoucherItem
                {
                    VoucherItemsId = Guid.NewGuid(),
                    VoucherId = voucher.VoucherId,
                    Description = request.Note ?? history.Notes,
                    Quantity = 1,
                    UnitPrice = request.Amount,
                    Amount = request.Amount,
                    ServiceTypeId = serviceTypeId,
                    CreatedAt = now
                };

                await _voucherRepo.AddVoucherItemAsync(item);

                // Tính lại TotalAmount từ tất cả items để đảm bảo chính xác
                var allItems = await _context.VoucherItems
                    .Where(i => i.VoucherId == voucher.VoucherId)
                    .ToListAsync();
                voucher.TotalAmount = allItems.Sum(i => i.Amount ?? 0);

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException dbEx)
                {
                    var isFkError = dbEx.InnerException?.Message.Contains("FK_vi_service_type") == true ||
                                   dbEx.Message.Contains("FK_vi_service_type");

                    if (isFkError)
                    {
                        _logger.LogWarning("FK_vi_service_type error detected. Retrying with ServiceTypeId = null...");

                        // Find the tracked entity and update it
                        var trackedItem = _context.VoucherItems.Local.FirstOrDefault(i => i.VoucherItemsId == item.VoucherItemsId);
                        if (trackedItem != null)
                        {
                            trackedItem.ServiceTypeId = null;
                            _context.Entry(trackedItem).Property(x => x.ServiceTypeId).IsModified = true;
                        }
                        else
                        {
                            // Fallback: update the original item reference
                            item.ServiceTypeId = null;
                        }

                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Successfully created voucher with ServiceTypeId = null after FK retry");
                    }
                    else
                    {
                        throw;
                    }
                }

                return (voucher.VoucherId, voucher.VoucherNumber);
            }
            catch (ArgumentException ex)
            {
                // Re-throw validation errors
                throw;
            }
            catch (InvalidOperationException ex)
            {
                // Re-throw business logic errors
                throw;
            }
            catch (DbUpdateException ex)
            {
                // Database constraint violations
                throw new InvalidOperationException(
                    $"Lỗi database khi tạo phiếu chi. " +
                    $"Có thể do foreign key constraint violation. " +
                    $"Chi tiết: {ex.InnerException?.Message ?? ex.Message}", ex);
            }
            catch (Exception ex)
            {
                // Unexpected errors
                throw new InvalidOperationException(
                    $"Lỗi không mong đợi khi tạo phiếu chi từ maintenance history. " +
                    $"HistoryId: {request.HistoryId}, " +
                    $"ServiceTypeId: {request.ServiceTypeId}. " +
                    $"Chi tiết: {ex.Message}", ex);
            }
        }

        public async Task<Guid?> GetVoucherIdByHistoryAsync(Guid historyId)
        {
            return await _voucherRepo.GetVoucherIdByHistoryAsync(historyId);
        }

        public async Task<(Guid Id, string Name)?> GetDefaultMaintenanceServiceTypeAsync()
        {
            var serviceType = await FindDefaultMaintenanceServiceTypeAsync();
            if (serviceType == null)
            {
                return null;
            }

            return (serviceType.ServiceTypeId, serviceType.Name);
        }

        // =========================
        // 3️⃣ Get Methods
        // =========================
        public async Task<VoucherDto?> GetByIdAsync(Guid voucherId)
        {
            var voucher = await _voucherRepo.GetByIdAsync(voucherId);
            if (voucher == null) return null;

            return MapToDto(voucher, includeItems: true);
        }

        public async Task<PagedResult<VoucherDto>> ListAsync(VoucherListQueryDto query)
        {
            var (items, total) = await _voucherRepo.ListAsync(query);
            var page = query.Page <= 0 ? 1 : query.Page;
            var pageSize = query.PageSize <= 0 ? 20 : query.PageSize;

            var dtoItems = items.Select(v => MapToDto(v, includeItems: false)).ToList();
            return new PagedResult<VoucherDto>
            {
                Items = dtoItems,
                TotalItems = total,
                TotalCount = total,
                PageNumber = page,
                PageSize = pageSize,
                TotalPages = pageSize == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize)
            };
        }

        // =========================
        // 4️⃣ Update Voucher
        // =========================
        public async Task<VoucherDto> UpdateAsync(Guid id, UpdateVoucherDto dto)
        {
            var voucher = await _voucherRepo.GetByIdForUpdateAsync(id)
                ?? throw new KeyNotFoundException($"Voucher with ID {id} not found.");

            if (!VoucherHelper.CanEditOrDelete(voucher.Status))
                throw new InvalidOperationException(
                    $"Cannot edit voucher with status {VoucherHelper.GetStatusDisplayName(voucher.Status)}. " +
                    $"Only {VoucherHelper.GetStatusDisplayName(VoucherHelper.STATUS_DRAFT)} vouchers can be edited. " +
                    $"Vouchers from Ticket/Maintenance are PENDING and cannot be edited.");

            if (dto.Date.HasValue) voucher.Date = dto.Date.Value;
            if (dto.CompanyInfo != null) voucher.CompanyInfo = dto.CompanyInfo;
            if (dto.Description != null) voucher.Description = dto.Description;

            await _voucherRepo.UpdateAsync(voucher);
            return await BuildVoucherDtoAsync(id);
        }

        public async Task<VoucherDto> UpdateStatusAsync(Guid id, UpdateVoucherStatusDto dto)
        {
            var voucher = await _voucherRepo.GetByIdForUpdateAsync(id)
                ?? throw new KeyNotFoundException($"Voucher with ID {id} not found.");

            if (!VoucherHelper.IsValidStatus(dto.Status))
                throw new ArgumentException($"Invalid voucher status: {dto.Status}");

            if (!VoucherHelper.CanTransitionStatus(voucher.Status, dto.Status))
                throw new InvalidOperationException(
                    $"Cannot transition voucher status from " +
                    $"{VoucherHelper.GetStatusDisplayName(voucher.Status)} to " +
                    $"{VoucherHelper.GetStatusDisplayName(dto.Status)}");

            // Validate before approve
            if (dto.Status == VoucherHelper.STATUS_APPROVED)
                await ValidateVoucherBalanceAsync(id);

            voucher.Status = dto.Status;

            // When approved, set approved date and create journal entry
            if (dto.Status == VoucherHelper.STATUS_APPROVED)
            {
                voucher.ApprovedDate = DateTime.UtcNow;

                // ✅ Tự động tạo Journal Entry khi approve (giống Receipt)
                await _journalEntryService.CreateJournalEntryFromVoucherAsync(voucher);
            }

            await _voucherRepo.UpdateAsync(voucher);
            return await BuildVoucherDtoAsync(id);
        }

        // =========================
        // 5️⃣ Update Item
        // =========================
        public async Task<VoucherItemResponseDto?> GetItemByIdAsync(Guid itemId)
        {
            var item = await _voucherRepo.GetItemByIdAsync(itemId);
            if (item == null) return null;

            return new VoucherItemResponseDto
            {
                VoucherItemsId = item.VoucherItemsId,
                ServiceTypeId = item.ServiceTypeId,
                ServiceTypeName = item.ServiceType?.Name ?? string.Empty,
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Amount = item.Amount,
                ApartmentId = item.ApartmentId
            };
        }

        public async Task<bool> UpdateItemAsync(Guid itemId, UpdateVoucherItemDto dto)
        {
            var item = await _context.VoucherItems
                .Include(i => i.Voucher)
                .FirstOrDefaultAsync(i => i.VoucherItemsId == itemId);
            if (item == null) return false;

            if (!string.Equals(item.Voucher.Status, "DRAFT", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Chỉ có thể chỉnh sửa VoucherItem khi Voucher status = DRAFT");

            var oldAmount = item.Amount ?? 0;

            // Cập nhật các trường từ DTO
            if (dto.Quantity.HasValue)
            {
                item.Quantity = dto.Quantity;
            }

            if (dto.UnitPrice.HasValue)
            {
                item.UnitPrice = dto.UnitPrice;
            }

            if (dto.Amount.HasValue)
            {
                item.Amount = dto.Amount;
            }
            else if (dto.Quantity.HasValue && dto.UnitPrice.HasValue)
            {
                // Tự động tính Amount nếu có Quantity và UnitPrice
                item.Amount = dto.Quantity.Value * dto.UnitPrice.Value;
            }
            else if (item.Quantity.HasValue && item.UnitPrice.HasValue)
            {
                // Tính lại Amount từ Quantity và UnitPrice hiện tại
                item.Amount = item.Quantity.Value * item.UnitPrice.Value;
            }

            if (dto.Description != null)
            {
                item.Description = dto.Description;
            }

            // Tính lại tổng tiền của voucher dựa trên số tiền mới
            var newAmount = item.Amount ?? 0;
            var voucher = item.Voucher;
            var allItems = await _context.VoucherItems
                .Where(i => i.VoucherId == voucher.VoucherId)
                .ToListAsync();
            voucher.TotalAmount = allItems.Sum(i => i.Amount ?? 0);

            await _context.SaveChangesAsync();
            return true;
        }

        // =========================
        // 6️⃣ Delete Voucher
        // =========================
        public async Task DeleteAsync(Guid id)
        {
            var voucher = await _voucherRepo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Voucher with ID {id} not found.");

            if (!VoucherHelper.CanEditOrDelete(voucher.Status))
                throw new InvalidOperationException(
                    $"Cannot delete voucher with status {VoucherHelper.GetStatusDisplayName(voucher.Status)}. " +
                    $"Only {VoucherHelper.GetStatusDisplayName(VoucherHelper.STATUS_DRAFT)} vouchers can be deleted. " +
                    $"Vouchers from Ticket/Maintenance are PENDING and cannot be deleted.");

            await _voucherRepo.DeleteAsync(id);
        }

        // =========================
        // 7️⃣ Validate Balance
        // =========================
        private async Task ValidateVoucherBalanceAsync(Guid voucherId)
        {
            var items = await _voucherItemRepo.GetByVoucherIdAsync(voucherId);
            if (items == null || items.Count == 0)
                throw new InvalidOperationException("Cannot approve or post voucher without any items.");

            var totalAmount = items.Sum(i => i.Amount ?? 0);

            // Với schema mới (quantity/unit_price/amount), không cần check balance như debit/credit
            // Chỉ cần đảm bảo có items và totalAmount > 0
            if (totalAmount <= 0)
                throw new InvalidOperationException(
                    $"Voucher không hợp lệ. Tổng tiền phải lớn hơn 0. Hiện tại: {totalAmount:N2} VNĐ. Vui lòng kiểm tra lại các voucher items.");
        }

        // =========================
        // 8️⃣ Get Vouchers By Ticket
        // =========================
        public async Task<List<FinanceItemSummaryDto>> GetByTicketAsync(Guid ticketId)
        {
            var list = await _voucherRepo.GetVouchersByTicketAsync(ticketId);
            return list.Select(x => new FinanceItemSummaryDto
            {
                Id = x.VoucherId,
                Number = x.VoucherNumber,
                Amount = x.Amount
            }).ToList();
        }
    }
}