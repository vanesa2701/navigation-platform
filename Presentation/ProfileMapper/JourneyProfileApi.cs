using AutoMapper;
using DTO.WebApiDTO.Journey;
using DTO.DTO.Journey;

namespace Presentation.ProfileMapper
{
    public class JourneyProfileApi : Profile
    {
        public JourneyProfileApi()
        {
            CreateMap<AddJourneyRequestDtoApi, AddJourneyRequestDto>()
                .ForMember(dest => dest.UserId, opt => opt.Ignore());

            CreateMap<JourneyDto, JourneyDtoApi>();

            CreateMap<JourneyShareRequestDtoApi, JourneyShareRequestDto>();
            CreateMap<JourneyShareResponseDto, JourneyShareResponseDtoApi>();
            CreateMap<PublicJourneyLinkResponseDto, PublicJourneyLinkResponseDtoApi>();
            CreateMap<JourneyPublicLinkDto, JourneyPublicLinkDtoApi>();
            CreateMap<JourneyFilterRequestDtoApi, JourneyFilterRequestDto>();
            CreateMap<JourneyFilterResponseDto, JourneyFilterResponseDtoApi>();
            CreateMap<MonthlyRouteDistanceDtoApi, MonthlyRouteDistanceDto>();
            CreateMap<MonthlyRouteDistanceResponseDto, MonthlyRouteDistanceResponseDtoApi>();
            CreateMap<DTO.WebApiDTO.Journey.JourneyUnshareRequestDtoApi, DTO.DTO.Journey.JourneyUnshareRequestDto>();

        }
    }
}

