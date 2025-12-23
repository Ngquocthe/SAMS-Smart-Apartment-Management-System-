using SAMS_BE.DTOs.Request.Keycloak;
using SAMS_BE.DTOs.Request.Staff;
using SAMS_BE.DTOs.Response.Staff;
using SAMS_BE.Helpers;
using SAMS_BE.Interfaces.IMail;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Interfaces.IRepository.GlobalAdmin;
using SAMS_BE.Interfaces.IService.GlobalAdmin;
using SAMS_BE.Interfaces.IService.IBuilding;
using SAMS_BE.Interfaces.IService.Keycloak;
using SAMS_BE.Mappers.Admin;
using SAMS_BE.Mappers.Staff;
using SAMS_BE.Utils;
using SAMS_BE.Utils.HanldeException;

namespace SAMS_BE.Services.GlobalAdmin
{
    public sealed class StaffService : IStaffService
    {
        private readonly IStaffRepository repo;
        private readonly IUserRepository userRepository;
        private readonly IAdminUserRepository adminUserRepository;
        private readonly IKeycloakRoleService kcRoles;
        private readonly IFileStorageHelper storage;
        private readonly IWebHostEnvironment env;
        private readonly IEmailSender emailSender;
        private readonly IBuildingService buildingService;

        public StaffService(IStaffRepository repo, IKeycloakRoleService kcRoles, IFileStorageHelper storage, IUserRepository userRepository, IAdminUserRepository adminUserRepository, IWebHostEnvironment env, IEmailSender emailSender, IBuildingService buildingService)
        {
            this.repo = repo;
            this.kcRoles = kcRoles;
            this.storage = storage;
            this.userRepository = userRepository;
            this.adminUserRepository = adminUserRepository;
            this.env = env;
            this.emailSender = emailSender;
            this.buildingService = buildingService;
        }

        public async Task<(List<StaffListItemDto> Items, int Total, int Page, int PageSize)>
            SearchAsync(string schema, StaffQuery query, CancellationToken ct)
        {
            var (items, total) = await repo.SearchAsync(schema, query, ct);
            var page = Math.Max(1, query.Page);
            var size = Math.Clamp(query.PageSize, 1, 200);
            return (items, total, page, size);
        }

        public async Task<StaffDetailDto?> GetDetailAsync(string schema, Guid staffCode, CancellationToken ct)
        {
            var dto = await repo.GetDetailAsync(schema, staffCode, ct);
            if (dto is null) return null;

            var accessRoles = Array.Empty<string>();

            if (!string.IsNullOrWhiteSpace(dto.Username))
            {
                var kcUserId = await kcRoles.FindUserIdByUsernameAsync(dto.Username, ct);
                if (!string.IsNullOrEmpty(kcUserId))
                {
                    var roles = await kcRoles.GetUserClientRolesAsync(kcUserId, clientId: null, ct);
                    accessRoles = roles
                        .Select(r => r.Name)
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                }
            }

            var building = await buildingService.GetBuildingBySchema(schema);
            if (building == null)
            {
                throw new BusinessException($"Building not found for schema: {schema}");
            }

            dto.BuildingId = building.id.ToString();

            dto.AccessRoles = accessRoles;
            return dto;
        }

