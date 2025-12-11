using System.ComponentModel.DataAnnotations;

namespace api_carrental.Dtos
{
    public class CreateNewUserDto
    {
        [Required(ErrorMessage = "First name is required.")]
        public string FirstName { get; set; }


        [Required(ErrorMessage = "Last name is required.")]
        public string LastName { get; set; }


        [Required(ErrorMessage = "Address is required.")]
        public string Address { get; set; }


        [Required(ErrorMessage = "City is required.")]
        public string City { get; set; }


        [Required(ErrorMessage = "Phonenumber is required")]
        public string PhoneNumber { get; set; }


        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; }

        public string UserName => Email; // Använd email som username


        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public List<BookingDto> BookingsList { get; set; } = new List<BookingDto>();
    }
}
