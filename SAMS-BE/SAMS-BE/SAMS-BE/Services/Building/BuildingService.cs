using System.Text.RegularExpressions;
using Azure.Core;
using Microsoft.Identity.Client.Extensions.Msal;
using SAMS_BE.DTOs.Request.Building;
using SAMS_BE.DTOs.Response.Building;
using SAMS_BE.Helpers;
using SAMS_BE.Infrastructure.Persistence.Global.Models;
using SAMS_BE.Interfaces.IRepository.Building;
using SAMS_BE.Interfaces.IRepository.GlobalAdmin;
using SAMS_BE.Interfaces.IService.IBuilding;
using SAMS_BE.Utils;

namespace SAMS_BE.Services.Building
{
    public class BuildingService : IBuildingService
    {
        private readonly IBuildingRepository buildingRepository;
        private readonly IScriptRepository scriptRepo;
        private readonly IHttpContextAccessor http;
        private readonly IFileStorageHelper storage;


        private const long MAX_AVATAR_BYTES = 5 * 1024 * 1024;
        private static readonly Regex CodeRegex = new Regex(@"^[A-Za-z0-9\-_]+$", RegexOptions.Compiled);

        public BuildingService(IBuildingRepository buildingRepository, IScriptRepository scriptRepo, IHttpContextAccessor http, IFileStorageHelper storage)
        {
            this.buildingRepository = buildingRepository;
            this.scriptRepo = scriptRepo;
            this.http = http;
            this.storage = storage;
        }

        public Task<IReadOnlyList<BuildingDto>> GetAllAsync(CancellationToken ct)
            => buildingRepository.GetAllAsync(ct);

        public Task<IReadOnlyList<BuildingDto>> GetAllIncludingInactiveAsync(CancellationToken ct)
            => buildingRepository.GetAllIncludingInactiveAsync(ct);

        public async Task<building> CreateTenantAsync(CreateBuildingRequest req, CancellationToken ct)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            string SchemaName = GenerateSchemaFromCode(req.Code);

            await ValidateBuildingCreateAsync(req, SchemaName, ct);

            var path = Path.Combine(AppContext.BaseDirectory, "Infrastructure", "Scripts", "create_schema_template.sql");
            var template = await File.ReadAllTextAsync(path);

            var finalScript = SqlScriptTransformer.TransformScript(template, SchemaName);
            string? avatarUrl = null;

            var idCurrentUser = http.HttpContext?.User.GetGuidClaim("sub");

            try
            {

                if (req.Avatar is not null && req.Avatar.Length > 0)
                {
                    var avatarFile = await storage.SaveAsync(
                    req.Avatar,
                        $"{SchemaName}/avatarBuildings",
                        idCurrentUser.ToString()
                    );

                    avatarUrl = string.IsNullOrWhiteSpace(avatarFile.StoragePath)
                        ? null
                        : avatarFile.StoragePath;
                }

                await scriptRepo.ExecuteSqlScriptAsync(finalScript);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create schema ({SchemaName}). Reason: {ex.Message}", ex);
            }

            var now = DateTime.UtcNow;

            var building = new building
            {
                id = Guid.NewGuid(),
                code = req.Code,
                schema_name = SchemaName,
                building_name = req.BuildingName,
                description = req.Description,
                total_area_m2 = req.TotalAreaM2,
                opening_date = req.OpeningDate,
                latitude = req.Latitude,
                longitude = req.Longitude,
                image_url = avatarUrl,
                create_at = now,
                created_by = idCurrentUser,
            };

            await buildingRepository.SaveBuilding(building, ct);

            return building;
        }

        private string GenerateSchemaFromCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            var s = code.Trim().ToLowerInvariant();
            s = s.Replace('-', '_');

