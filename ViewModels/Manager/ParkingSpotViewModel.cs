using System.ComponentModel.DataAnnotations;

namespace auth.ViewModels.Manager
{
    public class ParkingSpotViewModel
    {
        [StringLength(50)]
        public string? SpotCode { get; set; }

        public decimal PricePerHour { get; set; }

        [StringLength(30)]
        public string? SpotType { get; set; }
    }
}