using api_carrental.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace api_carrental.Repos
{
    public interface IBookingRepo
    {
        Task<IEnumerable<BookingDto>> GetAllBookingsAsync();
        Task<BookingDto?> GetBookingByIdAsync(int id);
        Task<BookingDto> AddBookingAsync(CreatingBookingDto booking);
        Task UpdateBookingAsync(BookingDto booking);
        Task DeleteBookingAsync(int id);
    }
}
