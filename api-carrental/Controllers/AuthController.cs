using api_carrental.Constants;
using api_carrental.Data;
using api_carrental.Dtos;
using api_carrental.Repos;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace api_carrental.Controllers
{
    [ApiController]
    [Route("api/[controller]")]    
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly JwtSettings _jwtSettings;
        private readonly IApplicationUser _applicationUser;
        private readonly UserManager<ApplicationUserDto> _userManager;
        private readonly SignInManager<ApplicationUserDto> _signInManager;
        private readonly ILogger<AuthController> _logger;
        private readonly IMapper _mapper;

        public AuthController(ApplicationDbContext applicationDbContext,
                                    IOptions<JwtSettings> jwtSettings,
                                    IApplicationUser applicationUser,
                                    UserManager<ApplicationUserDto> userManager,
                                    SignInManager<ApplicationUserDto> signInManager,
                                    ILogger<AuthController> logger,
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
                    // Hitta user i db
                    var user = await _userManager.FindByEmailAsync(loginusermodel.Email);

                    if (user != null)
                    {
                        // Kolla lösenordet
                        var passwordIsValid = await _userManager.CheckPasswordAsync(user, loginusermodel.Password);

                        if (passwordIsValid)
                        {
                            // SKAPA JWT
                            _logger.LogInformation($"User {user.Email} logged in.");
                            var tokenpair = await CreateAccessToken(user);

                            return Ok(tokenpair);
                        }
                        else
                        {
                            return Unauthorized("Invalid login attempt.");
                        }
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

//*********************** SKAPA TOKENS *****************************************************
        private async Task<TokenCollection> CreateAccessToken(ApplicationUserDto appUserDto)
        {
            // Steg A: Hämta den riktiga ApplicationUser entiteten FÖR DB-OPERATIONER
            var currentAppUser = await _userManager.FindByEmailAsync(appUserDto.Email);

            if (currentAppUser == null)
            {
                throw new ApplicationException("Something went wrong.");
            }

            try
            {
                var roles = await _userManager.GetRolesAsync(currentAppUser);
                // Skapa claims och lägg i en lista
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, currentAppUser.Email!), // Email som Name Claim
                    new Claim(JwtRegisteredClaimNames.Sub, currentAppUser.Id)
                };
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                // Hämta min key
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
                // Skapa credentials
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                // Skapa sen ny JWT (Access Token)
                var accsessToken = new JwtSecurityToken(
                    issuer: _jwtSettings.Issuer,
                    audience: _jwtSettings.Audience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpInMinutes),
                    signingCredentials: credentials);

                // Gör om accsessToken till sträng
                var tokenHandler = new JwtSecurityTokenHandler();
                var accessTokenString = tokenHandler.WriteToken(accsessToken);

                // 1. Skapa Refresh Token (RT) som en JWT via din metod
                var refreshTokenJwt = await CreateRefreshToken(currentAppUser); // Använder currentAppUser
                var refreshTokenString = tokenHandler.WriteToken(refreshTokenJwt);

                // Hämta utgångsdatumet från den skapade RT:n
                var refreshTokenExpiryTime = refreshTokenJwt.ValidTo;

                // 2. Steg 3: Spara RT i databasen (Initial Lagring)
                currentAppUser.RefreshTokenValue = refreshTokenString; // Sparar hela RT JWT strängen
                currentAppUser.RefreshTokenExpiryTime = refreshTokenExpiryTime;

                var updateResult = await _userManager.UpdateAsync(currentAppUser);
                if (!updateResult.Succeeded)
                {
                    throw new ApplicationException("Something went wrong. Could not update user.");
                }

                var tokenPair = new TokenCollection
                {
                    AccessToken = accessTokenString,
                    RefreshToken = refreshTokenString,
                };

                return tokenPair;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating access token.");
                var emptyTokenPair = new TokenCollection();
                return emptyTokenPair;
            }
        }
        private async Task<JwtSecurityToken> CreateRefreshToken(ApplicationUserDto applicationUserDto)
        {
            // Vi lägger bara till de claims som behövs för att identifiera användaren
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, applicationUserDto.Email!), 
                //Behövs för CheckRefreshToken
                new Claim(JwtRegisteredClaimNames.Sub, applicationUserDto.Id)
            };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(_jwtSettings.RefreshTokenExpInHours), 
                signingCredentials: credentials);

            return token;
        }

