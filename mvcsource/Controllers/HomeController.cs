using assignment_mvc_carrental.Models;
using assignment_mvc_carrental.ViewModels;
using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt; // För JwtSecurityTokenHandler
using Microsoft.IdentityModel.Tokens; // För ClaimsIdentity, etc.


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
                    // 1. Läs svaret
                    var tokenResponse = await result.Content.ReadFromJsonAsync<TokenResponse>();

                    // 2. Fortsätt till nästa steg för att hantera token
                    await StoreTokenAndClaims(tokenResponse.AccessToken, tokenResponse.RefreshToken); // Se Steg 2

                    _logger.LogInformation("User logged in successfully and session created.");
                    return RedirectToAction("Index", "Home"); // Använd RedirectToAction för att undvika LocalRedirect-felet
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


        private async Task StoreTokenAndClaims(string accessToken, string refreshToken)
        {
            // 1. Extrahera claims och utgångsdatum från Access Token (AT)
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(accessToken) as JwtSecurityToken;
            var claims = jsonToken.Claims.ToList();

            // Lägg till de unika tokensträngarna som claims (essentiellt för skoluppgiften)
            claims.Add(new Claim("access_token", accessToken));
            claims.Add(new Claim("refresh_token", refreshToken));

            // 2. Extrahera utgångsdatum ('exp' claim)
            var expirationClaim = claims.FirstOrDefault(c => c.Type == "exp");
            DateTimeOffset expirationDate;

            if (expirationClaim != null && long.TryParse(expirationClaim.Value, out long secondsSinceEpoch))
            {
                // Konvertera UNIX-tidstämpel till DateTimeOffset
                expirationDate = DateTimeOffset.FromUnixTimeSeconds(secondsSinceEpoch);
            }
            else
            {
                // Fallback: Om claim saknas/är fel, sätt en kort giltighet
                expirationDate = DateTimeOffset.Now.AddMinutes(30);
            }

            // 3. Skapa Identitet, Principal och Authentication Properties
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                // Denna är kritisk: Cookien går ut EXAKT samtidigt som JWT:n.
                ExpiresUtc = expirationDate
            };

            // 4. Skapa Cookie-sessionen
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties);
        }




















        //{
        //    // 1. Extrahera claims från JWT
        //    var handler = new JwtSecurityTokenHandler();
        //    var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;
        //    var claims = jsonToken.Claims;

        //    // 2. Skapa en Identity från claims (inklusive roll)
        //    var identity = new ClaimsIdentity(claims, "Cookies"); // Använder "Cookies" som autentiseringstyp
        //    var principal = new ClaimsPrincipal(identity);

        //    // Hämta utgångsdatum för token för att sätta giltighetstiden på cookien
        //    var expirationClaim = claims.FirstOrDefault(c => c.Type == "exp");
        //    DateTimeOffset expires = DateTimeOffset.Now.AddMinutes(30); // Fallback

        //    if (expirationClaim != null && long.TryParse(expirationClaim.Value, out long seconds))
        //    {
        //        // Konvertera Unix-tidstämpel till DateTimeOffset
        //        expires = DateTimeOffset.FromUnixTimeSeconds(seconds);
        //    }

        //    // 3. Spara Sessionen (Cookies)
        //    await HttpContext.SignInAsync(
        //        CookieAuthenticationDefaults.AuthenticationScheme,
        //        principal,
        //        new AuthenticationProperties
        //        {
        //            IsPersistent = false, // Session cookie (raderas vid stängning)
        //            IssuedUtc = DateTimeOffset.UtcNow,
        //            ExpiresUtc = expires // JWT:ns giltighetstid styr cookiens giltighet
        //        });

        //    // 4. (Valfritt men rekommenderat): Spara själva JWT:n i session/cookie
        //    // Detta behövs om MVC-klienten ska anropa skyddade API-endpoints
        //    HttpContext.Session.SetString("JWToken", jwtToken);
        //}
    }
}
