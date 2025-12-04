using api_carrental.Constants;
using api_carrental.Data;
using api_carrental.Dtos;
using api_carrental.Repos;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
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
        private readonly ILogger _logger;
        private readonly IMapper _mapper;

        public AuthController(ApplicationDbContext applicationDbContext,
                                    IOptions<JwtSettings> jwtSettings,
                                    IApplicationUser applicationUser,
                                    UserManager<ApplicationUserDto> userManager,
                                    SignInManager<ApplicationUserDto> signInManager,
                                    ILogger logger,
                                    IMapper mapper)
        {
            _applicationDbContext = applicationDbContext;
            _jwtSettings = jwtSettings.Value;
            _applicationUser = applicationUser;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _mapper = mapper;
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
        public async Task<IActionResult> UserLoginAsync([FromBody] LoginUserDto loginusermodel)
        {
            try
            {
                //returnUrl ??= Url.Content("~/");
                if (ModelState.IsValid)
                {
                    //Behöver mappa mellan ApplicationUserDto och LoginUserDto
                    ApplicationUserDto userDto = _mapper.Map<ApplicationUserDto>(loginusermodel);

                    // Skickar med false på slutet för jag vill inte
                    // låsa kontot vid misslyckad inloggning. Kan va dumt vid felsök
                    var result = await _signInManager.CheckPasswordSignInAsync(userDto, loginusermodel.Password, false);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User logged in.");
                        await CreateAccessToken(userDto);
                        return Ok(userDto);
                    }
                    else
                    {
                        return Unauthorized("Invalid login attempt.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while trying to log in.");
                return BadRequest("Something went wrong, please try again.");
            }
            return BadRequest("Something went wrong, please try again.");
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
