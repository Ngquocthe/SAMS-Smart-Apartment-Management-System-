using System.Security.Claims;

namespace SAMS_BE.Utils
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid? GetGuidClaim(this ClaimsPrincipal user, string claimType)
        {
            var val = user?.Claims?.FirstOrDefault(c => c.Type == claimType)?.Value;
            return Guid.TryParse(val, out var g) ? g : null;
        }

        public static string? GetStringClaim(this ClaimsPrincipal user, string claimType)
            => user?.Claims?.FirstOrDefault(c => c.Type == claimType)?.Value;
    }
}
