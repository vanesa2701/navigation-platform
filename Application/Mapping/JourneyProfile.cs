using AutoMapper;
using Domain.Entities;
using DTO.DTO.Journey;
using DTO.WebApiDTO.Journey;

namespace Application.Mapping
{
    public class JourneyProfile : Profile
    {
        public JourneyProfile()
        {
            CreateMap<Journey, JourneyDto>();
            CreateMap<AddJourneyRequestDto, Journey>()
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.JourneyShares, opt => opt.Ignore())
                .ForMember(dest => dest.JourneyPublicLinks, opt => opt.Ignore());

            CreateMap<JourneyPublicLink, JourneyPublicLinkDto>();
            CreateMap<JourneyUnshareRequestDtoApi, JourneyUnshareRequestDto>();
        }
    }
}

