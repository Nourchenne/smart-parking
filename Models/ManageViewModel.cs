using System.ComponentModel.DataAnnotations;

namespace auth.Models
{
    public class ManageViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public bool HasPassword { get; set; }
    }
}