using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace SAMS_BE.Security
{
    public class DynamicPermissionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAuthorizationService _auth;

        public DynamicPermissionMiddleware(
            RequestDelegate next,
            IAuthorizationService auth)
        {
            _next = next;
            _auth = auth;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            if (endpoint == null)
            {
                await _next(context);
                return;
            }

            if (endpoint.Metadata.GetMetadata<AllowAnonymousAttribute>() != null)
            {
                await _next(context);
                return;
            }

            if (context.User?.Identity == null || !context.User.Identity.IsAuthenticated)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var cad = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
            if (cad == null)
            {
                await _next(context);
                return;
            }

            var resource = cad.ControllerName.ToLowerInvariant();
            var scope = context.Request.Method.ToUpperInvariant();

            var key = $"{resource}:{scope}";
            // Ví dụ: building:GET

            var kcResources = context.RequestServices
                .GetRequiredService<IKeycloakResourceService>();

            var protectedSet = await kcResources.GetProtectedPermissionsAsync();

            if (protectedSet == null || !protectedSet.Contains(key))
            {
                await _next(context);
                return;
            }

            var requirement = new PermissionRequirement(resource, scope);
            var result = await _auth.AuthorizeAsync(context.User, null, requirement);

            if (!result.Succeeded)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    error = "forbidden",
                    resource,
                    scope
                }));

                return;
            }

            await _next(context);
        }
    }
}
