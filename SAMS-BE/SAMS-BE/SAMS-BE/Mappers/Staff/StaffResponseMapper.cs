using System.Security.AccessControl;
using SAMS_BE.DTOs.Response.Staff;
using SAMS_BE.Models;

namespace SAMS_BE.Mappers.Staff
{
    public static partial class StaffResponseMapper
    {
        public static StaffDetailDto ToStaffDetailDto(
            this StaffProfile staff)
        {
            var user = staff.User;

            return new StaffDetailDto
            {
                StaffCode = staff.StaffCode,
                UserId = user?.UserId,
                Username = user?.Username,
                Email = user?.Email,
                FirstName = user?.FirstName,
                LastName = user?.LastName,
                FullName = user == null
                    ? null
                    : $"{user.FirstName} {user.LastName}".Trim(),

                Phone = user?.Phone,
                Dob = user?.Dob?.ToDateTime(TimeOnly.MinValue),
                Address = user?.Address,
                CurrentAddress = staff.CurrentAddress,
                HireDate = staff.HireDate?.ToDateTime(TimeOnly.MinValue),
                TerminationDate = staff.TerminationDate?.ToDateTime(TimeOnly.MinValue),
                RoleId = staff.RoleId.ToString(),
                BaseSalary = (decimal) staff.BaseSalary,
                Notes = staff.Notes,
                EmergencyContactName = staff.EmergencyContactName,
                EmergencyContactPhone = staff.EmergencyContactPhone,
                EmergencyContactRelation = staff.EmergencyContactRelation,
                BankAccountNo = staff.BankAccountNo,
                BankName = staff.BankName,
                TaxCode = staff.TaxCode,
                SocialInsuranceNo = staff.SocialInsuranceNo,
                AvatarUrl = user?.AvatarUrl,
                CardPhotoUrl = staff.CardPhotoUrl
            };
        }
    }
}
