using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using auth.Models;

namespace auth.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AccountController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // =========================
        // REGISTER
        // =========================
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Interdire tout rôle autre que User / Manager
            var selectedRole = (model.SelectedRole == "Manager") ? "Manager" : "User";

            // Créer user
            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, model.Password);

            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return View(model);
            }

            // Assigner rôle
            var roleResult = await _userManager.AddToRoleAsync(user, selectedRole);
            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return View(model);
            }

            // Auto-login après inscription
            await _signInManager.SignInAsync(user, isPersistent: false);

            TempData["SuccessMessage"] = $"Compte {selectedRole} créé avec succès.";

            // ✅ Redirection selon rôle
            if (selectedRole == "Manager")
                return RedirectToAction("Dashboard", "Manager");

            return RedirectToAction("Dashboard", "User");
        }

        // =========================
        // LOGIN
        // =========================
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Email ou mot de passe incorrect.");
                return View(model);
            }

            // Interdire SuperAdmin ici (il aura sa page séparée)
            if (await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                ModelState.AddModelError(string.Empty, "Veuillez utiliser la page de connexion SuperAdmin.");
                return View(model);
            }

            var selectedRole = (model.SelectedRole == "Manager") ? "Manager" : "User";

            // Vérifier que l'utilisateur a bien le rôle choisi
            if (!await _userManager.IsInRoleAsync(user, selectedRole))
            {
                ModelState.AddModelError(string.Empty, "Vous n'avez pas accès avec ce type de compte.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                userName: model.Email,
                password: model.Password,
                isPersistent: model.RememberMe,
                lockoutOnFailure: false
            );

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Connexion réussie !";

                // ✅ Redirection selon rôle
                if (selectedRole == "Manager")
                    return RedirectToAction("Dashboard", "Manager");

                return RedirectToAction("Dashboard", "User");
            }

            if (result.IsLockedOut)
                ModelState.AddModelError(string.Empty, "Compte bloqué. Veuillez réessayer plus tard.");
            else
                ModelState.AddModelError(string.Empty, "Email ou mot de passe incorrect.");

            return View(model);
        }

        // =========================
        // LOGOUT
        // =========================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["SuccessMessage"] = "Déconnexion réussie !";
            return RedirectToAction("Index", "Home");
        }

        // =========================
        // MANAGE
        // =========================
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Manage()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var model = new ManageViewModel
            {
                Email = user.Email ?? string.Empty,
                HasPassword = await _userManager.HasPasswordAsync(user)
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ManageViewModel model)
        {
            if (!ModelState.IsValid) return View("Manage", model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Update email if changed
            if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
            {
                var setEmail = await _userManager.SetEmailAsync(user, model.Email);
                var setUserName = await _userManager.SetUserNameAsync(user, model.Email);
                if (!setEmail.Succeeded || !setUserName.Succeeded)
                {
                    foreach (var e in setEmail.Errors.Concat(setUserName.Errors))
                        ModelState.AddModelError(string.Empty, e.Description);
                    return View("Manage", model);
                }
            }

            TempData["SuccessMessage"] = "Profil mis à jour.";
            return RedirectToAction("Manage");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View("Manage", new ManageViewModel { Email = (await _userManager.GetUserAsync(User))?.Email ?? string.Empty, HasPassword = true });

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (await _userManager.HasPasswordAsync(user))
            {
                var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                if (!result.Succeeded)
                {
                    foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
                    return View("Manage", new ManageViewModel { Email = user.Email ?? string.Empty, HasPassword = true });
                }
            }
            else
            {
                // no password set -> add one
                var add = await _userManager.AddPasswordAsync(user, model.NewPassword);
                if (!add.Succeeded)
                {
                    foreach (var e in add.Errors) ModelState.AddModelError(string.Empty, e.Description);
                    return View("Manage", new ManageViewModel { Email = user.Email ?? string.Empty, HasPassword = false });
                }
            }

            // refresh sign-in
            await _signInManager.RefreshSignInAsync(user);
            TempData["SuccessMessage"] = "Mot de passe mis à jour.";
            return RedirectToAction("Manage");
        }
    }
}
