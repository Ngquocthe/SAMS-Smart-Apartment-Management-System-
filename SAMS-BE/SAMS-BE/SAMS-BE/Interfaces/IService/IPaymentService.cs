using SAMS_BE.DTOs;

namespace SAMS_BE.Interfaces.IService
{
    public interface IPaymentService
    {
        /// <summary>
        /// Tạo mã QR thanh toán SePay
        /// </summary>
        /// <param name="request">Thông tin thanh toán</param>
        /// <returns>Thông tin mã QR thanh toán</returns>
        Task<PaymentResponseDto> CreatePaymentLinkAsync(CreatePaymentRequestDto request);

        /// <summary>
        /// Kiểm tra trạng thái thanh toán SePay
        /// </summary>
        /// <param name="orderCode">Mã đơn hàng</param>
        /// <returns>Trạng thái thanh toán</returns>
        Task<PaymentStatusDto> GetPaymentStatusAsync(int orderCode);

        /// <summary>
        /// Xử lý webhook từ SePay (không dùng, dùng polling thay thế)
        /// </summary>
        /// <param name="webhookData">Dữ liệu webhook</param>
        /// <returns>Kết quả xử lý</returns>
        Task<bool> ProcessWebhookAsync(PaymentWebhookDto webhookData);

        /// <summary>
        /// Hủy thanh toán
        /// </summary>
        /// <param name="orderCode">Mã đơn hàng</param>
        /// <param name="cancellationReason">Lý do hủy</param>
        /// <returns>Kết quả hủy thanh toán</returns>
        Task<CancelPaymentResponseDto> CancelPaymentAsync(int orderCode, string? cancellationReason = null);

        /// <summary>
        /// Xác thực webhook signature
        /// </summary>
        /// <param name="webhookUrl">URL webhook</param>
        /// <param name="requestBody">Nội dung request</param>
        /// <param name="signature">Chữ ký</param>
        /// <returns>True nếu hợp lệ</returns>
        bool VerifyWebhookSignature(string webhookUrl, string requestBody, string signature);
    }
}