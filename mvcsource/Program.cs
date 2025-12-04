using assignment_mvc_carrental.Data;
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

            //hjälper till att skicka till inlogg om man vill hyra bil utan konto tex
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Home/Login";
            });

            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();



            //**************    MAPPER  **********************************************************************

            builder.Services.AddAutoMapper(typeof(MappingProfile)); //mappning mellan klasser och VMs



            //************ L�GG TILL ALLA REPOS H�R   *****************************************************************
            


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

            
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication(); // kopplat till inlogg osv

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            app.Run();
        }
    }
}