        public async Task<bool> UpdateAsync(string schema, Guid staffCode, StaffUpdateDto dto, CancellationToken ct)
        {
            var staffDetail = await repo.GetDetailAsync(schema, staffCode, ct);
            if (staffDetail == null)
            {
                throw new BusinessException("Nhân sự không tồn tại");
            }

            if (!Validate.IsValidPhone(dto.Phone))
            {
                throw new BusinessException("Số điện thoại không hợp lệ");
            }
            dto.Phone = Validate.NormalizePhone(dto.Phone)!;

            string? avatarUrl = staffDetail.AvatarUrl;
            if (dto.Avatar is not null && dto.Avatar.Length > 0)
            {
                var avatarFile = await storage.SaveAsync(
                    dto.Avatar,
                    $"{schema}/avatars",
                    staffDetail.Username
                );

                avatarUrl = avatarFile.StoragePath;
            }

            string? cardPhotoUrl = staffDetail.CardPhotoUrl;
            if (dto.CardPhoto is not null && dto.CardPhoto.Length > 0)
            {
                var cardFile = await storage.SaveAsync(
                    dto.CardPhoto,
                    $"{schema}/staff-card",
                    staffDetail.Username
                );

                cardPhotoUrl = cardFile.StoragePath;
            }

            if (!string.IsNullOrWhiteSpace(staffDetail.Username))
            {
                var kcUserId = await kcRoles.FindUserIdByUsernameAsync(staffDetail.Username, ct);
                if (!string.IsNullOrWhiteSpace(kcUserId))
                {
                    var currentRoles = await kcRoles.GetUserClientRolesAsync(kcUserId, clientId: "backend", ct);

                    var currentRoleNames = currentRoles.Select(r => r.Name).ToHashSet();
                    var newRoleNames = dto.AccessRoles.ToHashSet();

                    var toAdd = newRoleNames.Except(currentRoleNames).ToList();
                    var toRemove = currentRoleNames.Except(newRoleNames).ToList();

                    if (toAdd.Count > 0)
                    {
                        await kcRoles.AssignClientRolesToUserAsync(
                            kcUserId,
                            "backend",
                            toAdd,
                            ct);
                    }

                    if (toRemove.Count > 0)
                    {
                        await kcRoles.RemoveClientRolesFromUserAsync(
                            kcUserId,
                            "backend",
                            toRemove,
                            ct);
                    }
                }
            }

            var result = await repo.UpdateAsync(schema, staffCode, dto, avatarUrl, cardPhotoUrl, ct);
            if (!result)
            {
                throw new BusinessException("Cập nhật nhân sự thất bại");
            }

            return true;
        }


        public Task<bool> ActivateAsync(string schema, Guid staffCode, CancellationToken ct)
            => repo.ActivateAsync(schema, staffCode, ct);

        public Task<bool> DeactivateAsync(string schema, Guid staffCode, DateTime? date, CancellationToken ct)
            => repo.DeactivateAsync(schema, staffCode, date, ct);

        public async Task<Guid> CreateAsync(string schema, StaffCreateRequest request, CancellationToken ct)
        {
            await ValidateStaffCreateAsync(schema, request, ct);

            string? avatarUrl = null;
            if (request.Avatar is not null && request.Avatar.Length > 0)
            {
                var avatarFile = await storage.SaveAsync(
                    request.Avatar,
                    $"{schema}/avatars",
                    request.Username
                );

                avatarUrl = string.IsNullOrWhiteSpace(avatarFile.StoragePath)
                    ? null
                    : avatarFile.StoragePath;
            }

            string? cardPhotoUrl = null;
            if (request.CardPhoto is not null && request.CardPhoto.Length > 0)
            {
                var cardFile = await storage.SaveAsync(
                    request.CardPhoto,
                    $"{schema}/staff-card",
                    request.Username
                );

                cardPhotoUrl = string.IsNullOrWhiteSpace(cardFile.StoragePath)
                    ? null
                    : cardFile.StoragePath;
            }

            string? keycloakUserId = null;
            string? tempPassword = null;

            Guid? keycloakGuid = null;

            try
            {
                var kcUser = new KeycloakUserCreateDto
                {
                    Username = request.Username!,
                    Email = request.Email!,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Enabled = true
                };

                (keycloakUserId, tempPassword) = await kcRoles.CreateUserAsync(kcUser, ct);

                var staffCode = Guid.NewGuid();

                if (string.IsNullOrWhiteSpace(keycloakUserId) || !Guid.TryParse(keycloakUserId, out var parsedGuid))
                {
                    throw new Exception("Keycloak userId is not a valid UUID");
                }

                keycloakGuid = parsedGuid;

                var user = request.ToUserEntity(keycloakGuid.Value, avatarUrl);
                var staff = request.ToStaffProfileEntity(staffCode, keycloakGuid.Value, cardPhotoUrl);

                user.StaffProfiles.Add(staff);

                if (request.AccessRoles is { Count: > 0 })
                {
                    await kcRoles.AssignClientRolesToUserAsync(
                        keycloakUserId,
                        clientId: "backend",  
                        roleNames: request.AccessRoles,
                        ct: ct
                    );
                }

                await userRepository.CreateUserAsync(schema, user, ct);

                var reg = request.ToUserRegistryEntity(keycloakGuid.Value);
                var ub = request.ToUserBuildingEntity(keycloakGuid.Value);
                reg.user_buildings.Add( ub );
                await adminUserRepository.CreateUserRegistryAsync(reg, ct);

                if (!string.IsNullOrWhiteSpace(request.Email) && !string.IsNullOrWhiteSpace(tempPassword))
                {
                    try
                    {
                        var htmlBody = await BuildStaffCreatedEmailBodyAsync(
                            request,
                            tempPassword!,
                            ct);

                        await emailSender.SendEmailAsync(
                            request.Email!,
                            "[NOAH] Thông tin tài khoản đăng nhập hệ thống",
                            htmlBody);
                    }
                    catch (Exception)
                    {
                        throw new BusinessException("Gửi email không thành công");
                    }
                }

                return staffCode;
            }
            catch
            {
                if (!string.IsNullOrWhiteSpace(keycloakUserId))
                {
                    try
                    {
                        await kcRoles.DeleteUserAsync(keycloakUserId, ct);
                    }
                    catch
                    {
                        throw new InvalidOperationException($"User is not exist");
                    }
                }

                try
                {
                    await userRepository.DeleteUserAsync(schema, keycloakGuid.Value, ct);
                }
                catch
                {
                    throw new BusinessException($"User is not exist");
                }

                throw;
            }
        }

