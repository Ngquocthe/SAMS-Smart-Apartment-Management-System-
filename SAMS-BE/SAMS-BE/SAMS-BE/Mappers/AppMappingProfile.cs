using AutoMapper;
using SAMS_BE.DTOs.Request.Staff;
using SAMS_BE.DTOs.Response.Building;
using SAMS_BE.DTOs.Response.Staff;
using SAMS_BE.Infrastructure.Persistence.Global.Models;
using SAMS_BE.Models;

namespace SAMS_BE.Mappers
{
    public sealed class AppMappingProfile : Profile
    {
        public AppMappingProfile()
        {
            // dbo.building -> BuildingDto
            CreateMap<building, BuildingDto>();

            // building.staff_profiles (+ User) -> Staff DTOs
            CreateMap<StaffProfile, StaffListItemDto>()
                .ForMember(d => d.FirstName, m => m.MapFrom(s => s.User != null ? s.User.FirstName : null))
                .ForMember(d => d.LastName, m => m.MapFrom(s => s.User != null ? s.User.LastName : null))
                .ForMember(d => d.FullName, m => m.MapFrom(s =>
                    ((s.User!.FirstName ?? "") + " " + (s.User!.LastName ?? "")).Trim()))
                .ForMember(d => d.Email, m => m.MapFrom(s => s.User != null ? s.User.Email : null))
                .ForMember(d => d.Phone, m => m.MapFrom(s => s.User != null ? s.User.Phone : null));

            CreateMap<StaffProfile, StaffDetailDto>()
                .IncludeBase<StaffProfile, StaffListItemDto>();

            // Update DTO -> Entities (map partial nếu source có giá trị)
            CreateMap<StaffUpdateDto, StaffProfile>()
                .ForAllMembers(opts => opts.Condition((src, _, srcMember) => srcMember != null));

            CreateMap<StaffUpdateDto, User>()
                .ForAllMembers(opts => opts.Condition((src, _, srcMember) => srcMember != null));

            // Create DTO -> Entities
            CreateMap<StaffCreateRequest, User>()
                .ForMember(d => d.UserId, m => m.Ignore())
                .ForMember(d => d.Phone, m => m.MapFrom(s => s.Phone ?? ""));

            CreateMap<StaffCreateRequest, StaffProfile>()
                .ForMember(d => d.StaffCode, m => m.Ignore())
                .ForMember(d => d.TerminationDate, m => m.Ignore());
        }
    }
}
