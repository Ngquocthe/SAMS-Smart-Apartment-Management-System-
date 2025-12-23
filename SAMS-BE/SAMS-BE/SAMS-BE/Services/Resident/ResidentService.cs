using SAMS_BE.DTOs.Request.Resident;
using SAMS_BE.Interfaces.IMail;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Interfaces.IRepository.GlobalAdmin;
using SAMS_BE.Interfaces.IRepository.Resident;
using SAMS_BE.Interfaces.IService.IResident;
using SAMS_BE.Interfaces.IService.IBuilding;
using SAMS_BE.Interfaces.IService.Keycloak;
using SAMS_BE.Mappers;
using SAMS_BE.Mappers.Admin;
using SAMS_BE.Models;
using SAMS_BE.Tenant;
using SAMS_BE.Utils;
using SAMS_BE.Utils.HanldeException;
using SAMS_BE.DTOs;

namespace SAMS_BE.Services.Resident
{
    public class ResidentService : IResidentService
    {
        private readonly IResidentRepository _residentRepository;
        private readonly IUserRepository _userRepository;
        private readonly IAdminUserRepository _adminUserRepository;
        private readonly IKeycloakRoleService _kcRoles;
        private readonly IEmailSender _emailSender;
        private readonly IWebHostEnvironment _env;
        private readonly ITenantContextAccessor _tenant;
        private readonly IBuildingService _buildingService;

        private string Schema => string.IsNullOrWhiteSpace(_tenant?.Schema) ? "building": _tenant.Schema;

        public ResidentService(
            IResidentRepository residentRepository,
            IUserRepository userRepository,
            IAdminUserRepository adminUserRepository,
            IKeycloakRoleService kcRoles,
            IEmailSender emailSender,
            IWebHostEnvironment env,
            ITenantContextAccessor tenant,
            IBuildingService buildingService)
        {
            _residentRepository = residentRepository;
            _userRepository = userRepository;
            _adminUserRepository = adminUserRepository;
            _kcRoles = kcRoles;
            _emailSender = emailSender;
            _env = env;
            _tenant = tenant;
            _buildingService = buildingService;
        }

        public async Task<Guid> CreateResidentAsync(CreateResidentRequest dto, CancellationToken ct)
        {
            // 1. Validate & check duplicate Email / Phone
            await ValidateBasicContactAsync(dto, ct);

            // 2. Validate IdNumber uniqueness (current building + placeholder for cross-building)
            await ValidateIdNumberAsync(dto, ct);

            // 3. Validate apartments & relations
            await ValidateApartmentsAsync(dto, ct);

            // 4. Create entities & optionally create user account
            string? keycloakUserId = null;
            string? tempPassword = null;
            Guid? userId = null;

            try
            {
                // 4.x – create Keycloak account (if username provided)
                if (!string.IsNullOrWhiteSpace(dto.Username))
                {
                    (keycloakUserId, tempPassword, userId) =
                        await CreateKeycloakUserAsync(dto, ct);
                }

                // Nếu chưa có userId (không tạo tài khoản) thì tạo Guid mới cho resident thôi
                var residentId = Guid.NewGuid();

                // 4.5 – create User in DB if account created (same schema)
                User? userEntity = null;
                if (userId.HasValue)
                {
                    userEntity = dto.ToUserEntity(userId.Value);
                    await _userRepository.CreateUserAsync(Schema, userEntity, ct);

                    // 4.5.1 – create UserRegistry in global database
                    var building = await _buildingService.GetBuildingBySchema(Schema);
                    if (building == null)
                    {
                        throw new BusinessException("Không tìm thấy thông tin tòa nhà");
                    }

                    var reg = dto.ToUserRegistryEntity(userId.Value);
                    var ub = dto.ToUserBuildingEntity(userId.Value, building.id);
                    reg.user_buildings.Add(ub);
                    await _adminUserRepository.CreateUserRegistryAsync(reg, ct);
                }

                var residentProfile = dto.ToResidentProfileEntity(residentId, userId);

                // Map apartments
                foreach (var ap in dto.Apartments!)
                {
                    var residentApartment = ap.ToResidentApartmentEntity(residentId);
                    residentProfile.ResidentApartments.Add(residentApartment);
                }

                await _residentRepository.CreateResidentAsync(Schema, residentProfile, ct);

                // 4.6 – send account info email (if account created)
                if (!string.IsNullOrWhiteSpace(dto.Email) &&
                    !string.IsNullOrWhiteSpace(tempPassword))
                {
                    await SendAccountEmailAsync(dto, tempPassword!, ct);
                }

                return residentId;
            }
            catch
            {
                // 4.5 – rollback Keycloak user if needed
                if (!string.IsNullOrWhiteSpace(keycloakUserId))
                {
                    try
                    {
                        await _kcRoles.DeleteUserAsync(keycloakUserId, ct);
                    }
                    catch
                    {
                        throw new BusinessException("Thêm mới cư dân không thành công");
                    }
                }

                // 4.5 – rollback DB user if created
                if (userId.HasValue)
                {
                    try
                    {
                        await _userRepository.DeleteUserAsync(Schema, userId.Value, ct);
                    }
                    catch
                    {
                        throw new BusinessException("Thêm mới cư dân không thành công");
                    }
                }

                throw;
            }
        }

