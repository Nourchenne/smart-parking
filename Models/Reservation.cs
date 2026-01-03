using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace auth.Models
{
    public enum ReservationStatus
    {
        Draft = 0,
        PendingPayment = 1,
        Confirmed = 2,
        Cancelled = 3
    }

    public class Reservation
    {
        public int Id { get; set; }

        // Lié à l'utilisateur connecté (Identity)
        [Required]
        public string UserId { get; set; } = string.Empty;

        // Lié à un parking (facultatif selon logique)
        [Required]
        public int ParkingId { get; set; }
        public Parking? Parking { get; set; }

        // Lié à une place (ParkingSpot)
        [Required]
        public int ParkingSpotId { get; set; }
        public ParkingSpot? ParkingSpot { get; set; }

        [Required]
        public DateTime StartAt { get; set; }

        [Required]
        public DateTime EndAt { get; set; }

        [Range(1, 24)]
        public int DurationHours { get; set; }

        [Range(0, 999999)]
        public decimal TotalPrice { get; set; }

        public ReservationStatus Status { get; set; } = ReservationStatus.PendingPayment;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // navigation optionnelle
        public Payment? Payment { get; set; }
    }
}
