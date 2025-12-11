using assignment_mvc_carrental.Models;
using assignment_mvc_carrental.ViewModels;
using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens; // För ClaimsIdentity, etc.
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt; // För JwtSecurityTokenHandler
using System.Security.Claims;


namespace assignment_mvc_carrental.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IMapper _mapper;
        private readonly IHttpClientFactory _clientfactory;

        public HomeController(ILogger<HomeController> logger, IMapper mapper, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _mapper = mapper;
            _clientfactory = httpClientFactory;
        }

        //GET: Login
        public IActionResult Login()
        {
            return View();
        }
        public async Task<IActionResult> LoginPost(UserLoginViewModel loginUserVM)
        {
            try
            {
                //Kalla på authcontroller här istället i api-projektet
                var _client = _clientfactory.CreateClient("CarRentalApi");
                LoginModel loginUser = _mapper.Map<LoginModel>(loginUserVM);

                var result = await _client.PostAsJsonAsync("api/Auth/login", loginUser);

                if (result.IsSuccessStatusCode)
                {
                    var tokenResponse = await result.Content.ReadFromJsonAsync<TokenResponse>();
                    string accessToken = tokenResponse.AccessToken;
                    string refreshToken = tokenResponse.RefreshToken;

                    // Läs ut info i hjälpmetod
                    // 1. Parsa JWT-info (Hela tuple-resultatet lagras i variabeln 'jwtInfo')
                    var jwtInfo = GetJwtExpIdRole(accessToken); 

                    if (string.IsNullOrEmpty(jwtInfo.UserId)) 
                    {
                        ModelState.AddModelError(string.Empty, "User id is missing.");
                        return View(loginUserVM);
                    }

                    // Spara tokeninfo i session 
                    StoreJwtInfoInSession(accessToken, refreshToken, jwtInfo.Expiration); 

                    // Claims för cookie hos mvc/klientsidan
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, jwtInfo.UserId), 
                        new Claim(ClaimTypes.Name, loginUserVM.Email),
                    };

                    // Lägg till alla roller
                    foreach (var role in jwtInfo.Roles) // Använd jwtInfo.Roles
                    {
                        claims.Add(new Claim(ClaimTypes.Role, role));
                    }
                    // 4. Skapa Principal och Authentication Properties
                    var identity = new ClaimsIdentity(claims, 
                        CookieAuthenticationDefaults.AuthenticationScheme);

                    var principal = new ClaimsPrincipal(identity);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = jwtInfo.Expiration 
                        // Sätt cookiens livslängd till samma som token
                    };

                    // SKAPA COOKIE-SESSION!!
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        principal,
                        authProperties);

                    _logger.LogInformation($"User signed in. Role: {jwtInfo.Roles}");
                    return RedirectToAction("Index", "Home");
                }

                else
                {
                    return Unauthorized("Failed to login. Please try again");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while trying to log in.");
                return Unauthorized("Something went wrong. Please try again");
            }
        }

        //public async IActionResult LogOutAsync() SKITER I DENNA JUST NU. SORRY :/
        //{
        //    var _client = _clientfactory.CreateClient("CarRentalApi");
        //    var result = await _client.LogOut("api/Auth/login", loginUser);
        //    return View();
        //}
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        //Spara tokeninfo i session för att ta mindre plats vid anrop
        private async Task StoreJwtInfoInSession
                           (string accessToken, 
                            string refreshToken, 
                            DateTimeOffset expirationDate)
        {
            HttpContext.Session.SetString("AccessToken", accessToken);
            HttpContext.Session.SetString("RefreshToken", refreshToken);
            HttpContext.Session.SetString("AccessTokenExpiration", expirationDate.ToString("O"));
        }

        // Hämtar info från JWT-token
        internal (DateTimeOffset Expiration, List<string> Roles, string UserId) 
                  GetJwtExpIdRole(string accessToken)
        {
            var handler = new JwtSecurityTokenHandler();

            List<string> Roles = new List<string> { }; // Tom backup
            string userId = "0"; // Tom backup
            var expirationDate = DateTimeOffset.Now.AddMinutes(0);// Tom backup

            try
            {              
                if (!handler.CanReadToken(accessToken))
                {
                    return (expirationDate, Roles, userId);
                }

                var jwtToken = handler.ReadJwtToken(accessToken);

                // Hämta ALLA Roll-Claims (FirstOrDefault blir FirstOrDefault(x).ToList())
                var roleClaims = jwtToken.Claims.Where(c => c.Type == "role").ToList();

                if (roleClaims.Any())
                {
                    // 1. Töm fallback-listan om vi hittar roller
                    Roles.Clear();

                    // 2. Samla alla roller i listan
                    foreach (var claim in roleClaims)
                    {
                        Roles.Add(claim.Value);
                    }
                }
                else
                {
                    // Om ingen roll hittades, sätt till "Customer"
                    Roles.Add("Customer");
                }

                userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub" 
                         || c.Type == ClaimTypes.NameIdentifier)?.Value;

                var expClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "exp");
                long secondsSinceEpoch = 0;

                if (expClaim != null && long.TryParse(expClaim.Value, out secondsSinceEpoch))
                {
                    expirationDate = DateTimeOffset.FromUnixTimeSeconds(secondsSinceEpoch);
                }

                return (expirationDate, Roles, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing JWT token.");
                return (expirationDate, Roles, userId);
                // Returnerar tomma värden vid fel
            }
        }


    }
}
