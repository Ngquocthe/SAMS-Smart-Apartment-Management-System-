using System.Security.Claims;
using System.Security.Cryptography;

namespace SAMS_BE.Utils
{
    public static class AuthGuards
    {
        public static void EnsureSubMatchesOrThrow(ClaimsPrincipal? principal, Guid expectedUserId)
        {
            if (principal is null || principal.Identity is null || !principal.Identity.IsAuthenticated)
                throw new UnauthorizedAccessException("Missing or invalid user principal");

            var sub = principal.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (string.IsNullOrWhiteSpace(sub))
                throw new UnauthorizedAccessException("Missing sub claim");

            if (Guid.TryParse(sub, out var subGuid))
            {
                if (subGuid != expectedUserId)
                    throw new UnauthorizedAccessException("User ID mismatch");
                return;
            }

            var expected = expectedUserId.ToString("D");
            if (!string.Equals(sub, expected, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("User ID mismatch");
        }

        public static void EnsureBuildingMatchesOrThrow(ClaimsPrincipal? principal, string expectedBuildingId)
        {
            if (principal is null) throw new UnauthorizedAccessException("Missing user principal");
            var buildingId = principal.FindFirstValue("building_id");
            if (!string.Equals(buildingId, expectedBuildingId, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Building mismatch");
        }

        private static string? FindFirstValue(this ClaimsPrincipal principal, string claimType)
            => principal.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;

        public static string GenerateSecurePassword(int length = 12)
        {
            const string lower = "abcdefghijkmnopqrstuvwxyz";
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string digit = "23456789";
            const string symbol = "!@#$%^&*?";
            var all = lower + upper + digit + symbol;

            var rnd = RandomNumberGenerator.Create();
            var bytes = new byte[length];
            rnd.GetBytes(bytes);

            var chars = new char[length];
            for (var i = 0; i < length; i++)
            {
                var b = bytes[i] % all.Length;
                chars[i] = all[b];
            }

            return new string(chars);
        }

    }
}
