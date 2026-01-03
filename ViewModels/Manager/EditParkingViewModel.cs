using System.ComponentModel.DataAnnotations;

namespace auth.ViewModels.Manager
{
    public class EditParkingViewModel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        public string? LatitudeString { get; set; }
        public string? LongitudeString { get; set; }

        public bool IsActive { get; set; }
    }
}