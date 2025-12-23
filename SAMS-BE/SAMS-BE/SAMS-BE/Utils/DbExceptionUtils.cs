using Microsoft.Data.SqlClient;
using System.Data.Common;

namespace SAMS_BE.Utils
{
    internal static class DbExceptionUtils
    {
        public static bool IsMissingSchemaOrTable(DbException ex)
        {
            if (ex is SqlException sqlEx && (sqlEx.Number == 208 || sqlEx.Number == 2760))
                return true;

            var m = ex.Message?.ToLowerInvariant() ?? string.Empty;

            return m.Contains("invalid object name")
                || (m.Contains("relation") && m.Contains("does not exist"))
                || (m.Contains("no such table"))
                || (m.Contains("schema") && m.Contains("does not exist"));
        }

        public static bool IsMissingSchemaOrTableDeep(Exception ex)
        {
            for (var cur = ex; cur != null; cur = cur.InnerException)
            {
                if (cur is DbException dbEx && IsMissingSchemaOrTable(dbEx))
                    return true;
            }
            return false;
        }
    }
}
