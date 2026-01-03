using System;
using System.ComponentModel.DataAnnotations;

namespace auth.ViewModels
{
    public class StripeCheckoutCreateViewModel
    {
        [Required] public int ParkingId { get; set; }
        [Required] public int ParkingSpotId { get; set; }

        [Required] public DateTime StartAt { get; set; }
        [Range(1, 24)] public int DurationHours { get; set; }

        // optionnel : pour retrouver la réservation après paiement
        public string? ClientReferenceId { get; set; }
    }
}
