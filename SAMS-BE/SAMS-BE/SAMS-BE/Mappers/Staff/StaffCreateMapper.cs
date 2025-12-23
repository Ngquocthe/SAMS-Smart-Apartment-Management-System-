using System.ComponentModel.DataAnnotations;
using CloudinaryDotNet.Actions;
using SAMS_BE.DTOs.Request.Staff;
using SAMS_BE.Models;

namespace SAMS_BE.Mappers.Staff
{
    public static class StaffCreateMapper
    {
        public static User ToUserEntity(this StaffCreateRequest r, Guid userId, string? avatarUrl)
        {
            return new User
            {
                UserId = userId,
                Username = r.Username!.Trim(),
                Email = r.Email!.Trim(),
                Phone = r.Phone!.Trim(),
                FirstName = r.FirstName!.Trim(),
                LastName = r.LastName!.Trim(),
                Dob = r.Dob.HasValue
                                ? DateOnly.FromDateTime(r.Dob.Value)
                                : (DateOnly?)null,
                Address = string.IsNullOrWhiteSpace(r.Address) ? null : r.Address.Trim(),
                AvatarUrl = avatarUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };
        }

        public static StaffProfile ToStaffProfileEntity(
            this StaffCreateRequest r,
            Guid staffCode,
            Guid userId,
            string? cardPhotoUrl)
        {
            if (!Guid.TryParse(r.RoleId, out var roleId))
            {
                throw new ValidationException("RoleId không phải GUID hợp lệ.");
            }

            return new StaffProfile
            {
                StaffCode = staffCode,
                UserId = userId,
                HireDate = DateOnly.FromDateTime(r.HireDate),
                TerminationDate = r.TerminationDate.HasValue
                                                ? DateOnly.FromDateTime(r.TerminationDate.Value)
                                                : (DateOnly?)null,
                Notes = string.IsNullOrWhiteSpace(r.Notes) ? null : r.Notes.Trim(),
                IsActive = r.IsActive,
                CurrentAddress = string.IsNullOrWhiteSpace(r.CurrentAddress) ? null : r.CurrentAddress.Trim(),
                EmergencyContactName = string.IsNullOrWhiteSpace(r.EmergencyContactName) ? null : r.EmergencyContactName.Trim(),
                EmergencyContactPhone = string.IsNullOrWhiteSpace(r.EmergencyContactPhone) ? null : r.EmergencyContactPhone.Trim(),
                EmergencyContactRelation = string.IsNullOrWhiteSpace(r.EmergencyContactRelation) ? null : r.EmergencyContactRelation.Trim(),
                BankAccountNo = string.IsNullOrWhiteSpace(r.BankAccountNo) ? null : r.BankAccountNo.Trim(),
                BankName = string.IsNullOrWhiteSpace(r.BankName) ? null : r.BankName.Trim(),
                BaseSalary = r.BaseSalary,
                TaxCode = string.IsNullOrWhiteSpace(r.TaxCode) ? null : r.TaxCode.Trim(),
                SocialInsuranceNo = string.IsNullOrWhiteSpace(r.SocialInsuranceNo) ? null : r.SocialInsuranceNo.Trim(),
                CardPhotoUrl = cardPhotoUrl,
                RoleId = roleId
            };
        }
    }
}
