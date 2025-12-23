using AutoMapper;
using SAMS_BE.DTOs.Response.Building;
using SAMS_BE.Infrastructure.Persistence.Global.Models;

namespace SAMS_BE.Mappers
{
    public sealed class GlobalMappingProfile : Profile
    {
        public GlobalMappingProfile()
        {
            // Map entity dbo.building -> DTO
            CreateMap<building, BuildingDto>()
                .ForMember(d => d.Id, m => m.MapFrom(s => s.id))
                .ForMember(d => d.Code, m => m.MapFrom(s => s.code))
                .ForMember(d => d.SchemaName, m => m.MapFrom(s => s.schema_name))
                .ForMember(d => d.BuildingName, m => m.MapFrom(s => s.building_name));
            // nếu DTO còn field khác thì map tiếp ở đây
        }
    }
}
