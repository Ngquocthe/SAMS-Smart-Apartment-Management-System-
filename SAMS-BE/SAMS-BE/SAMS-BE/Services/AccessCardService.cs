using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Models;
using SAMS_BE.Mappers;
using SAMS_BE.Helpers;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Services;

public class AccessCardService : IAccessCardService
{
    private readonly IAccessCardRepository _accessCardRepository;
    private readonly IApartmentRepository _apartmentRepository;
    private readonly ILogger<AccessCardService> _logger;
    private readonly BuildingManagementContext _context;
    private readonly CardHistoryHelper _cardHistoryHelper;

    public AccessCardService(
        IAccessCardRepository accessCardRepository, 
        IApartmentRepository apartmentRepository,
        ILogger<AccessCardService> logger, 
        BuildingManagementContext context, 
        CardHistoryHelper cardHistoryHelper)
    {
        _accessCardRepository = accessCardRepository;
        _apartmentRepository = apartmentRepository;
        _logger = logger;
        _context = context;
        _cardHistoryHelper = cardHistoryHelper;
    }

    public async Task<IEnumerable<AccessCardDto>> GetAccessCardsWithDetailsAsync()
    {
        try
        {
            var accessCards = await _accessCardRepository.GetAccessCardsWithDetailsAsync();
            return accessCards.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting all access cards");
            throw;
        }
    }

