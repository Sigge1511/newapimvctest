using api_carrental.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.OpenApi;
using api_carrental.Dtos;
using api_carrental.Repos;
using AutoMapper;
using Swashbuckle.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerGen;


var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString)); // <-- Se till att denna rad finns!

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddControllers();
//***************** API STUFF *****************


builder.Services.AddScoped<IVehicleRepo, VehicleRepo>();
builder.Services.AddScoped<IBookingRepo, BookingRepo>();
builder.Services.AddScoped<IApplicationUser, ApplicationUserRepo>();






var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    //app.MapOpenApi();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();


};
using (var scope = app.Services.CreateScope()) //skapa en admin och usermanager 
{
    //var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    //var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    //string[] roles = { "Admin", "Customer" }; //fyller identitys userrole-tabell

    //foreach (var role in roles)
    //{
    //    if (!await roleManager.RoleExistsAsync(role))
    //        await roleManager.CreateAsync(new IdentityRole(role));
    //    //skapar nya om de inte finns i db redan
    //}

    // Skapa adminkonto 
    //var adminEmail = "sigge@site.com";
    //var adminUser = await userManager.FindByEmailAsync(adminEmail); //leta efter admin i db
    //if (adminUser == null)
    //{
    //    var newAdmin = new ApplicationUser { UserName = adminEmail, Email = adminEmail };
    //    //skapa en ny admin om den saknas
    //    var result = await userManager.CreateAsync(newAdmin, "Sally123!");

    //    if (result.Succeeded)
    //    {
    //        await userManager.AddToRoleAsync(newAdmin, "Admin");
    //    }
    //    else
    //    {
    //        // Logga ut fel
    //        foreach (var error in result.Errors)
    //        {
    //            Console.WriteLine($"Fel: {error.Code} - {error.Description}");
    //        }
    //    }
    //}
}
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
