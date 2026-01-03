using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace auth.ViewModels.Manager
{
    public class CreateParkingViewModel
    {
        [Required]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Address { get; set; } = string.Empty;

        // Accept as string to avoid culture binding issues
        public string? LatitudeString { get; set; }
        public string? LongitudeString { get; set; }

        public List<ParkingSpotViewModel> Spots { get; set; } = new List<ParkingSpotViewModel>();
    }
}