            s = Regex.Replace(s, @"[^a-z0-9_]", "");
            if (string.IsNullOrWhiteSpace(s)) return null;
            return s;
        }

        private async Task ValidateBuildingCreateAsync(CreateBuildingRequest req, string schema, CancellationToken ct = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            // ----- Code (mã tòa nhà) -----
            var code = req.Code?.Trim();
            if (string.IsNullOrWhiteSpace(code))
                throw new InvalidOperationException("Vui lòng nhập mã tòa nhà.");

            if (code.Length > 30)
                throw new InvalidOperationException("Mã tòa nhà tối đa 30 ký tự.");

            if (!CodeRegex.IsMatch(code))
                throw new InvalidOperationException("Mã tòa nhà chỉ cho phép chữ không dấu, số, gạch ngang và gạch dưới, không có khoảng trắng.");

            if (await buildingRepository.checkExistBuilding(req))
                throw new InvalidOperationException("Mã tòa nhà đã tồn tại.");

            // ----- Building name -----
            var name = req.BuildingName?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Vui lòng nhập tên tòa nhà.");

            if (name.Length > 150)
                throw new InvalidOperationException("Tên tòa nhà quá dài (tối đa 150 ký tự).");

            // ----- Total area -----
            if (req.TotalAreaM2.HasValue)
            {
                if (req.TotalAreaM2.Value < 0)
                    throw new InvalidOperationException("Tổng diện tích phải >= 0.");
            }

            // ----- Avatar validation -----
            if (req.Avatar is not null)
            {
                if (req.Avatar.Length == 0)
                    throw new InvalidOperationException("Avatar không hợp lệ.");

                if (req.Avatar.Length > MAX_AVATAR_BYTES)
                    throw new InvalidOperationException($"Kích thước avatar tối đa {MAX_AVATAR_BYTES / (1024 * 1024)} MB.");
            }
        }

        public Task<IReadOnlyList<BuildingListDropdownDto>> GetAllForDropDownAsync(CancellationToken ct)
            => buildingRepository.GetAllForDropDownAsync(ct);

        public async Task<building?> GetBuildingBySchema(string schema)
        {
            var building = await buildingRepository.GetBuildingBySchema(schema);
            if (building == null)
            {
                return null;
            }
            return building;
        }

        public async Task<BuildingDto?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var buildings = await buildingRepository.GetAllAsync(ct);
            return buildings.FirstOrDefault(b => b.Id == id);
        }

        public async Task<building?> UpdateBuildingAsync(Guid id, UpdateBuildingRequest req, CancellationToken ct)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            // Get existing building
            var existing = await buildingRepository.GetBuildingById(id, ct);
            if (existing == null)
            {
                return null;
            }

            // Validate update request
            await ValidateBuildingUpdateAsync(req, ct);

            var idCurrentUser = http.HttpContext?.User.GetGuidClaim("sub");
            string? avatarUrl = existing.image_url;

            // Handle avatar upload if provided
            if (req.Avatar is not null && req.Avatar.Length > 0)
            {
                var avatarFile = await storage.SaveAsync(
                    req.Avatar,
                    $"{existing.schema_name}/avatarBuildings",
                    idCurrentUser.ToString()
                );

                avatarUrl = string.IsNullOrWhiteSpace(avatarFile.StoragePath)
                    ? null
                    : avatarFile.StoragePath;
            }

            // Update fields
            if (!string.IsNullOrWhiteSpace(req.BuildingName))
            {
                existing.building_name = req.BuildingName;
            }
            if (req.Description != null)
            {
                existing.description = req.Description;
            }
            if (req.TotalAreaM2.HasValue)
            {
                existing.total_area_m2 = req.TotalAreaM2;
            }
            if (req.OpeningDate.HasValue)
            {
                existing.opening_date = req.OpeningDate;
            }
            if (req.Latitude.HasValue)
            {
                existing.latitude = req.Latitude;
            }
            if (req.Longitude.HasValue)
            {
                existing.longitude = req.Longitude;
            }
            if (avatarUrl != null)
            {
                existing.image_url = avatarUrl;
            }
            if (req.Status.HasValue)
            {
                existing.status = req.Status.Value;
            }
            existing.update_at = DateTime.UtcNow;
            existing.updated_by = idCurrentUser;

            await buildingRepository.UpdateBuilding(existing, ct);

            return existing;
        }

        private async Task ValidateBuildingUpdateAsync(UpdateBuildingRequest req, CancellationToken ct = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            // ----- Building name -----
            if (!string.IsNullOrWhiteSpace(req.BuildingName))
            {
                var name = req.BuildingName.Trim();
                if (name.Length > 150)
                    throw new InvalidOperationException("Tên tòa nhà quá dài (tối đa 150 ký tự).");
            }

            // ----- Status validation -----
            if (req.Status.HasValue)
            {
                if (req.Status.Value != 0 && req.Status.Value != 1)
                    throw new InvalidOperationException("Trạng thái chỉ có thể là 0 (INACTIVE) hoặc 1 (ACTIVE).");
            }

            // ----- Total area -----
            if (req.TotalAreaM2.HasValue)
            {
                if (req.TotalAreaM2.Value < 0)
                    throw new InvalidOperationException("Tổng diện tích phải >= 0.");
            }

            // ----- Avatar validation -----
            if (req.Avatar is not null)
            {
                if (req.Avatar.Length == 0)
                    throw new InvalidOperationException("Avatar không hợp lệ.");

                if (req.Avatar.Length > MAX_AVATAR_BYTES)
                    throw new InvalidOperationException($"Kích thước avatar tối đa {MAX_AVATAR_BYTES / (1024 * 1024)} MB.");
            }
        }
    }
}
