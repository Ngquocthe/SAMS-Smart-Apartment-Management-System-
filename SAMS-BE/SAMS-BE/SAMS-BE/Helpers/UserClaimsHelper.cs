using System.Security.Claims;

namespace SAMS_BE.Helpers
{
    public static class UserClaimsHelper
    {
        public static Guid GetUserIdOrThrow(ClaimsPrincipal user)
        {
            if (user == null) throw new UnauthorizedAccessException("Missing user principal");

            var id = user.FindFirst("sub")?.Value
                     ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? user.FindFirst("user_id")?.Value;

            if (!Guid.TryParse(id, out var userId) || userId == Guid.Empty)
            {
                throw new UnauthorizedAccessException("Missing or invalid user principal");
            }

            return userId;
        }

        /// <summary>
        /// Kiểm tra user có role cụ thể (không phân biệt hoa thường)
        /// </summary>
        private static bool HasRole(ClaimsPrincipal user, params string[] roles)
        {
            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
                return false;

            var userRoles = user.FindAll(ClaimTypes.Role)
                .Select(c => c.Value?.Trim())
                .Where(v => !string.IsNullOrEmpty(v))
                .Select(v => v!.ToLowerInvariant())
                .ToHashSet();

            var normalizedRoles = roles.Select(r => r.ToLowerInvariant()).ToHashSet();
            
            return userRoles.Any(ur => normalizedRoles.Contains(ur) || normalizedRoles.Any(nr => ur.Contains(nr) || nr.Contains(ur)));
        }

        /// <summary>
        /// Kiểm tra user có phải là quản lý hoặc admin không
        /// </summary>
        public static bool IsManagerOrAdmin(ClaimsPrincipal user)
        {
            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
                return false;

            return HasRole(user, "Building_Management", "building_management", "manager", "MANAGER", "Manager", 
                          "admin", "ADMIN", "Admin", "global_admin", "GLOBAL_ADMIN", "Global_Admin");
        }

        /// <summary>
        /// Kiểm tra user có phải là quản lý, admin hoặc lễ tân không
        /// </summary>
        public static bool IsManagerOrReceptionist(ClaimsPrincipal user)
        {
            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
                return false;

            return HasRole(user, "Building_Management", "building_management", "manager", "MANAGER", "Manager", 
                          "admin", "ADMIN", "Admin", "global_admin", "GLOBAL_ADMIN", "Global_Admin",
                          "receptionist", "RECEPTIONIST", "Receptionist", "building_admin", "building-manager");
        }
    }
}


