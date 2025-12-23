using System;

namespace SAMS_BE.Helpers;

/// <summary>
/// Công cụ xử lý thời gian chuẩn giờ Việt Nam (UTC+7).
/// </summary>
public static class DateTimeHelper
{
    private const string WindowsTzId = "SE Asia Standard Time";
    private const string LinuxTzId = "Asia/Bangkok";

    private static readonly TimeZoneInfo VietnamTimeZone;

    static DateTimeHelper()
    {
        try
        {
            VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById(WindowsTzId);
        }
        catch (TimeZoneNotFoundException)
        {
            VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById(LinuxTzId);
        }
    }

    /// <summary>
    /// Trả về thời gian hiện tại theo giờ Việt Nam (UTC+7).
    /// </summary>
    public static DateTime VietnamNow
    {
        get
        {
            // Trả về DateTime Kind.Unspecified để khi serialize không tự thêm "Z" (UTC)
            var dt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);
            return DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);
        }
    }

    /// <summary>
    /// Chuyển đổi DateTime (UTC/Local/Unspecified) sang giờ Việt Nam.
    /// </summary>
    public static DateTime ToVietnamTime(DateTime dateTime)
    {
        var utc = dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
        };

        return TimeZoneInfo.ConvertTimeFromUtc(utc, VietnamTimeZone);
    }

    /// <summary>
    /// Chuyển đổi thời gian giờ Việt Nam sang UTC.
    /// </summary>
    public static DateTime ToUtcFromVietnam(DateTime vietnamDateTime)
    {
        return vietnamDateTime.Kind == DateTimeKind.Utc
            ? vietnamDateTime
            : TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(vietnamDateTime, DateTimeKind.Unspecified),
                VietnamTimeZone);
    }
}

