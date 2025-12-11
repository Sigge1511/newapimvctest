using api_carrental.Dtos;
using Microsoft.AspNetCore.Identity;

namespace api_carrental.Repos
{
    public interface IApplicationUser
    {
        Task<(IdentityResult Result, ApplicationUserDto? User)> AddCustomerAsync(CreateNewUserDto appUser);

        Task<ApplicationUserDto?> GetUserWithBookingsAsync(string userId);
        Task<ApplicationUserDto> GetUserByIdAsync(int id);

        //låter resten av CRUD fixas av idenitys manager direkt i controllern
    }
}