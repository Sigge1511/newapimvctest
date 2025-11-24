using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using api_carrental.Dtos;

namespace api_carrental.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUserDto>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<VehicleDto> VehicleSet { get; set; } = default!;
        public DbSet<BookingDto> BookingSet { get; set; } = default!;
        public DbSet<ApplicationUserDto> AppUserSet { get; set; } = default!;

    }
}
