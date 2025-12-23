using AutoMapper;
using SAMS_BE.DTOs.Response.Staff;
using SAMS_BE.Models;

namespace SAMS_BE.Mappers
{
    public sealed class StaffMappingProfile : Profile
    {
        public StaffMappingProfile()
        {
            // Entity có DateOnly? -> DTO dùng DateTime?
            CreateMap<StaffProfile, StaffListItemDto>()
                .ForMember(d => d.StaffCode, m => m.MapFrom(s => s.StaffCode))
                .ForMember(d => d.UserId, m => m.MapFrom(s => s.UserId))

                .ForMember(d => d.FirstName, m => m.MapFrom(s => s.User != null ? s.User.FirstName : null))
                .ForMember(d => d.LastName, m => m.MapFrom(s => s.User != null ? s.User.LastName : null))
                .ForMember(d => d.FullName, m => m.MapFrom(s =>
                    ((s.User!.FirstName ?? "") + " " + (s.User!.LastName ?? "")).Trim()))
                .ForMember(d => d.Email, m => m.MapFrom(s => s.User != null ? s.User.Email : null))
                .ForMember(d => d.Phone, m => m.MapFrom(s => s.User != null ? s.User.Phone : null))

                .ForMember(d => d.HireDate, m => m.MapFrom(s =>
                    s.HireDate.HasValue ? s.HireDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null))
                .ForMember(d => d.TerminationDate, m => m.MapFrom(s =>
                    s.TerminationDate.HasValue ? s.TerminationDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null));

            CreateMap<StaffProfile, StaffDetailDto>()
                .IncludeBase<StaffProfile, StaffListItemDto>()
                .ForMember(d => d.Notes, m => m.MapFrom(s => s.Notes));
        }
    }
}
