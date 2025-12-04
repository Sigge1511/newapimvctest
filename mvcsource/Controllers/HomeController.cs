using assignment_mvc_carrental.Models;
using assignment_mvc_carrental.ViewModels;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.Diagnostics;

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
                var _client = _clientfactory.CreateClient();
                LoginModel loginUser = _mapper.Map<LoginModel>(loginUserVM);

                var result = await _client.PostAsJsonAsync("Auth", loginUser);

                if (result.IsSuccessStatusCode)
                {
                    _logger.LogInformation("User logged in.");
                    return LocalRedirect("Index");
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
    }
}
