using SAMS_BE.DTOs;

namespace SAMS_BE.Interfaces.IService
{
    public interface IInvoicePaymentService
    {
        /// <summary>
        /// Tạo QR code thanh toán cho invoice
        /// </summary>
        /// <param name="dto">Thông tin invoice payment</param>
        /// <returns>QR code và thông tin invoice</returns>
        Task<InvoicePaymentResponseDto> CreateInvoicePaymentAsync(CreateInvoicePaymentDto dto);

        /// <summary>
        /// Verify payment status, tự động tạo Receipt và gửi email nếu đã thanh toán
        /// </summary>
        /// <param name="dto">Invoice ID và Order Code</param>
        /// <returns>Trạng thái thanh toán và thông tin receipt</returns>
        Task<VerifyInvoicePaymentResponseDto> VerifyInvoicePaymentAsync(VerifyInvoicePaymentDto dto);
    }
}
