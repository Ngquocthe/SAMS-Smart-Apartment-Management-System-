namespace SAMS_BE.Helpers
{
    public class InvoiceStatusHelper
    {
        public static readonly string[] ValidStatuses = {"DRAFT", "ISSUED", "PAID", "OVERDUE", "CANCELLED" };

        private static readonly HashSet<string> ValidSet =
            new(ValidStatuses, StringComparer.Ordinal);

        public static string Normalize(string? status)
        => (status ?? string.Empty).Trim().ToUpperInvariant();

        public static bool IsValid(string? status)
            => ValidSet.Contains(Normalize(status));

        public static string EnsureValid(string? status, string paramName = "status")
        {
            var s = Normalize(status);
            if (!ValidSet.Contains(s))
                throw new ArgumentException(
                    $"Invalid status '{status}'. Allowed: {string.Join(", ", ValidStatuses)}",
                    paramName);
            return s;
        }
        public static bool ShouldBeOverdue(DateOnly today, DateOnly dueDate, string currentStatus)
            => today > dueDate && Normalize(currentStatus) is not "PAID" and not "CANCELLED";

        public static bool IsCutoff(DateOnly today, DateOnly dueDate, string currentStatus, int graceDays = 10)
            => ShouldBeOverdue(today, dueDate, currentStatus) && today > dueDate.AddDays(graceDays);
    }
}