        public async Task<Guid> AddExistingResidentToBuildingAsync(AddExistingResidentRequest dto, CancellationToken ct)
        {
            // 1. Validate IdNumber
            var idNumber = dto.IdNumber?.Trim();
            if (string.IsNullOrWhiteSpace(idNumber))
            {
                throw new BusinessException("Số giấy tờ không được để trống");
            }

            // 2. Kiểm tra resident đã tồn tại trong tòa hiện tại chưa
            var existsInCurrent = await _residentRepository.ExistsIdNumberAsync(Schema, idNumber, ct);
            if (existsInCurrent)
            {
                throw new BusinessException("Cư dân đã tồn tại trong tòa nhà hiện tại");
            }

            // 3. Tìm resident trong các tòa khác
            ResidentProfile? existingResident = null;
            string? foundSchema = null;

            // Tìm trong các tòa khác (đã kiểm tra tòa hiện tại ở trên)
            var allBuildings = await _buildingService.GetAllAsync(ct);
            if (allBuildings != null)
            {
                foreach (var building in allBuildings)
                {
                    var otherSchema = building.SchemaName;
                    if (string.IsNullOrWhiteSpace(otherSchema) ||
                        string.Equals(otherSchema, Schema, StringComparison.OrdinalIgnoreCase))
                    {
                        continue; // bỏ qua schema hiện tại hoặc schema rỗng
                    }

                    existingResident = await _residentRepository.GetResidentByIdNumberAsync(otherSchema, idNumber, ct);
                    if (existingResident != null)
                    {
                        foundSchema = otherSchema;
                        break;
                    }
                }
            }

            if (existingResident == null)
            {
                throw new BusinessException("Không tìm thấy cư dân với số giấy tờ này trong hệ thống");
            }

            // 4. Validate apartments & relations
            if (dto.Apartments == null || dto.Apartments.Count == 0)
            {
                throw new BusinessException("Cư dân phải được gán ít nhất một căn hộ");
            }

            foreach (var ap in dto.Apartments)
            {
                // 4.1 – apartment exists?
                var apartmentExists = await _residentRepository.ApartmentExistsAsync(Schema, ap.ApartmentId, ct);
                if (!apartmentExists)
                {
                    throw new BusinessException("Căn hộ không tồn tại trên hệ thống.");
                }

                // 4.2 – EndDate > StartDate
                if (ap.EndDate.HasValue && ap.StartDate.HasValue &&
                    ap.EndDate.Value <= ap.StartDate.Value)
                {
                    throw new BusinessException("Ngày kết thúc phải lớn hơn ngày bắt đầu.");
                }

                // 4.3 – check current ownership/primary status
                var (hasOwner, hasPrimary) =
                    await _residentRepository.GetApartmentOwnershipStatusAsync(Schema, ap.ApartmentId, ct);

                if (ap.RelationType == Constant.Resident.Owner && hasOwner)
                {
                    throw new BusinessException("Căn hộ đã có cư dân khác sở hữu.");
                }

                if (ap.IsPrimary && hasPrimary)
                {
                    throw new BusinessException("Căn hộ đã có cư dân chịu trách nhiệm chính.");
                }

                // If no owner yet, first owner must be primary
                if (!hasOwner &&
                    ap.RelationType == Constant.Resident.Owner &&
                    !ap.IsPrimary)
                {
                    throw new BusinessException("Người sở hữu đầu tiên của căn hộ phải là người chịu trách nhiệm chính.");
                }
            }

            string? keycloakUserId = null;
            string? tempPassword = null;
            Guid? userId = existingResident.UserId;

            try
            {
                if (!userId.HasValue && !string.IsNullOrWhiteSpace(dto.Username))
                {
                    if (string.IsNullOrWhiteSpace(dto.Email))
                    {
                        throw new BusinessException("Yêu cầu nhập email.");
                    }

                    var fakeCreateDto = new CreateResidentRequest
                    {
                        Username = dto.Username.Trim(),
                        Email = string.IsNullOrWhiteSpace(existingResident.Email) ? existingResident.Email : dto.Email?.Trim(),
                        FirstName = existingResident.FullName,
                        LastName = string.Empty,
                        Phone = existingResident.Phone,
                        IdNumber = existingResident.IdNumber
                    };

                    (keycloakUserId, tempPassword, userId) =
                        await CreateKeycloakUserAsync(fakeCreateDto, ct);

                    var userEntity = fakeCreateDto.ToUserEntity(userId!.Value);
                    await _userRepository.CreateUserAsync(Schema, userEntity, ct);

                    var building = await _buildingService.GetBuildingBySchema(Schema);
                    if (building == null)
                    {
                        throw new BusinessException("Không tìm thấy thông tin tòa nhà");
                    }

                    var reg = fakeCreateDto.ToUserRegistryEntity(userId.Value);
                    var ub = fakeCreateDto.ToUserBuildingEntity(userId.Value, building.id);
                    reg.user_buildings.Add(ub);

                    await _adminUserRepository.CreateUserRegistryAsync(reg, ct);

                    // 5.4 – gửi email nếu có
                    if (!string.IsNullOrWhiteSpace(fakeCreateDto.Email) &&
                        !string.IsNullOrWhiteSpace(tempPassword))
                    {
                        await SendAccountEmailAsync(fakeCreateDto, tempPassword!, ct);
                    }
                }

                var newResidentId = Guid.NewGuid();
                var newResidentProfile = new ResidentProfile
                {
                    ResidentId = newResidentId,
                    UserId = userId,
                    FullName = existingResident.FullName,
                    Phone = existingResident.Phone,
                    Email = string.IsNullOrWhiteSpace(existingResident.Email) ? existingResident.Email : dto.Email?.Trim(),
                    IdNumber = existingResident.IdNumber,
                    Dob = existingResident.Dob,
                    Gender = existingResident.Gender,
                    Address = existingResident.Address,
                    Status = existingResident.Status,
                    IsVerified = existingResident.IsVerified,
                    VerifiedAt = existingResident.VerifiedAt,
                    Nationality = existingResident.Nationality,
                    InternalNote = existingResident.InternalNote,
                    Meta = existingResident.Meta,
                    CreatedAt = DateTime.UtcNow.AddHours(7),
                    UpdatedAt = null
                };

                foreach (var ap in dto.Apartments!)
                {
                    var residentApartment = ap.ToResidentApartmentEntity(newResidentId);
                    newResidentProfile.ResidentApartments.Add(residentApartment);
                }

                await _residentRepository.CreateResidentAsync(Schema, newResidentProfile, ct);

                return newResidentId;
            }
            catch
            {
                // ROLLBACK
                if (!string.IsNullOrWhiteSpace(keycloakUserId))
                {
                    try
                    {
                        await _kcRoles.DeleteUserAsync(keycloakUserId, ct);
                    }
                    catch
                    {
                        throw new BusinessException("Thêm cư dân không thành công");
                    }
                }

                if (userId.HasValue && existingResident.UserId == null)
                {
                    try
                    {
                        await _userRepository.DeleteUserAsync(Schema, userId.Value, ct);
                    }
                    catch
                    {
                        throw new BusinessException("Thêm cư dân không thành công");
                    }
                }

                throw;
            }
        }

