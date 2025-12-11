using Microsoft.EntityFrameworkCore;
using api_carrental.Data;
using api_carrental.Dtos;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace api_carrental.Repos
{
    public class BookingRepo : IBookingRepo
    {
        private readonly ApplicationDbContext _context;        

        public BookingRepo(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BookingDto>> GetAllBookingsAsync()
        {
            return await _context.BookingSet.Include(b => b.Vehicle).Include(b => b.ApplicationUser).ToListAsync();
        }
        public async Task<BookingDto?> GetBookingByIdAsync(int id)
        {
            return await _context.BookingSet
                .Include(b => b.Vehicle)
                .Include(b => b.ApplicationUser) // hämta med info om bil och user med bokningen
                .FirstOrDefaultAsync(b => b.Id == id);
        }
        public async Task<BookingDto> AddBookingAsync(CreatingBookingDto bookingInput)
        {
            try
            {
                //bookingInput.ApplicationUser = null;
                //bookingInput.Vehicle = null;
                var getUser = await _context.AppUserSet.FindAsync(bookingInput.ApplicationUserId);
                var getVehicle = await _context.VehicleSet.FindAsync(bookingInput.VehicleId);

                var newBookingDto = new BookingDto
                {
                    ApplicationUserId = bookingInput.ApplicationUserId,
                    ApplicationUser = getUser,
                    VehicleId = bookingInput.VehicleId,
                    Vehicle = getVehicle,
                    StartDate = bookingInput.StartDate,
                    EndDate = bookingInput.EndDate,
                    TotalPrice = bookingInput.TotalPrice,
                };

                _context.BookingSet.Add(newBookingDto);
                await _context.SaveChangesAsync();
                return newBookingDto;
            }
            catch (Exception ex)
            {
                throw new Exception("Error adding reservation: " + ex.Message, ex);
            }
        }
        public async Task UpdateBookingAsync(BookingDto booking)
        {
            _context.BookingSet.Update(booking);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteBookingAsync(int id)
        {
            var booking = await _context.BookingSet.FindAsync(id);
            if (booking != null)
            {
                _context.BookingSet.Remove(booking);
                await _context.SaveChangesAsync();
            }
        }
    }

}
