using Microsoft.EntityFrameworkCore;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Models;

namespace SAMS_BE.Repositories;

public class AssetMaintenanceHistoryRepository : IAssetMaintenanceHistoryRepository
{
    private readonly BuildingManagementContext _context;

    public AssetMaintenanceHistoryRepository(BuildingManagementContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AssetMaintenanceHistory>> GetAllHistoriesAsync()
    {
        return await _context.AssetMaintenanceHistories
            .Include(h => h.Asset)
                .ThenInclude(a => a.Category)
            .Include(h => h.Schedule)
                .ThenInclude(s => s.CreatedByUser)
            .Include(h => h.Vouchers)
            .OrderByDescending(h => h.ActionDate)
            .ToListAsync();
    }

    public async Task<AssetMaintenanceHistory?> GetHistoryByIdAsync(Guid historyId)
    {
        return await _context.AssetMaintenanceHistories
            .Include(h => h.Asset)
                .ThenInclude(a => a.Category)
            .Include(h => h.Schedule)
                .ThenInclude(s => s.CreatedByUser)
            .Include(h => h.Vouchers)
            .FirstOrDefaultAsync(h => h.HistoryId == historyId);
    }

    public async Task<IEnumerable<AssetMaintenanceHistory>> GetHistoriesByAssetIdAsync(Guid assetId)
    {
        return await _context.AssetMaintenanceHistories
            .Include(h => h.Asset)
                .ThenInclude(a => a.Category)
            .Include(h => h.Schedule)
                .ThenInclude(s => s.CreatedByUser)
            .Include(h => h.Vouchers)
            .Where(h => h.AssetId == assetId)
            .OrderByDescending(h => h.ActionDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<AssetMaintenanceHistory>> GetHistoriesByScheduleIdAsync(Guid scheduleId)
    {
        return await _context.AssetMaintenanceHistories
            .Include(h => h.Asset)
                .ThenInclude(a => a.Category)
            .Include(h => h.Schedule)
                .ThenInclude(s => s.CreatedByUser)
            .Include(h => h.Vouchers)
            .Where(h => h.ScheduleId == scheduleId)
            .OrderByDescending(h => h.ActionDate)
            .ToListAsync();
    }

    public async Task<AssetMaintenanceHistory> CreateHistoryAsync(AssetMaintenanceHistory history)
    {
        if (history.HistoryId == Guid.Empty)
        {
            history.HistoryId = Guid.NewGuid();
        }

        if (history.ActionDate == default(DateTime))
        {
            history.ActionDate = DateTime.UtcNow.AddHours(7);
        }

        _context.AssetMaintenanceHistories.Add(history);
        await _context.SaveChangesAsync();
        return history;
    }

    public async Task<AssetMaintenanceHistory?> UpdateHistoryAsync(AssetMaintenanceHistory history)
    {
        var existingHistory = await _context.AssetMaintenanceHistories.FindAsync(history.HistoryId);
        if (existingHistory == null)
        {
            return null;
        }

        existingHistory.Action = history.Action;
        existingHistory.ActionDate = history.ActionDate;
        existingHistory.CostAmount = history.CostAmount;
        existingHistory.Notes = history.Notes;
        existingHistory.NextDueDate = history.NextDueDate;

        await _context.SaveChangesAsync();
        return existingHistory;
    }
}

