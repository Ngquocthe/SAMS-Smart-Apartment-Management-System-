namespace SAMS_BE.Tenant
{
    public interface ITenantContextAccessor
    {
        string Schema { get; }
        void SetSchema(string schema);
    }
}
