using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Mappers;
using SAMS_BE.Models;
using SAMS_BE.Utils;
using Microsoft.EntityFrameworkCore;
using static System.Net.WebRequestMethods;
using SAMS_BE.Interfaces.IRepository.GlobalAdmin;
using SAMS_BE.DTOs.Response;

namespace SAMS_BE.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepo;
        private readonly IHttpContextAccessor _http;
        private readonly BuildingManagementContext _context;
        private readonly IAdminUserRepository _adminUserRepo;

        public UserService(IUserRepository userRepo, IHttpContextAccessor http, BuildingManagementContext context, IAdminUserRepository adminUserRepo)
        {
            _userRepo = userRepo;
            _http = http;
            _context = context;
            _adminUserRepo = adminUserRepo;
        }

        public async Task<LoginUserDto?> GetLoginUserAsync(Guid id)
        {
            var principal = _http.HttpContext?.User
                         ?? throw new UnauthorizedAccessException();

            var isGlobalAdmin = principal.IsInRole("global_admin");

            if (isGlobalAdmin)
            {
                var adminUser = await _adminUserRepo.GetByIdAsync(id);
                return adminUser?.ToLoginUserDto();
            }

            AuthGuards.EnsureSubMatchesOrThrow(principal, id);

            var user = await _userRepo.GetByIdAsync(id);
            return user?.ToLoginUserDto();
        }

        public async Task<Apartment?> GetUserPrimaryApartmentAsync(Guid userId)
        {
            // Tìm căn hộ chính (primary) của user thông qua ResidentProfile và ResidentApartment
            // User → ResidentProfile → ResidentApartment → Apartment
            var residentProfile = await _context.ResidentProfiles
                .Include(rp => rp.ResidentApartments)
                .ThenInclude(ra => ra.Apartment)
                .FirstOrDefaultAsync(rp => rp.UserId == userId);

            if (residentProfile == null)
            {
                return null;
            }

            var primaryApartment = residentProfile.ResidentApartments
                .Where(ra => ra.IsPrimary)
                .Select(ra => ra.Apartment)
                .FirstOrDefault();

            if (primaryApartment != null)
            {
                return primaryApartment;
            }

            // Fallback: nếu chưa đánh dấu primary, lấy căn hộ gắn gần nhất (theo StartDate)
            var fallbackApartment = residentProfile.ResidentApartments
                .OrderByDescending(ra => ra.StartDate)
                .Select(ra => ra.Apartment)
                .FirstOrDefault();

            return fallbackApartment;
        }
    }
}
