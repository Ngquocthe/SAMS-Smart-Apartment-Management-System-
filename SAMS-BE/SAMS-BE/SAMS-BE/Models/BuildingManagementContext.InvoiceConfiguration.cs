using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

public partial class BuildingManagementContext
{
    public virtual DbSet<InvoiceConfiguration> InvoiceConfigurations { get; set; }
}
