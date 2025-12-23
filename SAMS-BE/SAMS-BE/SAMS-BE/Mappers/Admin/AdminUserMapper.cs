using Azure.Core;
using SAMS_BE.DTOs.Request.Resident;
using SAMS_BE.DTOs.Request.Staff;
using SAMS_BE.Infrastructure.Persistence.Global.Models;

namespace SAMS_BE.Mappers.Admin
{
    public static class AdminUserMapper
    {
        public static user_registry ToUserRegistryEntity(
            this StaffCreateRequest request,
            Guid keycloakUserId)
        {
            var now = DateTime.UtcNow;

            return new user_registry
            {
                id = Guid.NewGuid(),
                keycloak_user_id = keycloakUserId,
                username = request.Username!,
                email = request.Email!,
                status = 1,
                create_at = now,
                update_at = null
            };
        }

        public static user_building ToUserBuildingEntity(
            this StaffCreateRequest request,
            Guid keycloakUserId)
        {
            var now = DateTime.UtcNow;

            return new user_building
            {
                id = Guid.NewGuid(),
                keycloak_user_id = keycloakUserId,
                building_id = request.BuildingId,
                status = 1,
                create_at = now,
                update_at = null
            };
        }

        public static user_registry ToUserRegistryEntity(
            this CreateResidentRequest request,
            Guid keycloakUserId)
        {
            var now = DateTime.UtcNow;

            return new user_registry
            {
                id = Guid.NewGuid(),
                keycloak_user_id = keycloakUserId,
                username = request.Username!,
                email = request.Email ?? string.Empty,
                status = 1,
                create_at = now,
                update_at = null
            };
        }

        public static user_building ToUserBuildingEntity(
            this CreateResidentRequest request,
            Guid keycloakUserId,
            Guid buildingId)
        {
            var now = DateTime.UtcNow;

            return new user_building
            {
                id = Guid.NewGuid(),
                keycloak_user_id = keycloakUserId,
                building_id = buildingId,
                status = 1,
                create_at = now,
                update_at = null
            };
        }
    }
}
