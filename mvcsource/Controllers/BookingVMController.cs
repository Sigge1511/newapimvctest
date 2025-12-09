using assignment_mvc_carrental.Models;
using assignment_mvc_carrental.ViewModels;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Json;

namespace assignment_mvc_carrental.Controllers
{
    public class BookingVMController : Controller
    {
        private readonly IHttpClientFactory _clientfactory;
        private readonly IMapper _mapper;

        //För göra risken mindre för strul med deserialisering
        private readonly JsonSerializerOptions _jsonOptions = 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public BookingVMController(IMapper mapper, IHttpClientFactory httpClientFactory)
        {
            _clientfactory = httpClientFactory;
            _mapper = mapper;
        }
  //***********************************************************************************************************************
          // GET: BookingVM
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {            
            List<Booking> apiBookingList = null;

            // Anropar API:ets [HttpGet] på api/BookingDt
            // Använder GetFromJsonAsync för snabb deserialisering
            var _client = _clientfactory.CreateClient("CarRentalApi");
            var response = await _client.GetAsync("BookingDt");

            if (response.IsSuccessStatusCode)
            {
                apiBookingList = await response.Content.ReadFromJsonAsync<List<Booking>>(_jsonOptions);
            }
            
            if (apiBookingList != null)
            {
                var bookingVMList = _mapper.Map<List<BookingViewModel>>(apiBookingList);
                return View(bookingVMList);
            }

            // Om det misslyckas skickas en tom lista
            return View(new List<BookingViewModel>());
        }

//***********************************************************************************************************************
        //NÄR EN ANVÄNDARE SKAPAR EN NY BOKNING 
        // GET: BookingVM/Create
        [Authorize]
        public async Task<IActionResult> Create(int? vehicleId)
        {
            try 
            {
                // Deklarera det som skickas till vyn (BookingViewModel)
                BookingViewModel? viewModel = new BookingViewModel { VehicleId = vehicleId ?? 0 }; 
                List<Vehicle>? vehiclesList = null;

                // Anropar API GET för fordon. Rutt: "VehicleDt"
                var _client = _clientfactory.CreateClient("CarRentalApi");
                var response = await _client.GetAsync("api/VehicleDt");

                if (response.IsSuccessStatusCode)
                {
                    vehiclesList = await response.Content.ReadFromJsonAsync<List<Vehicle>>(_jsonOptions);

                    if (vehiclesList != null && vehiclesList.Count > 0)
                    {
                        // Mappa och returnera vyn 
                        var vehicleVMList = _mapper.Map<List<VehicleViewModel>>(vehiclesList);
                        ViewBag.VehicleList = vehicleVMList;
                        ViewBag.SelectedVehicleId = vehicleId;
                        return View(viewModel); 
                    }
                }         
                
                if(vehiclesList == null || vehiclesList.Count == 0)
                {
                    TempData["ErrorMessage"] = "Could not get the right vehicle.";
                    return RedirectToAction("Index", "Vehicle");
                }
                TempData["ErrorMessage"] = "Could not get the right vehicle.";
                return RedirectToAction("Index", "Vehicle");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred. Please try again";
                return RedirectToAction("Index", "Vehicle");
            }
        }

        // POST: BookingVM/Create
        [Authorize]
        [ActionName("Create")]
        [HttpPost]
        public async Task<IActionResult> CreateReservation(BookingViewModel inputBookingVM)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (inputBookingVM.StartDate < today)
            {
                ModelState.AddModelError(nameof(inputBookingVM.StartDate), 
                    "Start date cannot be in the past.");
            }
            var days = (inputBookingVM.EndDate.DayNumber - inputBookingVM.StartDate.DayNumber) + 1;
            if (days < 1)
            {
                ModelState.AddModelError(nameof(inputBookingVM.EndDate), 
                    "End date must be the same or after the start date.");
            }
            if (!ModelState.IsValid)
            {
                // Vi måste hämta fordonslistan från API:et igen för att fylla ViewBags.
                return await HandleFailedPost(inputBookingVM, 
                    "Something went wrong. Please check your input.");
            }            

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    TempData["ErrorMessage"] = "Unauthorized: User ID not found.";
                    return Unauthorized();
                }

