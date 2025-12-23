using System.Text.RegularExpressions;
using SAMS_BE.DTOs;

namespace SAMS_BE.Helpers
{
    public static class ServiceTypeValidation
    {
        public static string NormalizeCode(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Code is required.");
            return code.Trim().ToUpperInvariant();
        }

        public static void ValidateCode(string normalizedCode)
        {
            if (normalizedCode.Length is < 2 or > 50)
                throw new ArgumentException("Code length must be between 2 and 50 characters.");

            var regex = new Regex("^[A-Z0-9_]+$");
            if (!regex.IsMatch(normalizedCode))
                throw new ArgumentException("Code must contain only A-Z, 0-9 or underscore.");
        }

        public static IReadOnlyList<string> ValidateBusinessRules(CreateServiceTypeDto dto, string normalizedCode)
        {
            var warnings = new List<string>();

            if (dto.IsMandatory && !dto.IsRecurring)
                throw new ArgumentException($"Service type '{normalizedCode}': Mandatory service must also be Recurring.");

            if (dto.IsRecurring && string.IsNullOrWhiteSpace(dto.Unit))
                warnings.Add($"Service type '{normalizedCode}': Recurring but Unit is empty.");

            if (dto.CategoryId == Guid.Empty)
                throw new ArgumentException("CategoryId is required.");

            return warnings;
        }
    }
}
