using System;
using System.Collections.Generic;

namespace SAMS_BE.DTOs
{
    public class ResidentApartmentDto
    {
        public Guid ResidentApartmentId { get; set; }
        public Guid ApartmentId { get; set; }
        public string RelationType { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public bool IsPrimary { get; set; }
        public string? ApartmentNumber { get; set; }
    }

    public class ResidentUserDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateOnly? Dob { get; set; }
        public string? Address { get; set; }
        public string? AvatarUrl { get; set; }
        public string? CheckinPhotoUrl { get; set; }
    }

    public class ResidentDto
    {
        public Guid ResidentId { get; set; }
        public Guid? UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? IdNumber { get; set; }
        public DateOnly? Dob { get; set; }
        public string? Gender { get; set; }
        public string? Address { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool HasFaceRegistered { get; set; }

        public ResidentUserDto? User { get; set; }
        public List<ResidentApartmentDto> Apartments { get; set; } = new List<ResidentApartmentDto>();
    }
}


