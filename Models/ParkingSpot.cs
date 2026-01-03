using System.ComponentModel.DataAnnotations;

namespace auth.Models
{
    public class ParkingSpot
    {
        public int Id { get; set; }

        [Required]
        public int ParkingId { get; set; }
        public Parking? Parking { get; set; }

        // Exemple : "A01", "B12"
        [Required, MaxLength(20)]
        public string SpotCode { get; set; } = string.Empty;

        // Type de place (ex: "Normal", "Handicap", "EV")
        [MaxLength(30)]
        public string? SpotType { get; set; } = string.Empty;

        // Prix / heure
        [Range(0, 999999)]
        public decimal PricePerHour { get; set; }

        public bool IsActive { get; set; } = true;

        // navigation
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
