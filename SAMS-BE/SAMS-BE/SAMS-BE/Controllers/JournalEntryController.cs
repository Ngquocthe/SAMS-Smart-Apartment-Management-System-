using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMS_BE.DTOs.JournalEntry;
using SAMS_BE.Interfaces;

namespace SAMS_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class JournalEntryController : ControllerBase
    {
        private readonly IJournalEntryService _journalEntryService;
        private readonly ILogger<JournalEntryController> _logger;

        public JournalEntryController(
        IJournalEntryService journalEntryService,
            ILogger<JournalEntryController> logger)
        {
            _journalEntryService = journalEntryService;
            _logger = logger;
        }

        /// <summary>
        /// Get General Journal (Sổ nhật ký chung)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetGeneralJournal([FromQuery] JournalEntryQueryDto query)
        {
            try
            {
                var result = await _journalEntryService.GetGeneralJournalAsync(query);

                return Ok(new
                {
                    items = result.Items,
                    total = result.Total
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting general journal");
                return StatusCode(500, new { message = "Error retrieving general journal", error = ex.Message });
            }
        }

        /// <summary>
        /// Get Journal Entry by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var entry = await _journalEntryService.GetByIdAsync(id);
                if (entry == null)
                    return NotFound(new { message = $"Journal entry {id} not found" });

                return Ok(entry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting journal entry {id}");
                return StatusCode(500, new { message = "Error retrieving journal entry", error = ex.Message });
            }
        }

        /// <summary>
        /// Get Income Statement (Báo cáo thu chi)
        /// </summary>
        [HttpGet("income-statement")]
        public async Task<IActionResult> GetIncomeStatement(
        [FromQuery] DateTime from,
       [FromQuery] DateTime to)
        {
            try
            {
                if (from == default || to == default)
                    return BadRequest(new { message = "From and To dates are required" });

                if (from > to)
                    return BadRequest(new { message = "From date must be before To date" });

                var incomeStatement = await _journalEntryService.GetIncomeStatementAsync(from, to);
                return Ok(incomeStatement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting income statement");
                return StatusCode(500, new { message = "Error retrieving income statement", error = ex.Message });
            }
        }

        /// <summary>
        /// Get Financial Dashboard
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetFinancialDashboard([FromQuery] string period = "month")
        {
            try
            {
                var dashboard = await _journalEntryService.GetFinancialDashboardAsync(period);
                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting financial dashboard");
                return StatusCode(500, new { message = "Error retrieving financial dashboard", error = ex.Message });
            }
        }
    }
}
