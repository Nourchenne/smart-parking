using System.ComponentModel.DataAnnotations;

namespace auth.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est requis")]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Se souvenir de moi")]
        public bool RememberMe { get; set; }

        // Choix du type de compte : User ou Manager
        [Required(ErrorMessage = "Veuillez choisir le type de compte")]
        [Display(Name = "Type de compte")]
        [RegularExpression("User|Manager", ErrorMessage = "Type de compte invalide")]
        public string SelectedRole { get; set; } = "User";
    }
}
