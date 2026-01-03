using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace auth.Services
{
    public class AuthService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IConfiguration _config;

        public AuthService(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<IdentityUser> signInManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _config = configuration;
        }

        // =========================
        // REGISTER (WRAPPERS)
        // =========================
        public Task<IdentityResult> RegisterSuperAdminAsync(string email, string password)
            => RegisterAsync(email, password, "SuperAdmin");

        public Task<IdentityResult> RegisterManagerAsync(string email, string password)
            => RegisterAsync(email, password, "Manager");

        public Task<IdentityResult> RegisterUserAsync(string email, string password)
            => RegisterAsync(email, password, "User");

        // =========================
        // REGISTER (CORE)
        // =========================
        private async Task<IdentityResult> RegisterAsync(string email, string password, string role)
        {
            // Sécurité : rôle autorisé seulement
            if (role != "SuperAdmin" && role != "Manager" && role != "User")
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "InvalidRole",
                    Description = "Rôle invalide."
                });
            }

            // Vérifier si l'utilisateur existe déjà
            var existing = await _userManager.FindByEmailAsync(email);
            if (existing != null)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "DuplicateEmail",
                    Description = "Un compte avec cet email existe déjà."
                });
            }

            // Créer le rôle si besoin
            if (!await _roleManager.RoleExistsAsync(role))
            {
                var roleCreate = await _roleManager.CreateAsync(new IdentityRole(role));
                if (!roleCreate.Succeeded) return roleCreate;
            }

            // Créer user
            var user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded) return result;

            // Ajouter au rôle
            var addRoleResult = await _userManager.AddToRoleAsync(user, role);
            return addRoleResult;
        }

        // =========================
        // LOGIN / LOGOUT
        // =========================
        public async Task<SignInResult> LoginAsync(string email, string password, bool rememberMe)
        {
            return await _signInManager.PasswordSignInAsync(
                userName: email,
                password: password,
                isPersistent: rememberMe,
                lockoutOnFailure: false
            );
        }

        public async Task LogoutAsync()
        {
            await _signInManager.SignOutAsync();
        }

        // =========================
        // ROLE CHECKS
        // =========================
        public async Task<bool> IsUserInRoleAsync(string email, string role)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return false;
            return await _userManager.IsInRoleAsync(user, role);
        }

        public Task<bool> IsUserSuperAdminAsync(string email) => IsUserInRoleAsync(email, "SuperAdmin");
        public Task<bool> IsUserManagerAsync(string email) => IsUserInRoleAsync(email, "Manager");
        public Task<bool> IsSimpleUserAsync(string email) => IsUserInRoleAsync(email, "User");

        // =========================
        // INIT ROLES + DEFAULT SUPERADMIN
        // =========================
        public async Task InitializeRolesAndAdminAsync()
        {
            // 1) Créer les rôles
            string[] roles = { "SuperAdmin", "Manager", "User" };

            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2) Créer SuperAdmin par défaut
            var superAdminEmail = _config["SuperAdmin:Email"] ?? "superadmin@smartparking.com";
            var superAdminPassword = _config["SuperAdmin:Password"] ?? "SuperAdmin@123";

            var superAdminUser = await _userManager.FindByEmailAsync(superAdminEmail);
            if (superAdminUser == null)
            {
                superAdminUser = new IdentityUser
                {
                    UserName = superAdminEmail,
                    Email = superAdminEmail,
                    EmailConfirmed = true
                };

                var createResult = await _userManager.CreateAsync(superAdminUser, superAdminPassword);
                if (createResult.Succeeded)
                {
                    await _userManager.AddToRoleAsync(superAdminUser, "SuperAdmin");
                }
            }
            else
            {
                if (!await _userManager.IsInRoleAsync(superAdminUser, "SuperAdmin"))
                {
                    await _userManager.AddToRoleAsync(superAdminUser, "SuperAdmin");
                }
            }
        }
    }
}
