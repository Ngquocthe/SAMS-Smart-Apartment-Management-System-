using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Hangfire;
using Hangfire.Storage;
using SAMS_BE.Interfaces.IService;

namespace SAMS_BE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;
    private readonly IRecurringJobManager _recurringJobManager;

    public HealthController(
        ILogger<HealthController> logger,
        IRecurringJobManager recurringJobManager)
    {
        _logger = logger;
        _recurringJobManager = recurringJobManager;
    }

    /// <summary>
    /// Public health check endpoint - No authentication required
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow.AddHours(7),
            message = "API is running"
        });
    }

    /// <summary>
    /// Check Hangfire server status and jobs - Public for debugging
    /// </summary>
    [HttpGet("hangfire-status")]
    [AllowAnonymous]
    public IActionResult HangfireStatus()
    {
        try
        {
            var monitoringApi = JobStorage.Current.GetMonitoringApi();
            var servers = monitoringApi.Servers();
            var recurringJobs = JobStorage.Current.GetConnection().GetRecurringJobs();
            
            return Ok(new
            {
                hangfireConfigured = true,
                serverCount = servers.Count,
                servers = servers.Select(s => new
                {
                    name = s.Name,
                    queues = s.Queues,
                    startedAt = s.StartedAt,
                    workersCount = s.WorkersCount
                }),
                recurringJobCount = recurringJobs.Count,
                recurringJobs = recurringJobs.Select(j => new
                {
                    id = j.Id,
                    cron = j.Cron,
                    lastExecution = j.LastExecution,
                    nextExecution = j.NextExecution,
                    lastJobState = j.LastJobState
                }),
                message = servers.Any() ? "Hangfire Server is running" : "WARNING: No Hangfire servers found!",
                timestamp = DateTime.UtcNow.AddHours(7)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Hangfire status");
            return StatusCode(500, new
            {
                hangfireConfigured = false,
                error = ex.Message,
                stackTrace = ex.StackTrace,
                message = "Hangfire is NOT working properly"
            });
        }
    }
}
