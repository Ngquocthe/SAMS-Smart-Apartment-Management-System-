using Microsoft.EntityFrameworkCore;
using SAMS_BE.DTOs.JournalEntry;
using SAMS_BE.Helpers;
using SAMS_BE.Models;

namespace SAMS_BE.Services
{
    /// <summary>
    /// Journal Entry Service - Reporting Methods
    /// </summary>
    public partial class JournalEntryService
    {
        /// <summary>
        /// Get General Journal (S? nh?t ký chung)
        /// </summary>
        public async Task<(List<GeneralJournalDto> Items, int Total)> GetGeneralJournalAsync(JournalEntryQueryDto query)
        {
            try
            {
                var journalQuery = _context.JournalEntries
                 .Include(je => je.JournalEntryLines)
                .ThenInclude(jel => jel.Apartment)
              .Include(je => je.CreatedByNavigation)
               .ThenInclude(sp => sp.User)
                  .AsNoTracking()
                .AsQueryable();

                // Apply filters
                if (query.FromDate.HasValue)
                {
                    var fromDateOnly = DateOnly.FromDateTime(query.FromDate.Value);
                    journalQuery = journalQuery.Where(je => je.EntryDate >= fromDateOnly);
                }

                if (query.ToDate.HasValue)
                {
                    var toDateOnly = DateOnly.FromDateTime(query.ToDate.Value);
                    journalQuery = journalQuery.Where(je => je.EntryDate <= toDateOnly);
                }

                if (!string.IsNullOrWhiteSpace(query.EntryType))
                {
                    journalQuery = journalQuery.Where(je => je.EntryType == query.EntryType);
                }

                if (!string.IsNullOrWhiteSpace(query.Status))
                {
                    journalQuery = journalQuery.Where(je => je.Status == query.Status);
                }

                if (!string.IsNullOrWhiteSpace(query.FiscalPeriod))
                {
                    journalQuery = journalQuery.Where(je => je.FiscalPeriod == query.FiscalPeriod);
                }

                if (!string.IsNullOrWhiteSpace(query.Search))
                {
                    var searchTerm = query.Search.ToLower();
                    journalQuery = journalQuery.Where(je =>
                        je.EntryNumber.ToLower().Contains(searchTerm) ||
                   (je.Description != null && je.Description.ToLower().Contains(searchTerm)));
                }

                var total = await journalQuery.CountAsync();

                // Apply sorting
                journalQuery = query.SortBy?.ToLower() switch
                {
                    "entrynumber" => query.SortDir == "asc"
                          ? journalQuery.OrderBy(je => je.EntryNumber)
                       : journalQuery.OrderByDescending(je => je.EntryNumber),
                    "entrytype" => query.SortDir == "asc"
                          ? journalQuery.OrderBy(je => je.EntryType)
                           : journalQuery.OrderByDescending(je => je.EntryType),
                    _ => query.SortDir == "asc"
                         ? journalQuery.OrderBy(je => je.EntryDate)
                      : journalQuery.OrderByDescending(je => je.EntryDate)
                };

                // Apply pagination
                var entries = await journalQuery
             .Skip((query.Page - 1) * query.PageSize)
                  .Take(query.PageSize)
              .ToListAsync();

                // Map to DTO
                var items = entries.Select(MapToGeneralJournalDto).ToList();

                return (items, total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetGeneralJournalAsync");
                throw;
            }
        }

        /// <summary>
        /// Get Journal Entry by ID
        /// </summary>
        public async Task<GeneralJournalDto?> GetByIdAsync(Guid entryId)
        {
            try
            {
                var entry = await _context.JournalEntries
                     .Include(je => je.JournalEntryLines)
                   .ThenInclude(jel => jel.Apartment)
                .Include(je => je.CreatedByNavigation)
                          .ThenInclude(sp => sp.User)
                        .AsNoTracking()
                         .FirstOrDefaultAsync(je => je.EntryId == entryId);

                return entry == null ? null : MapToGeneralJournalDto(entry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting journal entry {entryId}");
                throw;
            }
        }

        /// <summary>
        /// Helper method to map JournalEntry to DTO
        /// </summary>
        private GeneralJournalDto MapToGeneralJournalDto(JournalEntry entry)
        {
            return new GeneralJournalDto
            {
                EntryId = entry.EntryId,
                EntryNumber = entry.EntryNumber,
                EntryDate = entry.EntryDate,
                EntryType = entry.EntryType,
                ReferenceType = entry.ReferenceType,
                ReferenceId = entry.ReferenceId,
                Description = entry.Description,
                Status = entry.Status,
                FiscalPeriod = entry.FiscalPeriod,
                CreatedBy = entry.CreatedBy,
                CreatedByName = entry.CreatedByNavigation?.User?.FirstName + " " + entry.CreatedByNavigation?.User?.LastName,
                PostedDate = entry.PostedDate,
                Lines = entry.JournalEntryLines.Select(line => new JournalEntryLineDto
                {
                    LineId = line.LineId,
                    LineNumber = line.LineNumber,
                    AccountCode = line.AccountCode,
                    Description = line.Description,
                    DebitAmount = line.DebitAmount,
                    CreditAmount = line.CreditAmount,
                    ApartmentId = line.ApartmentId,
                    ApartmentNumber = line.Apartment?.Number
                }).OrderBy(l => l.LineNumber).ToList()
            };
        }

        /// <summary>
        /// Get account name from code
        /// </summary>
        private string GetAccountName(string accountCode)
        {
            return accountCode switch
            {
                "1111" => "Tien mat",
                "1121" => "Tien gui ngan hang",
                "5112" => "Doanh thu dich vu",
                "5200" => "Doanh thu tien ich",
                "6211" => "Chi phi chung",
                "6271" => "Chi phi sua chua",
                "6421" => "Chi phi sua chua",
                "6422" => "Chi phi nhan cong",
                _ => $"Tai khoan {accountCode}"
            };
        }
    }
}
