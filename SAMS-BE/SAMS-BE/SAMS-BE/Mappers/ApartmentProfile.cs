using AutoMapper;
using SAMS_BE.DTOs;
using SAMS_BE.Models;

namespace SAMS_BE.Mappers
{
    public class FloorProfile : Profile
    {
        public FloorProfile()
        {
            // Floor mappings
            CreateMap<Floor, FloorResponseDto>()
                .ForMember(dest => dest.ApartmentCount, opt => opt.MapFrom(src => src.Apartments.Count));
        }
    }

    public class ApartmentProfile : Profile
    {
        public ApartmentProfile()
        {
            // Apartment mappings
            CreateMap<CreateApartmentDto, Apartment>()
                .ForMember(dest => dest.ApartmentId, opt => opt.MapFrom(src => Guid.NewGuid()))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

            CreateMap<Apartment, ApartmentResponseDto>()
                .ForMember(dest => dest.FloorNumber, opt => opt.MapFrom(src => src.Floor != null ? src.Floor.FloorNumber : 0))
                .ForMember(dest => dest.FloorName, opt => opt.MapFrom(src => src.Floor != null ? src.Floor.Name : string.Empty));

            // For replication - clone apartment
            CreateMap<Apartment, Apartment>()
                .ForMember(dest => dest.ApartmentId, opt => opt.MapFrom(src => Guid.NewGuid()))
                .ForMember(dest => dest.FloorId, opt => opt.Ignore()) // Ignore FloorId để set manual
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());
        }
    }

    public class AnnouncementProfile : Profile
    {
        public AnnouncementProfile()
        {
            // Announcement mappings
            CreateMap<Announcement, AnnouncementResponseDto>();

            CreateMap<CreateAnnouncementDto, Announcement>()
                .ForMember(dest => dest.AnnouncementId, opt => opt.MapFrom(src => Guid.NewGuid()))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

            CreateMap<UpdateAnnouncementDto, Announcement>()
                .ForMember(dest => dest.AnnouncementId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
        }
    }
}