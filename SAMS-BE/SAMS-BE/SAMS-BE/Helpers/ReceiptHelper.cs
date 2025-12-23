namespace SAMS_BE.Helpers
{
    /// <summary>
    /// Helper class for Receipt operations
    /// </summary>
    public static class ReceiptHelper
    {
        /// <summary>
        /// Generate Receipt Number in format: REC-{InvoiceNo}-YYYY/MM/DD
        /// Example: REC-INV202500001-2025/01/20
        /// </summary>
        /// <param name="invoiceNo">Invoice number</param>
        /// <param name="receivedDate">Date when payment was received</param>
        /// <returns>Generated receipt number</returns>
        public static string GenerateReceiptNumber(string invoiceNo, DateTime receivedDate)
        {
            var dateStr = receivedDate.ToString("yyyy/MM/dd");
            return $"REC-{invoiceNo}-{dateStr}";
        }

        /// <summary>
        /// Generate Receipt Number in format: REC-YYYYMMDD-NNNN (sequence per day)
        /// Example: REC-20250120-0001
        /// </summary>
        /// <param name="receivedDate">Date when payment was received</param>
        /// <param name="sequenceNumber">Sequence number for the day</param>
        /// <returns>Generated receipt number</returns>
        public static string GenerateReceiptNumberByDate(DateTime receivedDate, int sequenceNumber)
        {
            var dateStr = receivedDate.ToString("yyyyMMdd");
            return $"REC-{dateStr}-{sequenceNumber:D4}";
        }

        /// <summary>
        /// Generate Receipt Number in format: REC-YYYY-NNNN (sequence per year)
        /// Example: REC-2025-0001
        /// </summary>
        /// <param name="year">Year</param>
        /// <param name="sequenceNumber">Sequence number for the year</param>
        /// <returns>Generated receipt number</returns>
        public static string GenerateReceiptNumberByYear(int year, int sequenceNumber)
        {
            return $"REC-{year}-{sequenceNumber:D4}";
        }
    }
}
