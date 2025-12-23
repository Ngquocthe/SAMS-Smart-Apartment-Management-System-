using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Interfaces.IRepository.GlobalAdmin;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Tenant;

namespace SAMS_BE.BackgroundServices
{
    public class AnnouncementExpirationService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<AnnouncementExpirationService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

        public AnnouncementExpirationService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<AnnouncementExpirationService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Announcement Expiration Service started");

            using var timer = new PeriodicTimer(_checkInterval);

            try
            {
                // Run immediately on startup
                await ExpireAnnouncementsAsync(stoppingToken);

                // Then run every 30 seconds
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await ExpireAnnouncementsAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Announcement Expiration Service is stopping");
            }
        }

        private async Task ExpireAnnouncementsAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var buildingRepository = scope.ServiceProvider.GetRequiredService<IBuildingRepository>();
                var tenantContextAccessor = scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
                var announcementService = scope.ServiceProvider.GetRequiredService<IAnnouncementService>();

                // Get all buildings
                var buildings = await buildingRepository.GetAllAsync(stoppingToken);
                
                foreach (var building in buildings)
                {
                    try
                    {
                        // Set schema for current building
                        tenantContextAccessor.SetSchema(building.SchemaName);
                        
                        var expiredCount = await announcementService.ExpireAnnouncementsAsync();

                        if (expiredCount > 0)
                        {
                            _logger.LogInformation("Expired {Count} announcement(s) for building: {BuildingName}", 
                                expiredCount, building.BuildingName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error expiring announcements for building {BuildingName}", building.BuildingName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while expiring announcements");
            }
        }
    }
}
