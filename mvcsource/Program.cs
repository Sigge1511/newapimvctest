using assignment_mvc_carrental.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace assignment_mvc_carrental
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddHttpClient("CarRentalApi", client =>
            {
                // ERSÄTT XXXX MED DITT API-PROJEKTS KORREKTA HTTPS-PORT
                // Titta i launchSettings.json i DITT API-PROJEKT (api_carrental) för att hitta porten.
                client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5064/");
            });

            ////hjälper till att skicka till inlogg om man vill hyra bil utan konto tex
            //builder.Services.ConfigureApplicationCookie(options =>
            //{
            //    options.LoginPath = "/Home/Login";
            //});

            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();



//**************    MAPPER  **********************************************************************

            builder.Services.AddAutoMapper(typeof(MappingProfile)); //mappning mellan klasser och VMs


//**************    SESSION, TOKENS OSV  **********************************************************************

            // 1. Lägg till Session State
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // Session timeout
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // 2. Lägg till Cookie Authentication
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Home/Login"; // Sökväg om användaren inte är autentiserad
                    options.ExpireTimeSpan = TimeSpan.FromHours(1); // Cookie-giltighet (kan overrideas i SignInAsync)
                    options.Events = new CookieAuthenticationEvents
                    {
                        OnRedirectToLogin = context =>
                        {
                            // Berättar för kund varför de måste vara
                            // inloggade för att kunna boka en bil
                            context.Response.Redirect(context.RedirectUri + "You need to be signed in when making a reservation.");
                            return Task.CompletedTask;
                        }
                    };
                });

            
            //*************************************************************************************************
            var app = builder.Build();


            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseRouting();
            app.UseHttpsRedirection();
            app.UseStaticFiles();            
            app.UseSession();
            app.UseAuthentication(); // kopplat till inlogg osv
            app.UseAuthorization(); //kollar behörighet

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            app.Run(); //LET'S GO! :)
        }
    }
}
