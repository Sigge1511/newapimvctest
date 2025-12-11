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


            //CreateMap<VehicleViewModel, Vehicle>().ReverseMap();
            //CreateMap<Vehicle, VehicleViewModel>().ReverseMap();

            //CreateMap<Booking, BookingViewModel>()
            //    .ForMember(dest => dest.Vehicle, opt => opt.MapFrom(src => src.Vehicle))
            //    .ForMember(dest => dest.ApplicationUser, opt => opt.MapFrom(src => src.ApplicationUser));
            //CreateMap<BookingViewModel, Booking>()
            //    .ForMember(dest => dest.Vehicle, opt => opt.MapFrom(src => src.Vehicle))
            //    .ForMember(dest => dest.ApplicationUser, opt => opt.MapFrom(src => src.ApplicationUser));

            //CreateMap<AdminViewModel, ApplicationUser>().ReverseMap();
            //CreateMap<ApplicationUser, AdminViewModel>().ReverseMap();

            //CreateMap<UserInputViewModel, CustomerViewModel>().ReverseMap();
            //CreateMap<CustomerViewModel, UserInputViewModel>().ReverseMap();
            //CreateMap<ApplicationUser, CustomerViewModel>();


        }
    }
}