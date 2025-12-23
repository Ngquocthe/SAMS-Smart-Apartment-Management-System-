using SAMS_BE.DTOs;

namespace SAMS_BE.Interfaces.IService;

public interface IVehicleService
{
    /// <summary>
    /// Lấy danh sách xe của cư dân hiện tại
    /// </summary>
    Task<IEnumerable<MyVehicleDto>> GetMyVehiclesAsync(Guid userId);
    
    /// <summary>
    /// Lấy tất cả xe (admin)
    /// </summary>
    Task<IEnumerable<MyVehicleDto>> GetAllVehiclesAsync();
    
    /// <summary>
    /// Tạo ticket hủy đăng ký xe
    /// </summary>
    Task<ResidentTicketDto> CreateCancelVehicleTicketAsync(CreateCancelVehicleTicketDto dto, Guid userId);
    
    /// <summary>
    /// Update status xe và đóng ticket liên quan
    /// </summary>
    Task<MyVehicleDto> UpdateVehicleStatusAsync(Guid vehicleId, UpdateVehicleStatusDto dto);
    
    /// <summary>
    /// Tạo phiếu đăng ký xe cho cư dân (Manager)
    /// </summary>
    Task<ResidentTicketDto> CreateVehicleRegistrationTicketForManagerAsync(
        CreateVehicleRegistrationTicketDto dto, 
        Guid residentId, 
        Guid managerId);
    
    /// <summary>
    /// Lấy danh sách ticket hủy gửi xe
    /// </summary>
    Task<(IEnumerable<ResidentTicketDto> items, int total)> GetCancelVehicleTicketsAsync(
        string? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 20);
}
