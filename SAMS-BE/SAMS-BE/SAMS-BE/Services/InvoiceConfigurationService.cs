using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Models;

namespace SAMS_BE.Services;

public class InvoiceConfigurationService : IInvoiceConfigurationService
{
    private readonly IInvoiceConfigurationRepository _repository;
    private readonly ILogger<InvoiceConfigurationService> _logger;

    public InvoiceConfigurationService(
        IInvoiceConfigurationRepository repository,
   ILogger<InvoiceConfigurationService> logger)
{
        _repository = repository;
    _logger = logger;
    }

    public async Task<InvoiceConfigurationResponseDto> GetCurrentConfigAsync()
    {
        var config = await _repository.GetCurrentConfigAsync();

        // N?u ch?a có config, tr? v? config m?c ??nh
        if (config == null)
   {
  return new InvoiceConfigurationResponseDto
     {
ConfigId = Guid.Empty,
        GenerationDayOfMonth = 1,
       DueDaysAfterIssue = 40,
       IsEnabled = true,
      Notes = "C?u hình m?c ??nh - ch?a ???c l?u vào database",
    CreatedAt = DateTime.UtcNow
       };
     }

    return MapToDto(config);
    }

    public async Task<InvoiceConfigurationResponseDto> CreateOrUpdateAsync(
     CreateOrUpdateInvoiceConfigDto dto, 
        string updatedBy)
    {
        var exists = await _repository.ExistsAsync();

  InvoiceConfiguration config;

    if (exists)
  {
     // Update existing config
    var currentConfig = await _repository.GetCurrentConfigAsync();
            if (currentConfig == null)
        {
           throw new InvalidOperationException("Configuration exists but cannot be loaded");
            }

            currentConfig.GenerationDayOfMonth = dto.GenerationDayOfMonth;
   currentConfig.DueDaysAfterIssue = dto.DueDaysAfterIssue;
         currentConfig.IsEnabled = dto.IsEnabled;
            currentConfig.Notes = dto.Notes;
            currentConfig.UpdatedBy = updatedBy;

config = await _repository.UpdateAsync(currentConfig);
     _logger.LogInformation("Updated invoice configuration. GenerationDay: {Day}, DueDays: {DueDays}, UpdatedBy: {User}",
          dto.GenerationDayOfMonth, dto.DueDaysAfterIssue, updatedBy);
      }
        else
        {
    // Create new config
      config = new InvoiceConfiguration
     {
 GenerationDayOfMonth = dto.GenerationDayOfMonth,
       DueDaysAfterIssue = dto.DueDaysAfterIssue,
              IsEnabled = dto.IsEnabled,
       Notes = dto.Notes,
                CreatedBy = updatedBy
 };

         config = await _repository.CreateAsync(config);
  _logger.LogInformation("Created new invoice configuration. GenerationDay: {Day}, DueDays: {DueDays}, CreatedBy: {User}",
      dto.GenerationDayOfMonth, dto.DueDaysAfterIssue, updatedBy);
        }

        return MapToDto(config);
    }

    private static InvoiceConfigurationResponseDto MapToDto(InvoiceConfiguration config)
  {
        return new InvoiceConfigurationResponseDto
   {
            ConfigId = config.ConfigId,
      GenerationDayOfMonth = config.GenerationDayOfMonth,
   DueDaysAfterIssue = config.DueDaysAfterIssue,
            IsEnabled = config.IsEnabled,
    Notes = config.Notes,
            CreatedAt = config.CreatedAt,
            CreatedBy = config.CreatedBy,
   UpdatedAt = config.UpdatedAt,
            UpdatedBy = config.UpdatedBy
        };
    }
}