        private async Task ValidateBasicContactAsync(CreateResidentRequest dto, CancellationToken ct)
        {

            // Email
            var email = dto.Email?.Trim();
            if (!string.IsNullOrWhiteSpace(email))
            {
                var existsEmailInResident = await _residentRepository.ExistsEmailAsync(Schema, email, ct);
                var existsEmailInUser = await _userRepository.ExistsEmailAsync(Schema, email, ct);

                if (existsEmailInResident || existsEmailInUser)
                {
                    throw new BusinessException("Email đã được sử dụng");
                }

                dto.Email = email;
            }

            // Phone
            var phone = dto.Phone?.Trim();
            if (!string.IsNullOrWhiteSpace(phone))
            {
                if (!Validate.IsValidPhone(phone))
                {
                    throw new BusinessException("Số điện thoại không hợp lệ");
                }

                var normalized = Validate.NormalizePhone(phone)!;

                var existsPhoneInResident = await _residentRepository.ExistsPhoneAsync(Schema, normalized, ct);
                var existsPhoneInUser = await _userRepository.ExistsPhoneAsync(Schema, normalized, ct);

                if (existsPhoneInResident || existsPhoneInUser)
                {
                    throw new BusinessException("Số điện thoại đã được sử dụng");
                }

                dto.Phone = normalized;
            }
        }

