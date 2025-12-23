using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SAMS_BE.Models;

namespace SAMS_BE.Tenant
{
    public static class TenantSchemaBootstrapper
    {
        public static async Task EnsureSchemaExistsAsync(this BuildingManagementContext db, string schema)
        {
            var sql = $"""
        IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = @schema)
        BEGIN
            EXEC('CREATE SCHEMA [' + @schema + ']')
        END
        """;
            var p = new SqlParameter("@schema", schema);
            await db.Database.ExecuteSqlRawAsync(sql, p);
        }
    }
}
