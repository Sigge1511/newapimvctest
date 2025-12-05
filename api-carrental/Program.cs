using api_carrental.Constants;
using api_carrental.Data;
using api_carrental.Dtos;
using api_carrental.Repos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text;


//Skapar denna här så jag kan ha en builder även i detta projekt/fil
IServiceCollection serviceCollection = new ServiceCollection();

//*********** KOPPLA TILL DATABASEN ******************************
var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

//*********** KOPPLA TILL IDENTITY ******************************
builder.Services.AddIdentity<ApplicationUserDto, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Denna rad instruerar serialiseraren att INTE använda camelCase
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });


//**************    MAPPER  **********************************************************************
builder.Services.AddAutoMapper(typeof(MappingProfile)); 
//mappar users vid inlogg och registrering

//*********** SETUP FÖR CORS *********************************************************************

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMVCAccess",
        policy =>
        {
            policy.WithOrigins("https://localhost:7090", "http://localhost:7090")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

//*********** BÖRJA SETUP MED JWT ******************************
var jwtSettings = new JwtSettings(); //Finns som egen klass i mappen "Constants"
//Binda inställningarna från appsettings.json till JwtSettings-klassen
builder.Configuration.GetSection("JwtSettings").Bind(jwtSettings);
//Även bra för att ha till DI senare, i tex AuthController
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
// Hämta säkerhetsnyckeln och konvertera den till det format som behövs (SymmetricSecurityKey)
var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,

        ValidateLifetime = true, //Vad ska jag ha här????

        ValidateIssuerSigningKey = true,
        IssuerSigningKey = securityKey
    };
});





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
    app.UseSwagger();
    app.UseSwaggerUI();


};
using (var scope = app.Services.CreateScope()) //skapa en admin och usermanager 
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUserDto>>();

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
app.UseCors("AllowMVCAccess");
app.UseAuthorization();
app.MapControllers();
app.Run();
