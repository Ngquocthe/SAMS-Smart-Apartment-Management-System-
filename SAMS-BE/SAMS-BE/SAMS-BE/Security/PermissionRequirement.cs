using Microsoft.AspNetCore.Authorization;

namespace SAMS_BE.Security
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Resource { get; }
        public string Scope { get; }

        public PermissionRequirement(string resource, string scope)
        {
            Resource = resource;
            Scope = scope;
        }
    }
}
