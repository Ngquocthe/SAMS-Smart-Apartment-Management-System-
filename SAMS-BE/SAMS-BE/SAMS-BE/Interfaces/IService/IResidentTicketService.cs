using SAMS_BE.DTOs;
using Microsoft.AspNetCore.Http;

namespace SAMS_BE.Interfaces.IService;

public interface IResidentTicketService
{
    /// <summary>
    /// Tạo phiếu bảo trì (Maintenance ticket)
    /// Category: Maintenance, Scope: APARTMENT
    /// </summary>
    Task<ResidentTicketDto> CreateMaintenanceTicketAsync(CreateMaintenanceTicketDto dto, Guid userId);

    /// <summary>
    /// Tạo phiếu khiếu nại (Complaint ticket)
    /// Category: Complaint, Scope: BUILDING
    /// </summary>
    Task<ResidentTicketDto> CreateComplaintTicketAsync(CreateComplaintTicketDto dto, Guid userId);

    /// <summary>
    /// Tạo phiếu đăng ký xe (Vehicle Registration ticket)
    /// Category: VehicleRegistration, Scope: APARTMENT
    /// </summary>
    Task<ResidentTicketDto> CreateVehicleRegistrationTicketAsync(CreateVehicleRegistrationTicketDto dto, Guid userId);

    /// <summary>
    /// Lấy danh sách tickets của resident
    /// </summary>
    Task<(IEnumerable<ResidentTicketDto> items, int total)> GetMyTicketsAsync(ResidentTicketQueryDto query, Guid userId);

    Task<IEnumerable<ResidentTicketInvoiceDto>> GetInvoicesForTicketAsync(Guid ticketId, Guid userId);

    /// <summary>
    /// Lấy thống kê tickets của resident
    /// </summary>
    Task<ResidentTicketStatisticsDto> GetMyTicketStatisticsAsync(Guid userId);

    /// <summary>
    /// Lấy chi tiết ticket theo ID
    /// </summary>
    Task<ResidentTicketDto?> GetTicketByIdAsync(Guid ticketId, Guid userId);

    /// <summary>
    /// Thêm comment vào ticket
    /// </summary>
    Task<TicketCommentDto> AddCommentAsync(CreateResidentTicketCommentDto dto, Guid userId);

    /// <summary>
    /// Lấy danh sách comments của ticket
    /// </summary>
    Task<IEnumerable<TicketCommentDto>> GetCommentsAsync(Guid ticketId, Guid userId);

    /// <summary>
    /// Upload file cho ticket
    /// </summary>
    Task<FileDto> UploadFileAsync(IFormFile file, Guid userId);

    /// <summary>
    /// Thêm attachment vào ticket
    /// </summary>
    Task<TicketAttachmentDto> AddAttachmentAsync(Guid ticketId, Guid fileId, string? note, Guid userId);

    /// <summary>
    /// Lấy danh sách attachments của ticket
    /// </summary>
    Task<IEnumerable<TicketAttachmentDto>> GetAttachmentsAsync(Guid ticketId, Guid userId);

    /// <summary>
    /// Xóa attachment (chỉ người tạo mới được xóa)
    /// </summary>
    Task<bool> DeleteAttachmentAsync(Guid attachmentId, Guid userId);
}