//*********************** EV FÖRNYA TOKENS *****************************************************
        public async Task<TokenCollection> CheckRefreshToken(string refreshToken)
        {
            // Förbered handler och variabler
            var tokenHandler = new JwtSecurityTokenHandler();
            ClaimsPrincipal principal;

            try
            {
                // Kolla om accsessToken är giltig
                principal = tokenHandler.ValidateToken(refreshToken, 
                    new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidAudience = _jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
                    ClockSkew = TimeSpan.Zero
                }, 
                    out SecurityToken validatedToken);
            }
            catch (SecurityTokenException ex)
            {
                // Om det inte går bra svarar den med unauth.
                throw new UnauthorizedAccessException("You have been logged out.", ex);
            }

            try
            {
                var userEmailClaim = principal.Identity?.Name;
                if (string.IsNullOrEmpty(userEmailClaim))
                {
                    throw new UnauthorizedAccessException("User info missing. You have been logged out.");
                }

                // Kolla user i db
                var appUserDto = await _userManager.FindByEmailAsync(userEmailClaim);
                if (appUserDto == null)
                {
                    throw new UnauthorizedAccessException("Something went wrong.");
                }

                // Kolla RT i db, om det går fel svara med unauth.
                //annars skapa nya tokens
                if (appUserDto.RefreshTokenValue != refreshToken || appUserDto.RefreshTokenExpiryTime <= DateTime.UtcNow)
                {
                    _logger.LogWarning($"Invalid or expired refresh token for user {appUserDto.Email}.");
                    throw new UnauthorizedAccessException("You have been logged out." +
                        "");
                }
                TokenCollection newTokenPair = await CreateAccessToken(appUserDto);
                return newTokenPair;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred.");
                var emptyTokenPair = new TokenCollection();
                return emptyTokenPair;
            }
        }
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> NeedNewTokens([FromBody] RefreshTokenRequest request)
        {
            // Koll av argument
            if (request == null || string.IsNullOrEmpty(request.RefreshToken))
            {
                _logger.LogWarning("Refresh request received without a token.");
                return BadRequest(new { message = "You've been logged out." });
            }

            try
            {
                // Kollar:
                // a) Validering av RT 
                // b) Koll mot db
                // c) Ny AT/RT 
                TokenCollection newTokenPair = await CheckRefreshToken(request.RefreshToken);
                //Svara med nya tokens om det gick bra
                return Ok(newTokenPair);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Annars 401 och logga ut användaren helt.
                _logger.LogWarning(ex, "Refresh token-validation failed.");
                return Unauthorized(new { message = "You've been logged out due to inactivity." });
            }
            catch (Exception ex)
            {
                // 5. Oväntat Serverfel
                _logger.LogError(ex, "Something went wrong.");
                return StatusCode(500, new { message = "Something went wrong." });
            }
        }

        //*********************** LOGGA UT *****************************************************

        //[HttpPost("logout")]  BÄTTRE LYCKA NÄSTA GÅNG. FUNKTIONEN ÄR EJ FÄRDIGBYGGD
        //public async Task<IActionResult> Logout()
        //{
        //    try
        //    {
        //        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //        var currentUser = await _userManager.FindByIdAsync(userId);
        //        if (currentUser != null)
        //        {
        //            // Nolla allt med tokens
        //            currentUser.RefreshTokenValue = null;
        //            currentUser.RefreshTokenExpiryTime = DateTime.Now;

        //            var updateResult = await _userManager.UpdateAsync(currentUser);

        //            if (!updateResult.Succeeded)
        //            {
        //                _logger.LogError($"Failed to revoke RT for user {currentUser.Email}");
        //                return StatusCode(500, new { message = "Logout failed due to DB error." });
        //            }
        //        }
        //        await _signInManager.SignOutAsync();

        //        _logger.LogInformation($"User {userId} successfully logged out and RT revoked.");
        //        return Ok(new { message = "Logged out successfully" });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "An error occurred during logout.");
        //        return StatusCode(500, new { message = "Something went wrong. Please try again." });
        //    }
        //}
    }
}
