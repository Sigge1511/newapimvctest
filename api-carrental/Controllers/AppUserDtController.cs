using api_carrental.Data;
using api_carrental.Dtos;
using api_carrental.Repos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace api_carrental.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppUserDtController : ControllerBase
    {
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IApplicationUser _applicationUser;
        private readonly UserManager<ApplicationUserDto> _userManager;
        private readonly SignInManager<ApplicationUserDto> _signInManager;
        private readonly IBookingRepo _bookingRepo;

        public AppUserDtController(ApplicationDbContext applicationDbContext, 
                                    IApplicationUser applicationUser, 
                                    UserManager<ApplicationUserDto> userManager, 
                                    SignInManager<ApplicationUserDto> signInManager,
                                    IBookingRepo bookingRepo)
        {
            _applicationDbContext = applicationDbContext;
            _applicationUser = applicationUser;
            _userManager = userManager;
            _signInManager = signInManager;
            _bookingRepo = bookingRepo;
        }
//***************************************************************************************************************

        //HÄMTA ALLA KUNDER FÖR LISTA OCH ÖVERSIKT
        // GET: api/<AppUserDtController>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<ApplicationUserDto>>> GetCustomersIndexAsync()
        {
            try
            {
                var allUsers = await _userManager.Users.ToListAsync();
                var customerUsers = new List<ApplicationUserDto>();

                //loopa igenom alla användare och kolla om de har rollen "Customer"
                foreach (var user in allUsers)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    if (roles.Contains("Customer"))
                    {
                        customerUsers.Add(user); //lägg till i lista
                    }
                }
                return Ok(customerUsers);
            }
            catch (Exception ex)
            {
                return BadRequest("Something went wrong. Please try again" + ex.Message);
            }
        }

        //HÄMTA ENSKILD KUND
        // GET api/<AppUserDtController>/5
        [Authorize(Roles = "Admin")]
        [HttpGet("{id}")]
        public async Task<ActionResult<ApplicationUserDto>> GetUserByIdAsync(int id)
        {            
            var user = await _applicationUser.GetUserByIdAsync(id);
            if (user == null) { return NotFound(); }

            return Ok(user);
        }


        // Denna rutt är unik och säger: "Hämta en AppUser som inkluderar bokningar"
        [HttpGet("bookings/{id}")]
        public async Task<ActionResult<ApplicationUserDto>> GetUserWithBookings(string id)
        {
            var user = await _applicationUser.GetUserWithBookingsAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return user;
        }

        // LÄGG TILL NY KUND
        // POST api/<AppUserDtController>
        [HttpPost]
        //ALLA SKA KUNNA SKAPA KONTO SJÄLVA
        public async Task<IActionResult> PostUserAsync([FromBody] string value)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Something went wrong. Please try again.");
            }

            try
            {
                ApplicationUserDto customer = new ApplicationUserDto
                {
                    UserName = /*newuserVM.UserName*/"customerusername",
                    Email = "/*newuserVM.Email*/"
                }
                ;
                // Skapa användaren via repo – och få IdentityResult tillbaka

                var result = await _applicationUser.AddCustomerAsync(customer);

                if (result.Succeeded)
                {
                    //om skapandet lyckades, tilldela rollen "Customer"
                    var user = await _userManager.FindByEmailAsync(customer.Email);
                    if (user != null)
                    {
                        await _userManager.AddToRoleAsync(user, "Customer");
                        return Ok("New customer created!");
                    }                                        
                    return BadRequest("Something went wrong. Please try again.");
                }
                return BadRequest("Unexpected error. Please try again.");
            }
            catch {return BadRequest("Unexpected error. Please try again.");}
        }

        // UPPDATERA KUND
        // PUT api/<AppUserDtController>/5
        [HttpPut("{id}")]       
        public async Task<IActionResult> PutCustomerAsync(int id, [FromBody] ApplicationUserDto appUser)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Something went wrong. Please try again.");
            }
            else
            {
                try
                {
                    var user = await _userManager.FindByIdAsync(appUser.Id);
                    if (user == null)
                        return BadRequest("Unexpected error. Please try again.");

                    // Uppdatera fält manuellt
                    user.FirstName = appUser.FirstName;
                    user.LastName = appUser.LastName;
                    user.Email = appUser.Email;
                    user.PhoneNumber = appUser.PhoneNumber;
                    user.Address = appUser.Address;
                    user.City = appUser.City;

                    var result = await _userManager.UpdateAsync(appUser);

                    if (result.Succeeded)
                    {
                        return Ok("Customer information updated successfully.");
                    }
                    return BadRequest("Unexpected error. Please try again.");
                }
                catch
                {
                    return BadRequest("Unexpected error. Please try again.");
                }
            }
        }

        // RADERA KUND
        // DELETE api/<AppUserDtController>/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUserConfirmed(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return BadRequest("Unexpected error. Please try again.");
                }

                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    return Ok("Customer has been deleted");
                }
                else { return BadRequest("Something went wrong. Try again."); }
            }
            catch
            {
                return BadRequest("Unexpected error. Please try again.");
            }
        }
    }
}