        private async Task ValidateIdNumberAsync(CreateResidentRequest dto, CancellationToken ct)
        {
            var idNumber = dto.IdNumber?.Trim();
            if (string.IsNullOrWhiteSpace(idNumber))
            {
                return;
            }

            // 2.1 – check in current building (current schema)
            var existsInCurrent = await _residentRepository.ExistsIdNumberAsync(Schema, idNumber, ct);

            if (existsInCurrent)
            {
                throw new BusinessException("Thông tin cư dân đã tồn tại trên hệ thống tòa nhà hiện tại.");
            }

            // 2.2 – check across other buildings (other schemas)
            var allBuildings = await _buildingService.GetAllAsync(ct);
            if (allBuildings != null)
            {
                foreach (var building in allBuildings)
                {
                    var otherSchema = building.SchemaName;
                    if (string.IsNullOrWhiteSpace(otherSchema) ||
                        string.Equals(otherSchema, Schema, StringComparison.OrdinalIgnoreCase))
                    {
                        continue; // bỏ qua schema hiện tại hoặc schema rỗng
                    }

                    var existsInOther = await _residentRepository.ExistsIdNumberAsync(otherSchema, idNumber, ct);
                    if (existsInOther)
                    {
                        var ex = new BusinessException("Thông tin cư dân đã tồn tại trong một tòa nhà khác, bạn có muốn đồng bộ thông tin sang tòa nhà hiện tại?");

                        // gắn mã lỗi duy nhất tại đây
                        ex.Data["code"] = "RESIDENT_EXISTS_OTHER_BUILDING";

                        throw ex;
                    }
                }
            }

            dto.IdNumber = idNumber;
        }