                // Mappa från ViewModel till modellklass
                var bookingToSend = _mapper.Map<Booking>(inputBookingVM);
                bookingToSend.ApplicationUserId = userId;

                var accessToken = FetchAccessTokenInfo();
                var _client = _clientfactory.CreateClient("CarRentalApi");
                //Se till att skicka med authinfo i token
                _client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers
                        .AuthenticationHeaderValue("Bearer", accessToken); 
                
                var response = await _client.PostAsJsonAsync("api/BookingDt", bookingToSend);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Reservation created successfully!";
                    return RedirectToAction("Index", "VehicleVM");
                }
                                
                // ANNARS: Läs ut felmeddelandet från API:et för att visa för användaren
                string apiErrorContent = await response.Content.ReadAsStringAsync();
                string errorMessage = $"Booking failed (Status: {response.StatusCode}). API details: {apiErrorContent.Substring(0, Math.Min(apiErrorContent.Length, 150))}";

                // Vi måste ladda om ViewBags och returnera vyn med felmeddelandet.
                return await HandleFailedPost(inputBookingVM, errorMessage);
            }
            catch (Exception ex)
            {
                // 5. Hantering av Exception (Nätverksfel, Timeout)
                string errorMessage = "A critical error occurred while communicating with the booking service. Please try again.";
                // Lägg in detaljer i TempData (bra för debug, mindre för slutanvändare)
                TempData["DebugError"] = ex.Message;

                return await HandleFailedPost(inputBookingVM, errorMessage);
            }
        }

        // Hjälpmetod för att hantera att ladda om vyn vid fel
        private async Task<IActionResult> HandleFailedPost(BookingViewModel inputBookingVM, string errorMessage)
        {
            TempData["ErrorMessage"] = errorMessage;
            ModelState.AddModelError(string.Empty, errorMessage);

            // Hämta fordonslistan från API:et igen för att fylla ViewBags
            var _client = _clientfactory.CreateClient("CarRentalApi");
            List<Vehicle>? vehiclesList = await _client.GetFromJsonAsync<List<Vehicle>>("api/VehicleDt");

            if (vehiclesList != null)
            {
                var vehicleVMList = _mapper.Map<List<VehicleViewModel>>(vehiclesList);
                ViewBag.VehicleList = vehicleVMList;
                ViewBag.SelectedVehicleId = inputBookingVM.VehicleId;
            }
            else
            {
                // Om vi inte ens kan hämta fordonslistan, omdirigera till Vehicle Index.
                TempData["ErrorMessage"] = "Could not load. Please try again";
                return RedirectToAction("Index", "VehicleVM");
            }

            // Återgå till Create-vyn med felmeddelanden och inmatad data.
            return View(inputBookingVM);
        }

