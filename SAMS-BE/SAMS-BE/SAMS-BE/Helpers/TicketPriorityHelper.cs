using System;
using System.Collections.Generic;

namespace SAMS_BE.Helpers;

public static class TicketPriorityHelper
{
    public const string DefaultPriority = "Bình thường";

    private static readonly HashSet<string> LowPriorities = new(StringComparer.OrdinalIgnoreCase)
    {
        "Thấp",

    };

    private static readonly HashSet<string> UrgentPriorities = new(StringComparer.OrdinalIgnoreCase)
    {
        "Khẩn cấp",
    };

    public static int GetResolutionDays(string? priority)
    {
        var normalized = string.IsNullOrWhiteSpace(priority)
            ? DefaultPriority
            : priority.Trim();

        if (LowPriorities.Contains(normalized))
        {
            return 5;
        }

        if (UrgentPriorities.Contains(normalized))
        {
            return 1;
        }

        return 3;
    }

    public static DateTime? CalculateExpectedCompletionDate(string? priority, DateTime createdAt)
    {
        // Chỉ tính ngày hoàn thành khi có mức độ ưu tiên
        if (string.IsNullOrWhiteSpace(priority))
        {
            return null;
        }

        // Ưu tiên giờ Việt Nam (UTC+7) và trả về Kind.Unspecified để tránh thêm "Z"
        var baseTime = createdAt == default ? DateTime.UtcNow : createdAt;
        var vietnamTime = baseTime.Kind == DateTimeKind.Utc
            ? baseTime.AddHours(7)
            : baseTime;

        var days = GetResolutionDays(priority);
        var expected = vietnamTime.AddDays(days);
        return DateTime.SpecifyKind(expected, DateTimeKind.Unspecified);
    }
}



