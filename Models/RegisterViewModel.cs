using System.ComponentModel.DataAnnotations;

namespace auth.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est requis")]
        [StringLength(100, ErrorMessage = "Le mot de passe doit avoir au moins {2} caractères", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmation est requise")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmer le mot de passe")]
        [Compare("Password", ErrorMessage = "Les mots de passe ne correspondent pas")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "Nom complet")]
        [StringLength(100)]
        public string? FullName { get; set; }

        // Choix du type de compte : User ou Manager
        [Required(ErrorMessage = "Veuillez choisir le type de compte")]
        [Display(Name = "Type de compte")]
        [RegularExpression("User|Manager", ErrorMessage = "Type de compte invalide")]
        public string SelectedRole { get; set; } = "User";
    }
}
