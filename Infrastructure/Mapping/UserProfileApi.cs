using AutoMapper;
using DTO.DTO.User;

namespace Infrastructure.MapperProfile
{
    public class UserProfileApi : Profile
    {
        public UserProfileApi() 
        {
            CreateMap<RegisterRequestDto, RegisterRequestDto>();
            CreateMap<RegisterResponseDto, RegisterResponseDto>();
            CreateMap<DTO.WebApiDTO.User.LoginRequestDtoApi, DTO.WebApiDTO.User.LoginRequestDtoApi>();

        }
    }
}

