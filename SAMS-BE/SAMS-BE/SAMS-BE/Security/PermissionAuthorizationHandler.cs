using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

namespace SAMS_BE.Security
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            var user = context.User;
            if (user?.Identity == null || !user.Identity.IsAuthenticated)
                return Task.CompletedTask;

            // 1) Try Keycloak "authorization" JSON claim (RPT)
            var authJson = user.FindFirst("authorization")?.Value;
            if (!string.IsNullOrEmpty(authJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(authJson);
                    if (doc.RootElement.TryGetProperty("permissions", out var perms))
                    {
                        foreach (var p in perms.EnumerateArray())
                        {
                            // Keycloak may use "rsname" or "resource_name" or "rsid"
                            if (p.TryGetProperty("rsname", out var rsname) && rsname.GetString() == requirement.Resource)
                            {
                                if (HasScope(p, requirement.Scope))
                                {
                                    context.Succeed(requirement);
                                    return Task.CompletedTask;
                                }
                            }

                            if (p.TryGetProperty("resource_name", out var rname) && rname.GetString() == requirement.Resource)
                            {
                                if (HasScope(p, requirement.Scope))
                                {
                                    context.Succeed(requirement);
                                    return Task.CompletedTask;
                                }
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                    // fallback to other checks
                }
            }

            // 2) Top-level "permissions" claims (strings like "invoices#get")
            var topPermissions = user.FindAll("permissions").Select(c => c.Value);
            foreach (var perm in topPermissions)
            {
                if (string.Equals(perm, $"{requirement.Resource}#{requirement.Scope}", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(perm, $"{requirement.Resource}:{requirement.Scope}", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(perm, $"{requirement.Resource}#{requirement.Scope.ToUpperInvariant()}", StringComparison.OrdinalIgnoreCase))
                {
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }
            }

            // 3) Fallback role-based (admin bypass)
            if (user.IsInRole("admin") || user.IsInRole("ROLE_admin") || user.IsInRole("ROLE_ADMIN"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }

        private static bool HasScope(JsonElement permElement, string scope)
        {
            if (permElement.TryGetProperty("scopes", out var scopesEl) && scopesEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var s in scopesEl.EnumerateArray())
                {
                    if (s.GetString() == scope) return true;
                }
            }

            // Some KC versions embed "scope" as single string property
            if (permElement.TryGetProperty("scope", out var scopeEl) && scopeEl.ValueKind == JsonValueKind.String)
            {
                if (scopeEl.GetString() == scope) return true;
            }

            return false;
        }
    }
}
