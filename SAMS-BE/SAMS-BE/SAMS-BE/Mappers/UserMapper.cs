using SAMS_BE.DTOs.Response;
using SAMS_BE.Models;

namespace SAMS_BE.Mappers
{
    public static class UserMapper
    {
        public static LoginUserDto ToLoginUserDto(this User u)
        {
            return new LoginUserDto
            {
                UserId = u.UserId,
                Username = u.Username,
                Email = u.Email,
                Phone = u.Phone,
                FirstName = u.FirstName,
                LastName = u.LastName,
                FullName = $"{u.LastName} {u.FirstName}".Trim(),
                Dob = u.Dob.HasValue ? u.Dob.Value.ToString("dd-MM-yyyy") : null,
                Address = u.Address,
                AvatarUrl = u.AvatarUrl,
                CheckinPhotoUrl = u.CheckinPhotoUrl,
            };
        }
    }
}
