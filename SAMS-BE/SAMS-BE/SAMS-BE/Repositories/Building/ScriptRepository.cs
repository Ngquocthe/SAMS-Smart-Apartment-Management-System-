using Microsoft.Data.SqlClient;
using SAMS_BE.Interfaces.IRepository.Building;
using System.Data;

namespace SAMS_BE.Repositories.Building
{
    public class ScriptRepository : IScriptRepository
    {
        private readonly string _connectionString;
        public ScriptRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        public async Task ExecuteSqlScriptAsync(string sqlScript)
        {
            var batches = SplitSqlBatches(sqlScript);

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var tran = conn.BeginTransaction(); 
            try
            {
                foreach (var batch in batches)
                {
                    var trimmed = batch.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed)) continue;

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = trimmed;
                    cmd.CommandType = CommandType.Text;
                    cmd.Transaction = tran;
                    cmd.CommandTimeout = 60 * 5;

                    await cmd.ExecuteNonQueryAsync();
                }

                tran.Commit();
            }
            catch
            {
                try { tran.Rollback(); } catch {}
                throw;
            }
        }

        private static IEnumerable<string> SplitSqlBatches(string sql)
        {
            var lines = sql.Replace("\r\n", "\n").Split('\n');
            var sb = new System.Text.StringBuilder();
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.Equals("GO", StringComparison.OrdinalIgnoreCase))
                {
                    yield return sb.ToString();
                    sb.Clear();
                }
                else
                {
                    sb.AppendLine(rawLine);
                }
            }
            if (sb.Length > 0) yield return sb.ToString();
        }
    }
}
