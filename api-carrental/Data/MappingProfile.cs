using AutoMapper;
using api_carrental.Dtos;


namespace api_carrental.Data
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            
            CreateMap<ApplicationUserDto, LoginUserDto>().ReverseMap();
            CreateMap<LoginUserDto, ApplicationUserDto>().ReverseMap();

            CreateMap<ApplicationUserDto, CreateNewUserDto>().ReverseMap();
            CreateMap<CreateNewUserDto, ApplicationUserDto>().ReverseMap();

            CreateMap<CreatingBookingDto, BookingDto>().ReverseMap();
            CreateMap<BookingDto, CreatingBookingDto>().ReverseMap();

            
            //CreateMap<AdminViewModel, ApplicationUser>().ReverseMap();
            //CreateMap<ApplicationUser, AdminViewModel>().ReverseMap();

            


        }
    }
}