        private async Task ValidateApartmentsAsync(CreateResidentRequest dto, CancellationToken ct)
        {

            if (dto.Apartments == null || dto.Apartments.Count == 0)
            {
                throw new BusinessException("Cư dân phải được gán ít nhất một căn hộ");
            }

            foreach (var ap in dto.Apartments)
            {
                // 3.1 – apartment exists?
                var apartmentExists = await _residentRepository.ApartmentExistsAsync(Schema, ap.ApartmentId, ct);
                if (!apartmentExists)
                {
                    throw new BusinessException("Căn hộ không tồn tại trên hệ thống.");
                }

                // 3.3 – EndDate > StartDate
                if (ap.EndDate.HasValue && ap.StartDate.HasValue &&
                    ap.EndDate.Value <= ap.StartDate.Value)
                {
                    throw new BusinessException("Ngày kết thúc phải lớn hơn ngày bắt đầu.");
                }

                // 3.2 – check current ownership/primary status
                var (hasOwner, hasPrimary) =
                    await _residentRepository.GetApartmentOwnershipStatusAsync(Schema, ap.ApartmentId, ct);

                if (ap.RelationType == Constant.Resident.Owner && hasOwner)
                {
                    throw new BusinessException("Căn hộ đã có cư dân khác sở hữu.");
                }

                if (ap.IsPrimary && hasPrimary)
                {
                    throw new BusinessException("Căn hộ đã có cư dân chịu trách nhiệm chính.");
                }

                // If no owner yet, first owner must be primary
                if (!hasOwner &&
                    ap.RelationType == Constant.Resident.Owner &&
                    !ap.IsPrimary)
                {
                    throw new BusinessException("Người sở hữu đầu tiên của căn hộ phải là người chịu trách nhiệm chính.");
                }
            }
        }

        private async Task<(string keycloakUserId, string tempPassword, Guid? userId)> CreateKeycloakUserAsync(
            CreateResidentRequest dto,
            CancellationToken ct)
        {
            var username = dto.Username!.Trim();

            // 4.1 – check username in DB (all users in this building)
            var existsUsername = await _userRepository.ExistsUsernameAsync(Schema, username, ct);
            if (existsUsername)
            {
                throw new BusinessException("Tên tài khoản đã được sử dụng");
            }

            // 4.1 – check username on Keycloak
            var kcByUsername = await _kcRoles.FindUserIdByUsernameAsync(username, ct);
            if (!string.IsNullOrEmpty(kcByUsername))
            {
                throw new BusinessException("Tên tài khoản đã được sử dụng");
            }

            // 4.2 – check email on Keycloak
            var email = dto.Email?.Trim();
            if (!string.IsNullOrWhiteSpace(email))
            {
                var kcByEmail = await _kcRoles.FindUserIdByEmailAsync(email, ct);
                if (!string.IsNullOrEmpty(kcByEmail))
                {
                    throw new BusinessException("Email đã được sử dụng.");
                }
            }

            try
            {
                var kcUser = new DTOs.Request.Keycloak.KeycloakUserCreateDto
                {
                    Username = username,
                    Email = email ?? string.Empty,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Enabled = true
                };

                var (kcUserId, tempPwd) = await _kcRoles.CreateUserAsync(kcUser, ct);

                // Gán role "resident" cho tài khoản cư dân trên Keycloak
                try
                {
                    await _kcRoles.AssignClientRolesToUserAsync(
                        kcUserId,
                        clientId: "backend",
                        roleNames: new[] { "resident" },
                        ct: ct);
                }
                catch
                {
                    // Nếu gán role thất bại thì coi như tạo account không thành công
                    throw new BusinessException("Tạo tài khoản không thành công");
                }

                if (string.IsNullOrWhiteSpace(kcUserId) ||
                    !Guid.TryParse(kcUserId, out var parsedGuid))
                {
                    throw new BusinessException("Tạo tài khoản không thành công");
                }

                return (kcUserId, tempPwd, parsedGuid);
            }
            catch
            {
                throw new BusinessException("Tạo tài khoản không thành công");
            }
        }

        private async Task SendAccountEmailAsync(CreateResidentRequest dto, string tempPassword, CancellationToken ct)
        {
            try
            {
                var htmlBody = await BuildResidentCreatedEmailBodyAsync(dto, tempPassword, ct);

                await _emailSender.SendEmailAsync(
                    dto.Email!,
                    "[NOAH] Thông tin tài khoản cư dân",
                    htmlBody);
            }
            catch
            {
                throw new BusinessException("Gửi email không thành công");
            }
        }

