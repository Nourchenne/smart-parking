using System.ComponentModel.DataAnnotations;

namespace auth.Models
{
    public class ContactViewModel
    {
        [Required(ErrorMessage = "Le nom est requis")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le sujet est requis")]
        [StringLength(120)]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le message est requis")]
        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;
    }
}
