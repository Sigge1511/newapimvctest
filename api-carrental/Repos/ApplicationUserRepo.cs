using api_carrental.Data;
using api_carrental.Dtos;
using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace api_carrental.Repos
{
    public class ApplicationUserRepo : IApplicationUser
    {
        private readonly UserManager<ApplicationUserDto> _userManager;
        private readonly IBookingRepo _bookingRepo;
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        //private readonly ILogger _logger;

        public ApplicationUserRepo(UserManager<ApplicationUserDto> userManager, 
                                   IBookingRepo bookingRepo, 
                                   ApplicationDbContext context,
                                   IMapper mapper/*,*/
                                   /*ILogger logger*/)
        {
            _userManager = userManager;
            _bookingRepo = bookingRepo;
            _context = context;
            _mapper = mapper;
            //_logger = logger;
        }

        //****************************************************************************************************************
        public async Task<(IdentityResult Result, ApplicationUserDto? User)> AddCustomerAsync(CreateNewUserDto createNewUser)
        {
            // Deklarera här + sätt default
            // så det är in scope för att använda senare
            IdentityResult addingResult = IdentityResult.Failed();

            //Mappa om inputmodell till ordinarie modell
            //så jag kan använda UserManager senare
            ApplicationUserDto newUserDto = _mapper.Map<ApplicationUserDto>(createNewUser);
            try
            {
                // Skapa användaren mha ordinarie modell
                //   + lösen från inputen i CreateNewUserDto 
                addingResult = await _userManager.CreateAsync(newUserDto, createNewUser.Password);

                if (addingResult.Succeeded)
                {
                    // Lägg på roll
                    var roleResult = await _userManager.AddToRoleAsync(newUserDto, "Customer");

                    if (!roleResult.Succeeded)
                    {
                        // Om roll misslyckas, men användaren skapades:
                        // Logga och låt Controllern hantera 500.
                        // Vi returnerar det lyckade IdentityResult från CreateAsync,
                        // men skickar med en varning/logg internt.

                        //_logger.LogError($"User created but failed to assign 'Customer' role. " +
                        //                 $"UserId: {newUserDto.Id}, Email: {newUserDto.UserName}");
                        return (addingResult, newUserDto);
                    }


                    // Returnera det lyckade resultatet och DTO:n
                    return (addingResult, newUserDto);
                }

                // 5. Om skapandet misslyckades (valideringsfel)
                return (addingResult, null);
            }
            catch (Exception)
            {
                // 6. Oväntat fel (server/databas)
                return (IdentityResult.Failed(new IdentityError { Description = "An unexpected server error occurred during creation." }), null);
            }
        }
        public async Task<ApplicationUserDto> GetUserByIdAsync(int id)
        {
            throw new NotImplementedException();
        }
        public async Task<ApplicationUserDto?> GetUserWithBookingsAsync(string userId)
        {
            return await _context.Users
                                .Include(u => u.Bookings!)
                                    .ThenInclude(b => b.Vehicle)
                                .FirstOrDefaultAsync(u => u.Id == userId);
        }


        //Låter resten av CRUD fixas av idenitys manager direkt i controllern vilket
        //ska va ok i mindre projekt vad jag läst mig till 
    }
}
