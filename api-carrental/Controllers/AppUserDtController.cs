using api_carrental.Data;
using api_carrental.Dtos;
using api_carrental.Repos;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;


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
        private readonly IMapper _mapper;

        public AppUserDtController(ApplicationDbContext applicationDbContext, 
                                    IApplicationUser applicationUser, 
                                    UserManager<ApplicationUserDto> userManager, 
                                    SignInManager<ApplicationUserDto> signInManager,
                                    IBookingRepo bookingRepo,
                                    IMapper mapper)
        {
            _applicationDbContext = applicationDbContext;
            _applicationUser = applicationUser;
            _userManager = userManager;
            _signInManager = signInManager;
            _bookingRepo = bookingRepo;
            _mapper = mapper;
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
        public async Task<IActionResult> PostUserAsync(CreateNewUserDto newUser)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest("Something went wrong. Please try again.");
            }

            try
            {
                var (addingResult, createdUserDto) = await _applicationUser.AddCustomerAsync(newUser);
                if (addingResult.Succeeded && createdUserDto != null)
                {
                    // Skapa URI:n manuellt atm
                    string resourceUri = $"/api/AppUserDt/{createdUserDto.Id}";

                    // Svara med Status 201 Created med URI + DTO
                    return Created(resourceUri, createdUserDto);
                }
                else
                {
                    // Registration failed (Identity validation - lösenord/email)
                    var identityErrors = addingResult.Errors.Select(e => e.Description);
                    return BadRequest(new { Errors = identityErrors, Message = "Registration failed (Identity validation)." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Unexpected server error during user creation.", Debug = ex.Message });
            }
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
