using SAMS_BE.DTOs.Request.Apartment;
using SAMS_BE.DTOs.Request.Resident;
using SAMS_BE.Models;

namespace SAMS_BE.Mappers;

public static class ResidentMapper
{
    public static User ToUserEntity(this CreateResidentRequest dto, Guid userId)
    {
        return new User
        {
            UserId = userId,
            Username = dto.Username,
            Email = dto.Email ?? string.Empty,
            Phone = dto.Phone ?? string.Empty,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Dob = dto.Dob,
            Address = dto.Address,
            AvatarUrl = null,
            CheckinPhotoUrl = null,
            CreatedAt = DateTime.UtcNow.AddHours(7),
            UpdatedAt = null
        };
    }

    public static ResidentProfile ToResidentProfileEntity(this CreateResidentRequest dto, Guid residentId, Guid? userId)
    {
        var fullName = $"{dto.LastName} {dto.FirstName}".Trim();

        return new ResidentProfile
        {
            ResidentId = residentId,
            UserId = userId,
            FullName = fullName,
            Phone = dto.Phone,
            Email = dto.Email,
            IdNumber = dto.IdNumber,
            Dob = dto.Dob,
            Gender = dto.Gender,
            Address = dto.Address,
            Status = dto.Status ?? "ACTIVE",
            IsVerified = dto.IsVerified ?? true,
            VerifiedAt = dto.VerifiedAt,
            Nationality = dto.Nationality,
            InternalNote = dto.InternalNote,
            Meta = dto.Meta,
            CreatedAt = DateTime.UtcNow.AddHours(7),
            UpdatedAt = null
        };
    }

    public static ResidentApartment ToResidentApartmentEntity(this ResidentApartmentRequest dto, Guid residentId)
    {
        return new ResidentApartment
        {
            ResidentApartmentId = Guid.NewGuid(),
            ResidentId = residentId,
            ApartmentId = dto.ApartmentId,
            RelationType = dto.RelationType,
            StartDate = dto.StartDate!.Value,
            EndDate = dto.EndDate,
            IsPrimary = dto.IsPrimary
        };
    }
}


