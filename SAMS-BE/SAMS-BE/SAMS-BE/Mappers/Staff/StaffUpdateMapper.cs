using SAMS_BE.DTOs.Request.Staff;
using SAMS_BE.Models;

namespace SAMS_BE.Mappers.Staff
{
    public static class StaffUpdateMapper
    {
        public static void MapToUser(
            this StaffUpdateDto dto,
            User user,
            string? avatarUrl)
        {
            user.Phone = dto.Phone;
            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.Address = dto.Address;
            user.Dob = dto.Dob.HasValue
                ? DateOnly.FromDateTime(dto.Dob.Value)
                : null;

            if (!string.IsNullOrWhiteSpace(avatarUrl))
            {
                user.AvatarUrl = avatarUrl;
            }

            user.UpdatedAt = DateTime.UtcNow;
        }

        public static void MapToStaffProfile(
            this StaffUpdateDto dto,
            StaffProfile staff,
            string? cardPhotoUrl)
        {
            staff.CurrentAddress = dto.CurrentAddress;
            staff.Notes = dto.Notes;
            staff.IsActive = dto.IsActive;

            staff.HireDate = DateOnly.FromDateTime(dto.HireDate);
            staff.TerminationDate = dto.TerminationDate.HasValue
                ? DateOnly.FromDateTime(dto.TerminationDate.Value)
                : null;

            staff.EmergencyContactName = dto.EmergencyContactName;
            staff.EmergencyContactPhone = dto.EmergencyContactPhone;
            staff.EmergencyContactRelation = dto.EmergencyContactRelation;

            staff.BankAccountNo = dto.BankAccountNo;
            staff.BankName = dto.BankName;
            staff.BaseSalary = dto.BaseSalary;

            staff.TaxCode = dto.TaxCode;
            staff.SocialInsuranceNo = dto.SocialInsuranceNo;

            if (!string.IsNullOrWhiteSpace(cardPhotoUrl))
            {
                staff.CardPhotoUrl = cardPhotoUrl;
            }

            staff.RoleId = Guid.Parse(dto.RoleId);
        }
    }
}