        private async Task ValidateStaffCreateAsync(string schema, StaffCreateRequest request, CancellationToken ct)
        {
            var username = request.Username?.Trim();
            if (!string.IsNullOrWhiteSpace(username))
            {
                if (await userRepository.ExistsUsernameAsync(schema, username, ct))
                {
                    throw new BusinessException("Username đã được sử dụng trong hệ thống tòa nhà.");
                }

                if (await adminUserRepository.ExistsUsernameAsync(username, ct))
                {
                    throw new BusinessException("Username đã được sử dụng trong hệ thống quản lý chung.");
                }

                var kcByUsername = await kcRoles.FindUserIdByUsernameAsync(username, ct);
                if (!string.IsNullOrEmpty(kcByUsername))
                {
                    throw new BusinessException("Username đã tồn tại trên Keycloak.");
                }
            }

            var email = request.Email?.Trim();
            if (!string.IsNullOrWhiteSpace(email))
            {
                if (await userRepository.ExistsEmailAsync(schema, email, ct))
                {
                    throw new BusinessException("Email đã được sử dụng trong hệ thống tòa nhà.");
                }

                if (await adminUserRepository.ExistsEmailAsync(email, ct))
                {
                    throw new BusinessException("Email đã được sử dụng trong hệ thống quản lý chung.");
                }

                var kcByEmail = await kcRoles.FindUserIdByEmailAsync(email, ct);
                if (!string.IsNullOrEmpty(kcByEmail))
                {
                    throw new BusinessException("Email đã tồn tại trên Keycloak.");
                }
            }

            var phone = request.Phone?.Trim();
            if (!string.IsNullOrEmpty(phone))
            {
                if (!Validate.IsValidPhone(phone))
                {
                    throw new BusinessException("Số điện thoại không hợp lệ.");
                }

                var normalizedPhone = Validate.NormalizePhone(phone);

                if (await userRepository.ExistsPhoneAsync(schema, normalizedPhone!, ct))
                {
                    throw new BusinessException("Số điện thoại đã được sử dụng.");
                }
                request.Phone = normalizedPhone;
            }

            var emergencyContactPhone = request.EmergencyContactPhone?.Trim();
            if (!string.IsNullOrEmpty(emergencyContactPhone))
            {
                if (!Validate.IsValidPhone(phone))
                {
                    throw new BusinessException("Số điện thoại không hợp lệ.");
                }

                var normalizedPhone = Validate.NormalizePhone(phone);

                request.EmergencyContactPhone = normalizedPhone;
            }

            var taxCode = request.TaxCode?.Trim();
            if (!string.IsNullOrWhiteSpace(taxCode))
            {
                if (await repo.ExistsTaxCodeAsync(schema, taxCode, ct))
                {
                    throw new BusinessException("Mã số thuế đã được sử dụng.");
                }
            }

            var siNo = request.SocialInsuranceNo?.Trim();
            if (!string.IsNullOrWhiteSpace(siNo))
            {
                if (await repo.ExistsSocialInsuranceNoAsync(schema, siNo, ct))
                {
                    throw new BusinessException("Mã bảo hiểm xã hội đã được sử dụng.");
                }
            }
        }

        private async Task<string> BuildStaffCreatedEmailBodyAsync(StaffCreateRequest request, string tempPassword, CancellationToken ct)
        {
            var templatePath = Path.Combine(
                env.ContentRootPath,
                "EmailTemplates",
                "StaffCreatedPasswordEmail.html");

            var template = await File.ReadAllTextAsync(templatePath, ct);

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

    }
}
