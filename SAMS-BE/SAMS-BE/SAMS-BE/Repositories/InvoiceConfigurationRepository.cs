using Microsoft.EntityFrameworkCore;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Models;

namespace SAMS_BE.Repositories;

public class InvoiceConfigurationRepository : IInvoiceConfigurationRepository
{
    private readonly BuildingManagementContext _context;

    public InvoiceConfigurationRepository(BuildingManagementContext context)
    {
        _context = context;
    }

    public async Task<InvoiceConfiguration?> GetCurrentConfigAsync()
    {
        // Ch? l?y config ??u tiên (h? th?ng ch? có 1 config duy nh?t)
        return await _context.InvoiceConfigurations
.AsNoTracking()
     .FirstOrDefaultAsync();
    }

    public async Task<InvoiceConfiguration> CreateAsync(InvoiceConfiguration config)
    {
        config.ConfigId = Guid.NewGuid();
        config.CreatedAt = DateTime.UtcNow;

        await _context.InvoiceConfigurations.AddAsync(config);
        await _context.SaveChangesAsync();

        return config;
    }

    public async Task<InvoiceConfiguration> UpdateAsync(InvoiceConfiguration config)
    {
        config.UpdatedAt = DateTime.UtcNow;

        _context.InvoiceConfigurations.Update(config);
        await _context.SaveChangesAsync();

        return config;
    }

    public async Task<bool> ExistsAsync()
    {
        return await _context.InvoiceConfigurations.AnyAsync();
    }
}