    public async Task<AccessCardDto?> GetAccessCardWithDetailsByIdAsync(Guid id)
    {
        try
        {
            var accessCard = await _accessCardRepository.GetAccessCardWithDetailsByIdAsync(id);
            return accessCard?.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting access card by ID: {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<AccessCardDto>> GetAccessCardsByUserIdAsync(Guid userId)
    {
        try
        {
            var accessCards = await _accessCardRepository.GetAccessCardsByUserIdAsync(userId);
            return accessCards.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting access cards by user ID: {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<AccessCardDto>> GetAccessCardsByApartmentIdAsync(Guid apartmentId)
    {
        try
        {
            var accessCards = await _accessCardRepository.GetAccessCardsByApartmentIdAsync(apartmentId);
            return accessCards.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting access cards by apartment ID: {ApartmentId}", apartmentId);
            throw;
        }
    }

    public async Task<IEnumerable<AccessCardDto>> GetAccessCardsByStatusAsync(string status)
    {
        try
        {
            var accessCards = await _accessCardRepository.GetAccessCardsByStatusAsync(status);
            return accessCards.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting access cards by status: {Status}", status);
            throw;
        }
    }

    public async Task<IEnumerable<AccessCardDto>> GetAccessCardsByCardTypeAsync(Guid cardTypeId)
    {
        try
        {
            var accessCards = await _accessCardRepository.GetAccessCardsByCardTypeAsync(cardTypeId);
            return accessCards.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting access cards by card type ID: {CardTypeId}", cardTypeId);
            throw;
        }
    }

    public async Task<bool> IsCardNumberExistsAsync(string cardNumber, Guid? excludeId = null)
    {
        try
        {
            return await _accessCardRepository.IsCardNumberExistsAsync(cardNumber, excludeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while checking card number existence: {CardNumber}", cardNumber);
            throw;
        }
    }

    /// <summary>
    /// Validate card number format: CARD-{ApartmentCode}-{SequenceNumber}
    /// Example: CARD-A1001-01, CARD-A1001-02
    /// </summary>
    private void ValidateCardNumberFormat(string? cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
        {
            throw new ArgumentException("Số thẻ không được để trống");
        }

        // Check format: CARD-{ApartmentCode}-{SequenceNumber}
        var pattern = new System.Text.RegularExpressions.Regex(@"^CARD-[A-Z0-9]+-\d{2,}$", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        if (!pattern.IsMatch(cardNumber))
        {
            throw new ArgumentException("Số thẻ phải theo định dạng: CARD-{Mã căn hộ}-{Số thứ tự}. Ví dụ: CARD-A1001-01");
        }

        // Validate parts
        var parts = cardNumber.Split('-');
        if (parts.Length != 3)
        {
            throw new ArgumentException("Số thẻ phải có 3 phần cách nhau bởi dấu gạch ngang: CARD-{Mã căn hộ}-{Số thứ tự}");
        }

        // Check prefix - phải là "CARD" viết hoa
        if (!parts[0].Equals("CARD", StringComparison.Ordinal))
        {
            if (parts[0].Equals("CARD", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Phần 'CARD' phải viết hoa. Ví dụ: CARD-A1001-01");
            }
            else
            {
                throw new ArgumentException("Số thẻ phải bắt đầu bằng 'CARD' (viết hoa)");
            }
        }

        // Check apartment code (không rỗng và phải có format: {Tên tòa}{4 số})
        // Ví dụ: A1001 (A là tên tòa, 1001 là 4 số - gồm số tầng và số căn hộ)
        if (string.IsNullOrWhiteSpace(parts[1]))
        {
            throw new ArgumentException("Mã căn hộ không được để trống");
        }

        // Validate apartment code format: phải bắt đầu bằng 1 chữ cái hoa (tên tòa), sau đó là chính xác 4 chữ số
        var apartmentCodePattern = new System.Text.RegularExpressions.Regex(@"^[A-Z][0-9]{4}$");
        if (!apartmentCodePattern.IsMatch(parts[1]))
        {
            // Kiểm tra nếu là chữ thường
            var firstChar = parts[1].Length > 0 ? parts[1][0] : '\0';
            if (char.IsLower(firstChar))
            {
                throw new ArgumentException("Tên tòa phải viết hoa. Ví dụ: CARD-A1001-01 (A là tên tòa, phải viết hoa)");
            }
            else
            {
                throw new ArgumentException("Mã căn hộ phải có định dạng: {Tên tòa}{4 số}. Ví dụ: A1001 (A là tên tòa viết hoa, 1001 là 4 số gồm số tầng và số căn hộ)");
            }
        }

        // Check sequence number (phải là số và có ít nhất 2 chữ số)
        if (!int.TryParse(parts[2], out var sequence) || sequence < 1)
        {
            throw new ArgumentException("Số thứ tự phải là số nguyên dương có ít nhất 2 chữ số");
        }
    }

    public async Task<AccessCardDto> CreateAccessCardAsync(CreateAccessCardDto createAccessCardDto)
    {
        try
        {
            // Validate card number format
            ValidateCardNumberFormat(createAccessCardDto.CardNumber);

            // Check if card number already exists
            var cardExists = await _accessCardRepository.IsCardNumberExistsAsync(createAccessCardDto.CardNumber);
            if (cardExists)
            {
                throw new InvalidOperationException($"Card number '{createAccessCardDto.CardNumber}' already exists");
            }

            // Xử lý tìm căn hộ theo số nếu người dùng nhập số căn hộ
            Guid? apartmentId = createAccessCardDto.IssuedToApartmentId;
            
            if (!string.IsNullOrWhiteSpace(createAccessCardDto.IssuedToApartmentNumber))
            {
                var apartment = await _apartmentRepository.GetApartmentByNumberAsync(createAccessCardDto.IssuedToApartmentNumber);
                if (apartment == null)
                {
                    throw new InvalidOperationException($"Không tìm thấy căn hộ với số '{createAccessCardDto.IssuedToApartmentNumber}'");
                }
                apartmentId = apartment.ApartmentId;
            }

            var accessCard = new AccessCard
            {
                CardNumber = createAccessCardDto.CardNumber,
                Status = createAccessCardDto.Status,
                IssuedToUserId = createAccessCardDto.IssuedToUserId,
                IssuedToApartmentId = apartmentId,
                IssuedDate = createAccessCardDto.IssuedDate ?? DateTime.UtcNow,
                ExpiredDate = createAccessCardDto.ExpiredDate,
                CreatedBy = createAccessCardDto.CreatedBy ?? "buildingmanager",
                UpdatedBy = createAccessCardDto.UpdatedBy
            };

            var createdAccessCard = await _accessCardRepository.CreateAccessCardAsync(accessCard, createAccessCardDto.CardTypeIds);
            
            await _cardHistoryHelper.LogCardCreatedAsync(
                createdAccessCard.CardId, 
                createAccessCardDto.CardNumber, 
                createAccessCardDto.CreatedBy ?? "buildingmanager",
                "Tạo mới thẻ theo yêu cầu"
            );
            
            var accessCardWithDetails = await _accessCardRepository.GetAccessCardWithDetailsByIdAsync(createdAccessCard.CardId);
            return accessCardWithDetails!.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating access card");
            throw;
        }
    }

    public async Task<AccessCardDto?> UpdateAccessCardAsync(Guid id, UpdateAccessCardDto updateAccessCardDto)
    {
        try
        {
            var existingCard = await _accessCardRepository.GetAccessCardWithDetailsByIdAsync(id);
            if (existingCard == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(updateAccessCardDto.CardNumber) && 
                updateAccessCardDto.CardNumber != existingCard.CardNumber)
            {
                // Validate card number format
                ValidateCardNumberFormat(updateAccessCardDto.CardNumber);

                var cardExists = await _accessCardRepository.IsCardNumberExistsAsync(updateAccessCardDto.CardNumber, id);
                if (cardExists)
                {
                    throw new InvalidOperationException($"Card number '{updateAccessCardDto.CardNumber}' already exists");
                }
            }

            // Store old values for history logging
            var oldCardNumber = existingCard.CardNumber;
            var oldUserId = existingCard.IssuedToUserId;
            var oldApartmentId = existingCard.IssuedToApartmentId;
            var oldStatus = existingCard.Status;
            var oldIssuedDate = existingCard.IssuedDate;
            var oldExpiryDate = existingCard.ExpiredDate;
            var oldUpdatedBy = existingCard.UpdatedBy;
            var oldUserName = existingCard.IssuedToUser != null ? 
                $"{existingCard.IssuedToUser.FirstName} {existingCard.IssuedToUser.LastName}" : null;
            var oldApartmentNumber = existingCard.IssuedToApartment?.Number;
            
            var oldCapabilities = await GetCardCapabilitiesAsync(id);
            var oldCapabilityIds = oldCapabilities.Select(c => c.CardTypeId).ToList();

            // Xử lý tìm căn hộ theo số nếu người dùng nhập số căn hộ
            Guid? apartmentId = updateAccessCardDto.IssuedToApartmentId ?? existingCard.IssuedToApartmentId;
            
            if (!string.IsNullOrWhiteSpace(updateAccessCardDto.IssuedToApartmentNumber))
            {
                var apartment = await _apartmentRepository.GetApartmentByNumberAsync(updateAccessCardDto.IssuedToApartmentNumber);
                if (apartment == null)
                {
                    throw new InvalidOperationException($"Không tìm thấy căn hộ với số '{updateAccessCardDto.IssuedToApartmentNumber}'");
                }
                apartmentId = apartment.ApartmentId;
            }

            // Xử lý ngày cấp: Front-end gửi UTC time, nhưng chúng ta cần giữ nguyên ngày mà user chọn
            // Lấy phần ngày trực tiếp từ UTC time (không convert timezone)
            DateTime? newIssuedDate = null;
            if (updateAccessCardDto.IssuedDate.HasValue)
            {
                var dateValue = updateAccessCardDto.IssuedDate.Value;
                // Nếu là UTC time, lấy trực tiếp phần Date (không convert)
                // Nếu là Unspecified, giả định là UTC
                if (dateValue.Kind == DateTimeKind.Unspecified)
                {
                    dateValue = DateTime.SpecifyKind(dateValue, DateTimeKind.Utc);
                }
                // Lấy phần ngày từ UTC time (không convert timezone để tránh lệch ngày)
                // Front-end gửi toISOString() nên đã là UTC, chỉ cần lấy Date
                newIssuedDate = new DateTime(dateValue.Year, dateValue.Month, dateValue.Day, 0, 0, 0, DateTimeKind.Utc);
            }
            else
            {
                newIssuedDate = existingCard.IssuedDate;
            }

            // Xử lý ngày hết hạn: tương tự
            DateTime? newExpiredDate = null;
            if (updateAccessCardDto.ExpiredDate.HasValue)
            {
                var dateValue = updateAccessCardDto.ExpiredDate.Value;
                // Nếu là UTC time, lấy trực tiếp phần Date (không convert)
                // Nếu là Unspecified, giả định là UTC
                if (dateValue.Kind == DateTimeKind.Unspecified)
                {
                    dateValue = DateTime.SpecifyKind(dateValue, DateTimeKind.Utc);
                }
                // Lấy phần ngày từ UTC time (không convert timezone để tránh lệch ngày)
                newExpiredDate = new DateTime(dateValue.Year, dateValue.Month, dateValue.Day, 0, 0, 0, DateTimeKind.Utc);
            }
            else
            {
                newExpiredDate = existingCard.ExpiredDate;
            }

            var accessCard = new AccessCard
            {
                CardNumber = updateAccessCardDto.CardNumber ?? existingCard.CardNumber,
                Status = updateAccessCardDto.Status ?? existingCard.Status,
                IssuedToUserId = updateAccessCardDto.IssuedToUserId ?? existingCard.IssuedToUserId,
                IssuedToApartmentId = apartmentId,
                IssuedDate = newIssuedDate ?? existingCard.IssuedDate,
                ExpiredDate = newExpiredDate,
                UpdatedBy = updateAccessCardDto.UpdatedBy ?? "buildingmanager"
            };

            var updatedCard = await _accessCardRepository.UpdateAccessCardAsync(id, accessCard, updateAccessCardDto.CardTypeIds);
            if (updatedCard == null)
            {
                return null;
            }

            var changedBy = updateAccessCardDto.UpdatedBy ?? "buildingmanager";

            if (!string.IsNullOrEmpty(updateAccessCardDto.CardNumber) && updateAccessCardDto.CardNumber != oldCardNumber)
            {
                await _cardHistoryHelper.LogCardNumberChangeAsync(
                    id, 
                    oldCardNumber, 
                    updateAccessCardDto.CardNumber, 
                    changedBy,
                    "Cập nhật số thẻ"
                );
            }

            if (updateAccessCardDto.IssuedToUserId.HasValue && updateAccessCardDto.IssuedToUserId != oldUserId)
            {
                var newUser = await _context.Users.FindAsync(updateAccessCardDto.IssuedToUserId);
                var newUserName = newUser != null ? $"{newUser.FirstName} {newUser.LastName}" : null;
                await _cardHistoryHelper.LogOwnerChangeAsync(
                    id, 
                    oldUserId, 
                    updateAccessCardDto.IssuedToUserId, 
                    oldUserName, 
                    newUserName, 
                    changedBy,
                    "Cập nhật chủ sở hữu thẻ"
                );
            }

            if (updateAccessCardDto.IssuedToApartmentId.HasValue && updateAccessCardDto.IssuedToApartmentId != oldApartmentId)
            {
                var newApartment = await _context.Apartments.FindAsync(updateAccessCardDto.IssuedToApartmentId);
                await _cardHistoryHelper.LogApartmentChangeAsync(
                    id, 
                    oldApartmentId, 
                    updateAccessCardDto.IssuedToApartmentId, 
                    oldApartmentNumber, 
                    newApartment?.Number, 
                    changedBy,
                    "Cập nhật căn hộ thẻ"
                );
            }

            if (!string.IsNullOrEmpty(updateAccessCardDto.Status) && updateAccessCardDto.Status != oldStatus)
            {
                await _cardHistoryHelper.LogStatusChangeAsync(
                    id, 
                    oldStatus, 
                    updateAccessCardDto.Status, 
                    changedBy,
                    "Cập nhật trạng thái thẻ"
                );
            }

            // Chỉ lưu lịch sử ngày cấp nếu thực sự có thay đổi (so sánh theo ngày, không so sánh giờ)
            if (updateAccessCardDto.IssuedDate.HasValue)
            {
                // Lấy giá trị đã được normalize từ trên
                var newIssuedDateNormalized = newIssuedDate.Value;
                var oldIssuedDateNormalized = oldIssuedDate;
                
                // Normalize old date: lấy trực tiếp phần ngày (không convert timezone)
                if (oldIssuedDateNormalized.Kind == DateTimeKind.Unspecified)
                {
                    oldIssuedDateNormalized = DateTime.SpecifyKind(oldIssuedDateNormalized, DateTimeKind.Utc);
                }
                oldIssuedDateNormalized = new DateTime(oldIssuedDateNormalized.Year, oldIssuedDateNormalized.Month, oldIssuedDateNormalized.Day, 0, 0, 0, DateTimeKind.Utc);
                
                // So sánh theo format ngày (yyyy-MM-dd) để tránh vấn đề timezone
                var newIssuedDateStr = newIssuedDateNormalized.ToString("yyyy-MM-dd");
                var oldIssuedDateStr = oldIssuedDateNormalized.ToString("yyyy-MM-dd");
                
                // Chỉ lưu lịch sử nếu ngày thực sự khác nhau
                if (newIssuedDateStr != oldIssuedDateStr)
                {
                    await _cardHistoryHelper.LogIssuedDateChangeAsync(
                        id, 
                        oldIssuedDate, 
                        newIssuedDateNormalized, 
                        changedBy,
                        "Cập nhật ngày cấp thẻ"
                    );
                }
            }

            // Chỉ lưu lịch sử ngày hết hạn nếu thực sự có thay đổi (so sánh theo ngày, không so sánh giờ)
            if (updateAccessCardDto.ExpiredDate.HasValue)
            {
                // Lấy giá trị đã được normalize từ trên
                var newExpiredDateNormalized = newExpiredDate.Value;
                DateTime? oldExpiredDateNormalized = oldExpiryDate;
                
                // Normalize old date: lấy trực tiếp phần ngày (không convert timezone)
                if (oldExpiredDateNormalized.HasValue)
                {
                    var oldDate = oldExpiredDateNormalized.Value;
                    if (oldDate.Kind == DateTimeKind.Unspecified)
                    {
                        oldDate = DateTime.SpecifyKind(oldDate, DateTimeKind.Utc);
                    }
                    oldExpiredDateNormalized = new DateTime(oldDate.Year, oldDate.Month, oldDate.Day, 0, 0, 0, DateTimeKind.Utc);
                }
                
                // So sánh theo format ngày (yyyy-MM-dd) để tránh vấn đề timezone
                var newExpiredDateStr = newExpiredDateNormalized.ToString("yyyy-MM-dd");
                var oldExpiredDateStr = oldExpiredDateNormalized?.ToString("yyyy-MM-dd");
                
                // Chỉ lưu lịch sử nếu:
                // 1. Trước đó không có ngày hết hạn (null) và bây giờ có
                // 2. Hoặc ngày thực sự khác nhau
                if (oldExpiredDateStr == null || newExpiredDateStr != oldExpiredDateStr)
                {
                    await _cardHistoryHelper.LogExpiryChangeAsync(
                        id, 
                        oldExpiryDate, 
                        newExpiredDateNormalized, 
                        changedBy,
                        "Cập nhật ngày hết hạn thẻ"
                    );
                }
            }

            if (!string.IsNullOrEmpty(updateAccessCardDto.UpdatedBy) && updateAccessCardDto.UpdatedBy != oldUpdatedBy)
            {
                await _cardHistoryHelper.LogUpdatedByChangeAsync(
                    id, 
                    oldUpdatedBy, 
                    updateAccessCardDto.UpdatedBy, 
                    changedBy,
                    "Cập nhật người cập nhật thẻ"
                );
            }

            if (updateAccessCardDto.CardTypeIds != null && updateAccessCardDto.CardTypeIds.Any())
            {
                var newCapabilities = await GetCardCapabilitiesAsync(id);
                var newCapabilityIds = newCapabilities.Select(c => c.CardTypeId).ToList();
                
                var oldIdsSet = new HashSet<Guid>(oldCapabilityIds);
                var newIdsSet = new HashSet<Guid>(newCapabilityIds);
                
                if (!oldIdsSet.SetEquals(newIdsSet))
                {
                    await _cardHistoryHelper.LogCapabilityChangeAsync(
                        id,
                        oldCapabilityIds,
                        newCapabilityIds,
                        changedBy,
                        "Cập nhật chức năng thẻ"
                    );
                }
            }

            var accessCardWithDetails = await _accessCardRepository.GetAccessCardWithDetailsByIdAsync(updatedCard.CardId);
            return accessCardWithDetails?.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating access card with ID: {Id}", id);
            throw;
        }
    }

    public async Task<bool> SoftDeleteAccessCardAsync(Guid id)
    {
        try
        {
            return await _accessCardRepository.SoftDeleteAccessCardAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while soft deleting access card with ID: {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<CardTypeDto>> GetCardTypesAsync()
    {
        try
        {
            var cardTypes = await _context.AccessCardTypes
                .Where(ct => ct.IsActive && !ct.IsDelete)
                .Select(ct => new CardTypeDto
                {
                    CardTypeId = ct.CardTypeId,
                    Code = ct.Code,
                    Name = ct.Name,
                    Description = ct.Description,
                    IsActive = ct.IsActive
                })
                .ToListAsync();

            return cardTypes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting card types");
            throw;
        }
    }

    public async Task<IEnumerable<AccessCardCapabilityDto>> GetCardCapabilitiesAsync(Guid cardId)
    {
        try
        {
            var capabilities = await _context.AccessCardCapabilities
                .Where(acc => acc.CardId == cardId && !acc.Card.IsDelete)
                .Include(acc => acc.CardType)
                .Select(acc => new AccessCardCapabilityDto
                {
                    CardCapabilityId = acc.CardCapabilityId,
                    CardId = acc.CardId,
                    CardTypeId = acc.CardTypeId,
                    CardTypeName = acc.CardType.Name,
                    CardTypeCode = acc.CardType.Code,
                    IsEnabled = acc.IsEnabled,
                    ValidFrom = acc.ValidFrom,
                    ValidTo = acc.ValidTo,
                    CreatedAt = acc.CreatedAt,
                    UpdatedAt = acc.UpdatedAt,
                    CreatedBy = acc.CreatedBy,
                    UpdatedBy = acc.UpdatedBy
                })
                .ToListAsync();

            return capabilities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting card capabilities for card ID: {CardId}", cardId);
            throw;
        }
    }

    public async Task<bool> UpdateCardCapabilitiesAsync(Guid cardId, List<Guid> cardTypeIds)
    {
        try
        {
            var existingCard = await _context.AccessCards
                .Where(ac => ac.CardId == cardId && !ac.IsDelete)
                .FirstOrDefaultAsync();

            if (existingCard == null)
            {
                return false;
            }

            var oldCapabilities = await GetCardCapabilitiesAsync(cardId);
            var oldCapabilityIds = oldCapabilities.Select(c => c.CardTypeId).ToList();

            var existingCapabilities = await _context.AccessCardCapabilities
                .Where(acc => acc.CardId == cardId)
                .ToListAsync();
            _context.AccessCardCapabilities.RemoveRange(existingCapabilities);

            foreach (var cardTypeId in cardTypeIds)
            {
                var capability = new AccessCardCapability
                {
                    CardId = cardId,
                    CardTypeId = cardTypeId,
                    IsEnabled = true,
                    ValidFrom = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "buildingmanager"
                };
                _context.AccessCardCapabilities.Add(capability);
            }

            await _context.SaveChangesAsync();

            var newCapabilities = await GetCardCapabilitiesAsync(cardId);
            var newCapabilityIds = newCapabilities.Select(c => c.CardTypeId).ToList();
            
            var oldIdsSet = new HashSet<Guid>(oldCapabilityIds);
            var newIdsSet = new HashSet<Guid>(newCapabilityIds);
            
            if (!oldIdsSet.SetEquals(newIdsSet))
            {
                await _cardHistoryHelper.LogCapabilityChangeAsync(
                    cardId,
                    oldCapabilityIds,
                    newCapabilityIds,
                    "buildingmanager",
                    "Cập nhật chức năng thẻ"
                );
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating card capabilities for card ID: {CardId}", cardId);
            throw;
        }
    }
}