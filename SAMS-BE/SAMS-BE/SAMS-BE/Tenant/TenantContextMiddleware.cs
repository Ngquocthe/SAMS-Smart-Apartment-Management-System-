using System.Text.RegularExpressions;

namespace SAMS_BE.Tenant
{
    public class TenantContextMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly Regex Safe = new(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

        public TenantContextMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext context, ITenantContextAccessor accessor)
        {
            var schema = context.User?.FindFirst("building_id")?.Value;

            if (string.IsNullOrWhiteSpace(schema))
            {
                schema = "building"; // Changed from "dbo" to "building" for default schema
            }

            if (!Safe.IsMatch(schema))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Invalid building_id");
                return;
            }

            accessor.SetSchema(schema);
            await _next(context);
        }
    }
}
