using Hangfire.Dashboard;

namespace SAMS_BE.Config
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
 {
        public bool Authorize(DashboardContext context)
        {
            // Allow all in development
            // TODO: Add proper authentication for production
    return true;
            
         // For production, check user roles:
      // var httpContext = context.GetHttpContext();
     // return httpContext.User.IsInRole("admin");
        }
    }
}
