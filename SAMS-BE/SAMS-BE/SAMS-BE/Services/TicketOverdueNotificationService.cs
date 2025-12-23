using Microsoft.EntityFrameworkCore;
using SAMS_BE.Helpers;
using SAMS_BE.Interfaces.IMail;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Models;

namespace SAMS_BE.Services
{
    public class TicketOverdueNotificationService : ITicketOverdueNotificationService
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<TicketOverdueNotificationService> _logger;
        private readonly BuildingManagementContext _context;

        // TODO: Có thể đổi sang config/appsettings thay vì hard-code nếu cần
        private static readonly Guid ReceptionistUserId = Guid.Parse("4AC86847-A770-41ED-AB92-2A6A800C1C7E");

        public TicketOverdueNotificationService(
            IUserRepository userRepository,
            IEmailSender emailSender,
            ILogger<TicketOverdueNotificationService> logger,
            BuildingManagementContext context)
        {
            _userRepository = userRepository;
            _emailSender = emailSender;
            _logger = logger;
            _context = context;
        }

        public async Task CheckAndNotifyOverdueTicketsAsync()
        {
            try
            {
                // Lấy thời gian hiện tại theo giờ Việt Nam rồi chuyển về UTC để so sánh với DB (DB lưu UTC)
                var nowVietnam = DateTimeHelper.VietnamNow;
                var nowUtc = DateTimeHelper.ToUtcFromVietnam(nowVietnam);
                var oneDayFromNowUtc = nowUtc.AddDays(1);

                // Lấy thông tin email lễ tân mặc định
                var receptionist = await _userRepository.GetByIdAsync(ReceptionistUserId);
                if (receptionist == null || string.IsNullOrWhiteSpace(receptionist.Email))
                {
                    _logger.LogWarning("Không tìm thấy tài khoản lễ tân hoặc email trống. Bỏ qua gửi email nhắc ticket.");
                    return;
                }
                var receptionistEmail = receptionist.Email;

                // Lấy các ticket còn khoảng 1 ngày đến hạn hoàn thành dự kiến
                // Điều kiện:
                // 1. Có ExpectedCompletionAt
                // 2. ExpectedCompletionAt >= now (chưa quá hạn)
                // 3. ExpectedCompletionAt <= oneDayFromNow (còn trong vòng 24 giờ tới)
                // 4. Status chưa đóng (chưa "Hoàn thành", "Đã đóng", "CLOSED", "RESOLVED")
                var ticketsDueSoon = await _context.Tickets
                    .Include(t => t.CreatedByUser)
                    .Where(t => t.ExpectedCompletionAt.HasValue
                        && t.ExpectedCompletionAt.Value >= nowUtc
                        && t.ExpectedCompletionAt.Value <= oneDayFromNowUtc
                        && t.Status != "Hoàn thành"
                        && t.Status != "Đã đóng"
                        && t.Status != "CLOSED"
                        && t.Status != "RESOLVED")
                    .ToListAsync();

                if (!ticketsDueSoon.Any())
                {
                    _logger.LogInformation("Không có ticket nào còn 1 ngày đến hạn vào lúc {Time}", nowVietnam);
                    return;
                }

                _logger.LogInformation("Tìm thấy {Count} ticket còn 1 ngày đến hạn. Bắt đầu gửi email thông báo.", ticketsDueSoon.Count);

                var sentCount = 0;
                var failedCount = 0;

                foreach (var ticket in ticketsDueSoon)
                {
                    try
                    {
                        var subject = $"⏰ Ticket #{ticket.TicketId.ToString().Substring(0, 8)} còn 1 ngày để xử lý";
                        var htmlContent = GenerateDueSoonTicketEmailHtml(ticket);

                        await _emailSender.SendEmailAsync(receptionistEmail, subject, htmlContent);
                        sentCount++;
                        _logger.LogInformation("Đã gửi email thông báo ticket còn 1 ngày đến hạn cho ticket {TicketId} đến lễ tân {Email}",
                            ticket.TicketId, receptionistEmail);
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        _logger.LogError(ex, "Lỗi khi gửi email thông báo ticket còn 1 ngày đến hạn cho ticket {TicketId}", ticket.TicketId);
                    }
                }

                _logger.LogInformation("Hoàn thành gửi email thông báo ticket còn 1 ngày đến hạn. Thành công: {SentCount}, Thất bại: {FailedCount}",
                    sentCount, failedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kiểm tra và gửi email thông báo ticket còn 1 ngày đến hạn");
                throw;
            }
        }

        private string GenerateDueSoonTicketEmailHtml(Ticket ticket)
        {
            // Convert từ UTC trong DB sang giờ Việt Nam để hiển thị đúng cho người dùng
            var expectedVietnam = DateTimeHelper.ToVietnamTime(ticket.ExpectedCompletionAt!.Value);
            var createdVietnam = DateTimeHelper.ToVietnamTime(ticket.CreatedAt);

            var expectedDate = expectedVietnam.ToString("dd/MM/yyyy HH:mm");
            var createdDate = createdVietnam.ToString("dd/MM/yyyy HH:mm");
            var today = DateTimeHelper.VietnamNow.ToString("dd/MM/yyyy");

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Ticket Còn 1 Ngày Đến Hạn</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #f4f4f4; padding: 20px;"">
        <tr>
            <td align=""center"">
                <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1);"">
                    <!-- Header -->
                    <tr>
                        <td style=""background-color: #ffc107; padding: 30px; text-align: center;"">
                            <h1 style=""color: #856404; margin: 0; font-size: 24px;"">⏰ Thông Báo Quan Trọng</h1>
                        </td>
                    </tr>
                    
                    <!-- Content -->
                    <tr>
                        <td style=""padding: 30px;"">
                            <p style=""color: #333333; font-size: 16px; line-height: 1.6; margin: 0 0 20px 0;"">
                                Xin chào,
                            </p>
                            <p style=""color: #333333; font-size: 16px; line-height: 1.6; margin: 0 0 20px 0;"">
                                <strong style=""color: #856404;"">Yêu cầu của bạn sắp đến hạn xử lý.</strong> Vui lòng xử lý yêu cầu này hoàn thành trong ngày hôm nay ({today}).
                            </p>
                            
                            <div style=""background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0;"">
                                <h2 style=""color: #856404; margin: 0 0 10px 0; font-size: 18px;"">Thông Tin Ticket</h2>
                                <table width=""100%"" cellpadding=""5"" cellspacing=""0"">
                                    <tr>
                                        <td style=""color: #856404; font-weight: bold; width: 150px;"">Mã Ticket:</td>
                                        <td style=""color: #856404;"">{ticket.TicketId}</td>
                                    </tr>
                                    <tr>
                                        <td style=""color: #856404; font-weight: bold;"">Tiêu đề:</td>
                                        <td style=""color: #856404;"">{ticket.Subject}</td>
                                    </tr>
                                    <tr>
                                        <td style=""color: #856404; font-weight: bold;"">Phân loại:</td>
                                        <td style=""color: #856404;"">{ticket.Category}</td>
                                    </tr>
                                    <tr>
                                        <td style=""color: #856404; font-weight: bold;"">Mức độ ưu tiên:</td>
                                        <td style=""color: #856404;"">{ticket.Priority ?? "N/A"}</td>
                                    </tr>
                                    <tr>
                                        <td style=""color: #856404; font-weight: bold;"">Trạng thái:</td>
                                        <td style=""color: #856404;"">{ticket.Status}</td>
                                    </tr>
                                    <tr>
                                        <td style=""color: #856404; font-weight: bold;"">Ngày tạo:</td>
                                        <td style=""color: #856404;"">{createdDate}</td>
                                    </tr>
                                    <tr>
                                        <td style=""color: #856404; font-weight: bold;"">Hạn hoàn thành:</td>
                                        <td style=""color: #856404; font-weight: bold;"">{expectedDate}</td>
                                    </tr>
                                </table>
                            </div>
                            
                            {(string.IsNullOrWhiteSpace(ticket.Description) ? "" : $@"
                            <div style=""margin: 20px 0;"">
                                <h3 style=""color: #333333; font-size: 16px; margin: 0 0 10px 0;"">Mô tả:</h3>
                                <p style=""color: #666666; font-size: 14px; line-height: 1.6; margin: 0; white-space: pre-wrap;"">{ticket.Description}</p>
                            </div>
                            ")}
                            
                            <div style=""background-color: #d1ecf1; border-left: 4px solid #0c5460; padding: 15px; margin: 20px 0;"">
                                <p style=""color: #0c5460; font-size: 16px; line-height: 1.6; margin: 0; font-weight: bold;"">
                                    ⚠️ Vui lòng xử lý yêu cầu này hoàn thành trong ngày hôm nay để đảm bảo đúng tiến độ.
                                </p>
                            </div>
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style=""background-color: #f8f9fa; padding: 20px; text-align: center; border-top: 1px solid #dee2e6;"">
                            <p style=""color: #6c757d; font-size: 12px; margin: 0;"">
                                Đây là email tự động từ hệ thống SAMS. Vui lòng không trả lời email này.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }
    }
}