        private async Task<string> BuildResidentCreatedEmailBodyAsync(CreateResidentRequest request, string tempPassword, CancellationToken ct)
        {
            var templatePath = Path.Combine(
                _env.ContentRootPath,
                "EmailTemplates",
                "StaffCreatedPasswordEmail.html");

            var template = await System.IO.File.ReadAllTextAsync(templatePath, ct);

            var fullName = $"{request.LastName} {request.FirstName}".Trim();

            var html = template
                .Replace("{{CompanyName}}", "NOAH Technology")
                .Replace("{{AppName}}", "NOAH Building Management")
                .Replace("{{FullName}}", fullName)
                .Replace("{{Username}}", request.Username ?? string.Empty)
                .Replace("{{TempPassword}}", tempPassword)
                .Replace("{{LoginUrl}}", "https://noahbuilding.me/login")
                .Replace("{{SupportEmail}}", "support@noahbuilding.me");

            return html;
        }

        public async Task<DTOs.Response.Resident.ResidentDto?> GetResidentByIdAsync(Guid residentId, CancellationToken ct = default)
        {
            var entity = await _residentRepository.GetWithApartmentsAsync(residentId, ct);
            if (entity == null) return null;
            return MapToDto(entity);
        }

        public async Task<IEnumerable<DTOs.Response.Resident.ResidentDto>> GetResidentsByApartmentIdAsync(Guid apartmentId, CancellationToken ct = default)
        {
            var entities = await _residentRepository.GetByApartmentIdAsync(apartmentId, ct);
            return (IEnumerable<DTOs.Response.Resident.ResidentDto>)entities.Select(MapToDto).ToList();
        }

        public async Task<PagedResult<DTOs.Response.Resident.ResidentDto>> GetResidentsPagedAsync(
            string schema,
            int pageNumber,
            int pageSize,
            Guid? apartmentId = null,
            string? q = null,
            string? sortBy = null,
            string? sortDir = null,
            CancellationToken ct = default)
        {
            var pagedEntities = await _residentRepository.GetResidentsPagedAsync(schema, pageNumber, pageSize, apartmentId, q, sortBy, sortDir, ct);

            var dtoItems = pagedEntities.Items.Select(MapToDto).ToList();

            var dtoPaged = new PagedResult<DTOs.Response.Resident.ResidentDto>
            {
                Items = dtoItems,
                TotalCount = pagedEntities.TotalCount,
                TotalItems = dtoItems.Count,
                PageNumber = pagedEntities.PageNumber,
                PageSize = pagedEntities.PageSize,
                TotalPages = pagedEntities.TotalPages
            };

            return dtoPaged;
        }

        private DTOs.Response.Resident.ResidentDto MapToDto(Models.ResidentProfile e)
        {
            var dto = new DTOs.Response.Resident.ResidentDto
            {
                ResidentId = e.ResidentId,
                UserId = e.UserId,
                FullName = e.FullName,
                Phone = e.Phone,
                Email = e.Email,
                IdNumber = e.IdNumber,
                Dob = e.Dob,
                Gender = e.Gender,
                Address = e.Address,
                Status = e.Status,
                IsVerified = e.IsVerified,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            };

            if (e.ResidentApartments != null)
            {
                dto.Apartments = e.ResidentApartments.Select(ra => new DTOs.Response.Resident.ResidentApartmentDto
                {
                    ResidentApartmentId = ra.ResidentApartmentId,
                    ApartmentId = ra.ApartmentId,
                    RelationType = ra.RelationType,
                    StartDate = ra.StartDate,
                    EndDate = ra.EndDate,
                    IsPrimary = ra.IsPrimary,
                    Apartment = ra.Apartment == null ? null : new DTOs.Response.Apartment.ApartmentSummaryDto
                    {
                        ApartmentId = ra.Apartment.ApartmentId,
                        FloorId = ra.Apartment.FloorId,
                        Number = ra.Apartment.Number,
                        AreaM2 = ra.Apartment.AreaM2,
                        Bedrooms = ra.Apartment.Bedrooms,
                        Status = ra.Apartment.Status,
                        Type = ra.Apartment.Type,
                        Image = ra.Apartment.Image
                    }
                }).ToList();
            }

            return dto;
        }
    }
}
