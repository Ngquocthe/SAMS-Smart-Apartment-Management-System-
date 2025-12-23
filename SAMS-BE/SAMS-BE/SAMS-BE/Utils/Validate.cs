using System.Text.RegularExpressions;

namespace SAMS_BE.Utils
{
    public static class Validate
    {
        private static readonly Regex VietNamPhoneRegex = new(
            pattern: @"^(?:0|\+84)(?:3|5|7|8|9)[0-9]{8}$",
            options: RegexOptions.Compiled
        );

        public static bool IsValidPhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            var cleaned = Regex.Replace(phone, @"[\s\-\.\(\)]", "");

            return VietNamPhoneRegex.IsMatch(cleaned);
        }

        public static string? NormalizePhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return phone;

            var cleaned = Regex.Replace(phone, @"[\s\-\.\(\)]", "");

            if (cleaned.StartsWith("+84"))
            {
                cleaned = "0" + cleaned.Substring(3);
            }

            return cleaned;
        }
    }
}
