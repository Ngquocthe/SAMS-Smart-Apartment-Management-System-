using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SAMS_BE.Models;

namespace SAMS_BE.Tenant
{
    public class TenantModelCacheKeyFactory : IModelCacheKeyFactory
    {
        public object Create(DbContext context, bool designTime)
        {
            var schema = (context as BuildingManagementContext)?.TenantSchema ?? "dbo";
            return (context.GetType(), schema, designTime);
        }
    }
}
