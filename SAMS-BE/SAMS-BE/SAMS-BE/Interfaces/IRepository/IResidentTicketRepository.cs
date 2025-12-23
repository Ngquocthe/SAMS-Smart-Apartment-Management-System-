using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Interfaces.IRepository;

public interface IResidentTicketRepository
{
    /// <summary>
    /// Query tickets
    /// </summary>
    IQueryable<Ticket> Query();

    /// <summary>
    /// Lấy ticket theo ID
    /// </summary>
    Task<Ticket?> GetByIdAsync(Guid id);

    /// <summary>
    /// Thêm ticket mới
    /// </summary>
    Task<Ticket> AddAsync(Ticket entity);

    /// <summary>
    /// Cập nhật ticket
    /// </summary>
    Task UpdateAsync(Ticket entity);

    /// <summary>
    /// Query comments
    /// </summary>
    IQueryable<TicketComment> QueryComments();

    /// <summary>
    /// Thêm comment
    /// </summary>
    Task<TicketComment> AddCommentAsync(TicketComment comment);

    /// <summary>
    /// Query attachments
    /// </summary>
    IQueryable<TicketAttachment> QueryAttachments();

    /// <summary>
    /// Lấy attachment theo ID
    /// </summary>
    Task<TicketAttachment?> GetAttachmentByIdAsync(Guid attachmentId);

    /// <summary>
    /// Thêm attachment
    /// </summary>
    Task<TicketAttachment> AddAttachmentAsync(TicketAttachment attachment);

    /// <summary>
    /// Xóa attachment
    /// </summary>
    Task DeleteAttachmentAsync(Guid attachmentId);

    /// <summary>
    /// Query files
    /// </summary>
    IQueryable<Models.File> QueryFiles();

    /// <summary>
    /// Lấy file theo ID
    /// </summary>
    Task<Models.File?> GetFileByIdAsync(Guid fileId);

    /// <summary>
    /// Thêm file
    /// </summary>
    Task<Models.File> AddFileAsync(Models.File file);
    /// <summary>
    /// Lấy hóa đơn liên quan đến ticket
    /// </summary>
    Task<List<ResidentTicketInvoiceDto>> GetInvoicesByTicketIdAsync(Guid ticketId);

    /// <summary>
    /// Lấy vehicle theo biển số xe
    /// </summary>
    Task<Vehicle?> GetVehicleByLicensePlateAsync(string licensePlate);

    /// <summary>
    /// Thêm vehicle mới
    /// </summary>
    Task<Vehicle> AddVehicleAsync(Vehicle vehicle);

    /// <summary>
    /// Lấy resident profile theo user ID
    /// </summary>
    Task<ResidentProfile?> GetResidentProfileByUserIdAsync(Guid userId);
}
