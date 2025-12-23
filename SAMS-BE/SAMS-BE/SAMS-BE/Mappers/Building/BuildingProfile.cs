using AutoMapper;
using SAMS_BE.DTOs.Response.Building;
using SAMS_BE.Infrastructure.Persistence.Global.Models;

namespace SAMS_BE.MappingProfiles
{
    public class BuildingProfile : Profile
    {
        public BuildingProfile()
        {
            CreateMap<building, BuildingDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.code))
                .ForMember(dest => dest.BuildingName, opt => opt.MapFrom(src => src.building_name))
                .ForMember(dest => dest.SchemaName, opt => opt.MapFrom(src => src.schema_name))
                .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.image_url))
                .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.latitude))
                .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.longitude))
                .ForMember(dest => dest.TotalAreaM2, opt => opt.MapFrom(src => src.total_area_m2))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.description))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.status));
        }
    }
}
