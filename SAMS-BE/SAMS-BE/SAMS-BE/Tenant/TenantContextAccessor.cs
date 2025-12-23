namespace SAMS_BE.Tenant
{
    public class TenantContextAccessor : ITenantContextAccessor
    {
        private string _schema = "building"; // Default schema cho background jobs
        public string Schema => _schema;
        public void SetSchema(string schema) => _schema = schema;
    }
}
