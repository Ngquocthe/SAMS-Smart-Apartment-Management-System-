using System.Text.RegularExpressions;

namespace SAMS_BE.Utils
{
    public static class SqlScriptTransformer
    {
        public static string TransformScript(string template, string schemaName)
        {
            if (string.IsNullOrWhiteSpace(template)) return template;
            if (string.IsNullOrWhiteSpace(schemaName))
                throw new ArgumentException(nameof(schemaName));

            // Validate schema name (chỉ cho phép alphanumeric và underscore)
            if (!Regex.IsMatch(schemaName, @"^[a-zA-Z0-9_]+$"))
                throw new ArgumentException("Invalid schema name", nameof(schemaName));

            // KHÔNG quote - template đã có [{{SCHEMA}}]
            var script = template.Replace("{{SCHEMA}}", schemaName);

            // Fix COL_LENGTH checks
            var pattern = new Regex(
                @"IF\s+COL_LENGTH\s*\(\s*'\[(?<schema>[^\]]+)\]\.(?<table>[^\]]+)'\s*,\s*'(?<col>[^']+)'\s*\)\s+IS\s+NULL",
                RegexOptions.IgnoreCase | RegexOptions.Compiled
            );

            script = pattern.Replace(script, match =>
            {
                var schema = match.Groups["schema"].Value;
                var table = match.Groups["table"].Value;
                var col = match.Groups["col"].Value;

                return $"IF NOT EXISTS (SELECT 1 FROM sys.columns c " +
                       $"JOIN sys.objects o ON c.object_id = o.object_id " +
                       $"JOIN sys.schemas s ON o.schema_id = s.schema_id " +
                       $"WHERE s.name = N'{schema}' AND o.name = N'{table}' AND c.name = N'{col}')";
            });

            // Fix OBJECT_ID checks
            var objPattern = new Regex(
                @"OBJECT_ID\s*\(\s*'\[(?<schema>[^\]]+)\]\.(?<table>[^\]]+)'\s*,\s*'(?<type>[^']*)'\s*\)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled
            );

            script = objPattern.Replace(script, match =>
            {
                var schema = match.Groups["schema"].Value;
                var table = match.Groups["table"].Value;
                var type = match.Groups["type"].Value;

                return $"OBJECT_ID(N'[{schema}].[{table}]', N'{type}')";
            });

            return script;
        }
    }
}