//***********************************************************************************************************************
        //NÄR EN ADMIN SKAPAR EN NY BOKNING 
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> AdminCreate()
        {
            // Den modell som skickas till vyn
            BookingViewModel bookingVM = new BookingViewModel();

            try
            {
                List<Vehicle>? vehiclesList = null;
                List<ApplicationUser>? customersList = null;
                var _client = _clientfactory.CreateClient("CarRentalApi");
                var vehiclesResponse = await _client.GetAsync("api/VehicleDt/GetIndexAsync");
                if (vehiclesResponse.IsSuccessStatusCode)
                {
                    vehiclesList = await vehiclesResponse.Content.ReadFromJsonAsync<List<Vehicle>>();
                }
                
                var customersResponse = await _client.GetAsync("AppUserDt/GetCustomersIndexAsync");
                if (customersResponse.IsSuccessStatusCode)
                {
                    customersList = await customersResponse.Content.ReadFromJsonAsync<List<ApplicationUser>>();
                }

                // Kolla att båda listorna har fyllts
                if (vehiclesList == null || vehiclesList.Count==0 || customersList == null)
                {
                    TempData["ErrorMessage"] = "Could not fetch required info. Please try again.";
                    // Omdirigera till fordonens index?
                    return RedirectToAction("Index", "Admin");
                }

                var vehicleVMList = _mapper.Map<List<VehicleViewModel>>(vehiclesList);
                ViewBag.VehicleList = vehicleVMList;
                var customerVMList = _mapper.Map<List<CustomerViewModel>>(customersList);
                ViewBag.CustomerList = customerVMList;

                return View("AdminCreate", bookingVM);
            }
            catch (Exception)
            {
                // Fånga nätverksfel eller oväntade undantag
                TempData["ErrorMessage"] = "An error occured. Please try again.";
                return RedirectToAction("Index", "Admin");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminCreate(BookingViewModel inputBookingVM)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (inputBookingVM.StartDate < today)
            {
                ModelState.AddModelError(nameof(inputBookingVM.StartDate), "Start date cannot be in the past.");
            }
            var days = (inputBookingVM.EndDate.DayNumber - inputBookingVM.StartDate.DayNumber) + 1;
            if (days < 1)
            {
                ModelState.AddModelError(nameof(inputBookingVM.EndDate), "End date must be the same as or after the start date.");
            }
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Please correct the input errors.");
                //Hämta fordon och kunder igen för ViewBags
                //för att kunna stanna i bokningsvyn och inte bli utkastad till index igen
                var _client = _clientfactory.CreateClient("CarRentalApi");
                List<Vehicle> vehiclesList = await _client.GetFromJsonAsync<List<Vehicle>>("VehicleDt/GetIndexAsync");
                List<ApplicationUser> customersList = await _client.GetFromJsonAsync<List<ApplicationUser>>("AppUserDt/GetCustomersIndexAsync");

                if (vehiclesList != null)
                {
                    ViewBag.VehicleList = _mapper.Map<List<VehicleViewModel>>(vehiclesList);
                }
                if (customersList != null)
                {
                    ViewBag.CustomerList = _mapper.Map<List<CustomerViewModel>>(customersList);
                }

                // Stanna i vyn för att visa felen
                return View("AdminCreate", inputBookingVM);
            }
            

            var bookingToSend = _mapper.Map<Booking>(inputBookingVM);
            try
            {
                var accessToken = FetchAccessTokenInfo();
                var _client = _clientfactory.CreateClient("CarRentalApi");
                _client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers
                        .AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _client.PostAsJsonAsync("api/BookingDt", bookingToSend);
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Booking successfully created.";
                    return RedirectToAction(nameof(Index));
                }

                TempData["ErrorMessage"] = "An error occured. Please try again.";
                return RedirectToAction("Index", "Admin");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occured. Please try again.";
                return RedirectToAction("Index", "Admin");
            }
        }

        //***********************************************************************************************************************

        // GET: BookingVM/Edit/5
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            try
            {
                Booking booking = null;
                var _client = _clientfactory.CreateClient("CarRentalApi");
                var bookingResponse = await _client.GetAsync($"BookingDt/{id.Value}");
                if (bookingResponse.IsSuccessStatusCode)
                {
                    booking = await bookingResponse.Content.ReadFromJsonAsync<Booking>();
                }
                if (booking == null)
                {
                    TempData["ErrorMessage"] = $"Booking with ID {id.Value} was not found.";
                    return NotFound();
                }
                var bookingVM = _mapper.Map<BookingViewModel>(booking);


                List<Vehicle> vehiclesList = null;
                var vehiclesResponse = await _client.GetAsync("VehicleDt/GetIndexAsync");

                if (vehiclesResponse.IsSuccessStatusCode)
                {
                    vehiclesList = await vehiclesResponse.Content.ReadFromJsonAsync<List<Vehicle>>();
                }
                if (vehiclesList == null || vehiclesList.Count == 0)
                {
                    TempData["ErrorMessage"] = "Could not retrieve the list of available vehicles.";
                    return RedirectToAction(nameof(Index)); 
                }

                var vehicleVMList = _mapper.Map<List<VehicleViewModel>>(vehiclesList);
                ViewBag.VehicleList = vehicleVMList;
                ViewBag.SelectedVehicleId = bookingVM.VehicleId;

                return View(bookingVM);
            }
            catch (Exception)
            {
                // Fånga fel
                TempData["ErrorMessage"] = "An error occurred. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: BookingVM/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, BookingViewModel bookingVM)
        {
            try
            {
                var _client = _clientfactory.CreateClient("CarRentalApi");
                if (id != bookingVM.Id)
                {
                    TempData["ErrorMessage"] = "Booking mismatch.";
                    return NotFound();
                }
                var today = DateOnly.FromDateTime(DateTime.Today);
                if (bookingVM.StartDate < today)
                {
                    ModelState.AddModelError(nameof(bookingVM.StartDate), "Start date cannot be in the past.");
                }
                var days = (bookingVM.EndDate.DayNumber - bookingVM.StartDate.DayNumber) + 1;
                if (days < 1)
                {
                    ModelState.AddModelError(nameof(bookingVM.EndDate), "End date must be the same as or after the start date.");
                }
                List<Vehicle>? vehiclesList = null;
                if (!ModelState.IsValid)
                {
                    ModelState.AddModelError(string.Empty, "Please correct the input errors.");

                    // Assign to the already declared nullable variable
                    vehiclesList = await _client.GetFromJsonAsync<List<Vehicle>>("VehicleDt/GetIndexAsync");

                    if (vehiclesList != null)
                    {
                        ViewBag.VehicleList = _mapper.Map<List<VehicleViewModel>>(vehiclesList);
                    }

                    ViewBag.SelectedVehicleId = bookingVM.VehicleId;
                    TempData["ErrorMessage"] = "Something went wrong. Please check your input.";
                    return View(bookingVM);
                }

                var bookingToSend = _mapper.Map<Booking>(bookingVM);
            
                var response = await _client.PutAsJsonAsync($"BookingDt/{id}", bookingToSend);
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Booking successfully updated!";
                    return RedirectToAction("Index", "VehicleVM");
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    TempData["ErrorMessage"] = "The booking to be updated was not found on the server.";
                    return NotFound();
                }

                // Läs ut felmeddelande från API:et för mer detaljer
                string apiErrorContent = await response.Content.ReadAsStringAsync();

                ModelState.AddModelError(string.Empty, $"Update failed. Details: {apiErrorContent.Substring(0, Math.Min(apiErrorContent.Length, 150))}");
                TempData["ErrorMessage"] = "Update failed. The vehicle might be unavailable during the new dates.";

                // Ladda om ViewBags igen vid API-fel:
                vehiclesList = await _client.GetFromJsonAsync<List<Vehicle>>("VehicleDt/GetIndexAsync");
                if (vehiclesList != null)
                {
                    ViewBag.VehicleList = _mapper.Map<List<VehicleViewModel>>(vehiclesList);
                }
                ViewBag.SelectedVehicleId = bookingVM.VehicleId;
                return View(bookingVM);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "A critical error occurred while communicating with the booking service.";
                return RedirectToAction(nameof(Index));
            }
        }

