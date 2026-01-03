using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace auth.Models
{
    public class Parking
    {
        public int Id { get; set; }

        [Required, StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string Address { get; set; } = string.Empty;

        // Owner = Manager (IdentityUser)
        [Required]
        public string OwnerId { get; set; } = string.Empty;

        // Navigation (optionnel mais recommandé)
        public IdentityUser? Owner { get; set; }

        // Localisation
        [Column(TypeName = "decimal(9,6)")]
        public decimal Latitude { get; set; }

        [Column(TypeName = "decimal(9,6)")]
        public decimal Longitude { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<ParkingSpot> Spots { get; set; } = new List<ParkingSpot>();
    }
}
