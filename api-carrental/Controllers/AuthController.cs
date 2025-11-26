using api_carrental.Constants;
using api_carrental.Data;
using api_carrental.Dtos;
using api_carrental.Repos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace api_carrental.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly JwtSettings _jwtSettings;
        private readonly IApplicationUser _applicationUser;
        private readonly UserManager<ApplicationUserDto> _userManager;
        private readonly SignInManager<ApplicationUserDto> _signInManager;

        public AuthController(ApplicationDbContext applicationDbContext,
                                    IOptions<JwtSettings> jwtSettings,
                                    IApplicationUser applicationUser,
                                    UserManager<ApplicationUserDto> userManager,
                                    SignInManager<ApplicationUserDto> signInManager)
        {
            _applicationDbContext = applicationDbContext;
            _jwtSettings = jwtSettings.Value;
            _applicationUser = applicationUser;
            _userManager = userManager;
            _signInManager = signInManager;
        }
//***************************************************************************************************************

        [HttpPost("/admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminLogin(LoginUserDto userDto)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(userDto.Email);
                var passwordValid = await _userManager.CheckPasswordAsync(user, userDto.Password); 
                // Bool som jämför password från user och det som skickas in userDto.Password dvs

                if (user == null || passwordValid == false) // För att inte ge ut om bara password eller User är felaktigt. 
                {
                    return BadRequest("Something went wrong, please try again."); // Skickas              
                }

                // Add what ever is needed to create a JWT. ?
                var result = await _signInManager.PasswordSignInAsync(user, userDto.Password, isPersistent: true, lockoutOnFailure: false);

                if (!result.Succeeded) return Unauthorized();
                return Ok(new { message = "Logged in as admin" });
            }
            catch (Exception)
            {
                return Problem($"Something went wrong in the", statusCode: 500);
            }
        }


        [HttpPost("login")]
        public IActionResult UserLogin([FromBody] LoginUserDto loginusermodel)
        {
            // ... validera användare ...

            // ... skapa claims ...


            // ...
            return Ok();
        }

        private async Task<TokenCollection> CreateAccessToken(ApplicationUserDto appUserDto)
        {
            try
            {
                // Hämta roller
                var roles = await _userManager.GetRolesAsync(appUserDto);

                // Skapa claims och lägg i en lista
                var claims = new List<Claim>();
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                // Hämta min key
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
                // Skapa credentials - vad är detta?
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                //Skapa sen ny JWT
                var token = new JwtSecurityToken(
                    issuer: _jwtSettings.Issuer,
                    audience: _jwtSettings.Audience,
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(_jwtSettings.AccessTokenExpInMinutes),
                    signingCredentials: credentials);

                //anropa för att få refresh token - lägg båda tokens i ett objekt och returnera
                var tokenPair = new TokenCollection
                {
                    AccessToken = token,
                    RefreshToken = await CreateRefreshToken(token)
                };
                return tokenPair;
            }
            catch (Exception)
            {
                var emptyTokenPair = new TokenCollection();
                return emptyTokenPair;
            }
        }

        private async Task<JwtSecurityToken> CreateRefreshToken(JwtSecurityToken refreshToken)
        {
            
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,                
                expires: DateTime.Now.AddMinutes(_jwtSettings.RefreshTokenExpInHours),
                signingCredentials: credentials);
            // ... returnera token

            return token;
        }
    }
}
