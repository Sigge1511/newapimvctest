using assignment_mvc_carrental.Models;
using System.ComponentModel.DataAnnotations;

namespace assignment_mvc_carrental.ViewModels
{
    public class InputBookingViewModel
    {
        // ********* Fordon *********

        [Required(ErrorMessage = "Vehicle ID is required.")]
        public int VehicleId { get; set; }        

        public string? ApplicationUserId { get; set; }


        [Required(ErrorMessage = "Start date is required.")]
        public DateOnly StartDate { get; set; }

        [Required(ErrorMessage = "End date is required.")]
        public DateOnly EndDate { get; set; }

        public double TotalPrice { get; set; } = 0.0;


    }
}