//***********************************************************************************************************************

        // GET: BookingVM/Delete/5
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                Booking booking = null;
                var _client = _clientfactory.CreateClient("CarRentalApi");
                var bookingResponse = await _client.GetAsync($"BookingDt/{id.Value}");
                if (bookingResponse.IsSuccessStatusCode)
                {
                    booking = await bookingResponse.Content.ReadFromJsonAsync<Booking>();
                }
                if (booking == null)
                {
                    TempData["ErrorMessage"] = $"The booking with ID {id.Value} was not found.";
                    return NotFound();
                }

                var bookingVM = _mapper.Map<BookingViewModel>(booking);
                return View(bookingVM);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "A critical error occurred while fetching the booking data for deletion.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: BookingVM/DeleteConfirmed/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<RedirectToActionResult> DeleteConfirmed(int id)
        {            
            try
            {
                var _client = _clientfactory.CreateClient("CarRentalApi");
                var response = await _client.DeleteAsync($"BookingDt/{id}");

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Reservation successfully deleted.";
                    return RedirectToAction("Index", "VehicleVM");
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    TempData["ErrorMessage"] = "The reservation was not found.";
                    return RedirectToAction(nameof(Delete), new { id });
                }
                else
                {
                    string apiErrorContent = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Could not delete reservation. " +
                        $"Server response: {response.StatusCode}. Details: {apiErrorContent}";
                    return RedirectToAction(nameof(Delete), new { id });
                }
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "A critical error occurred while communicating with the server.";
            }
            return RedirectToAction(nameof(Delete), new { id });
        }

//***************************************************************************************
        //Hjälpmetod för att hämta & skicka med tokeninfo
        internal string FetchAccessTokenInfo()
        {
            var accessToken = HttpContext.Session.GetString("AccessToken");
            return (accessToken);
        }
    }
}
