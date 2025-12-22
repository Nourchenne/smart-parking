using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using auth.Services;

namespace auth.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthService _authService;
        private readonly UserManager<IdentityUser> _userManager;

        public AccountController(
            AuthService authService,
            UserManager<IdentityUser> userManager)
        {
            _authService = authService;
            _userManager = userManager;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string email, string password, string confirmPassword)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError(string.Empty, "Email et mot de passe sont requis");
                return View();
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Les mots de passe ne correspondent pas");
                return View();
            }

            var result = await _authService.RegisterAdminAsync(email, password);

            if (result.Succeeded)
            {
                await _authService.LoginAsync(email, password, false);
                TempData["SuccessMessage"] = "Compte administrateur créé avec succès!";
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe, string? returnUrl = null)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError(string.Empty, "Email et mot de passe sont requis");
                return View();
            }

            var result = await _authService.LoginAsync(email, password, rememberMe);

            if (result.Succeeded)
            {
                // Vérifier si l'utilisateur est admin
                var isAdmin = await _authService.IsUserAdminAsync(email);

                if (!isAdmin)
                {
                    await _authService.LogoutAsync();
                    ModelState.AddModelError(string.Empty, "Accès réservé aux administrateurs");
                    return View();
                }

                TempData["SuccessMessage"] = "Connexion réussie!";
                return LocalRedirect(returnUrl ?? "/");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Compte bloqué. Veuillez réessayer plus tard.");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Email ou mot de passe incorrect.");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync();
            TempData["SuccessMessage"] = "Déconnexion réussie!";
            return RedirectToAction("Index", "Home");
        }
    }
}