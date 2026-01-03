using System;
using System.ComponentModel.DataAnnotations;

namespace auth.ViewModels
{
    public class ReservationCreateViewModel
    {
        [Required]
        public int ParkingId { get; set; }

        [Required]
        public int ParkingSpotId { get; set; }

        [Required]
        public DateTime StartAt { get; set; } = DateTime.Now;

        [Range(1, 24)]
        public int DurationHours { get; set; } = 1;
    }
}
