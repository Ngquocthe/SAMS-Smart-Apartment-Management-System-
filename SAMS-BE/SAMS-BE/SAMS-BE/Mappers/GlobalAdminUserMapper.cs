using SAMS_BE.DTOs.Response;
using SAMS_BE.Infrastructure.Persistence.Global.Models;

namespace SAMS_BE.Mappers
{
    public static class GlobalAdminUserMapper
    {
        public static LoginUserDto ToLoginUserDto(this user_registry a)
            => new()
            {
                UserId = a.keycloak_user_id != Guid.Empty ? a.keycloak_user_id : a.id,
                Username = a.username,
                Email = a.email,
                Phone = null,
                FirstName = null,
                LastName = null,
                FullName = a.username, 
                Dob = null,
                Address = null,
                AvatarUrl = null,
            };
    }
}
