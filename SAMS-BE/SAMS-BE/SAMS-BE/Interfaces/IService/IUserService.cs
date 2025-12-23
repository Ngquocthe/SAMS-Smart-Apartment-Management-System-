using SAMS_BE.DTOs.Response;
using SAMS_BE.Models;

namespace SAMS_BE.Interfaces.IService
{
    public interface IUserService
    {
        Task<LoginUserDto?> GetLoginUserAsync(Guid id);
        Task<Apartment?> GetUserPrimaryApartmentAsync(Guid userId);
    }
}
