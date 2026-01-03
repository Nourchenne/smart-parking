using System;
using System.Linq;
using System.Threading.Tasks;
using auth.Data;
using auth.ViewModels.SuperAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace auth.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;

        public SuperAdminController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _db = db;
        }

        // LOGIN actions remain anonymous (keeps existing behavior)
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Dashboard");

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError(string.Empty, "Email et mot de passe requis.");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                ModelState.AddModelError(string.Empty, "Accès réservé au SuperAdmin.");
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(email, password, false, lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Email ou mot de passe incorrect.");
                return View();
            }

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        // =========================
        // DASHBOARD
        // =========================
        public async Task<IActionResult> Dashboard()
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var managersRole = "Manager";
            var totalManagers = await (from u in _userManager.Users
                                       join ur in _db.UserRoles on u.Id equals ur.UserId
                                       join r in _db.Roles on ur.RoleId equals r.Id
                                       where r.Name == managersRole
                                       select u).CountAsync();

            var totalParkings = await _db.Parkings.CountAsync();
            var totalReservations = await _db.Reservations.CountAsync();

            // list users with roles and lockout
            var users = await _userManager.Users.Select(u => new UserSummaryViewModel
            {
                Id = u.Id,
                Email = u.Email,
                LockoutEnd = u.LockoutEnd
            }).ToListAsync();

            // populate roles for each user
            foreach (var usr in users)
            {
                var user = await _userManager.FindByIdAsync(usr.Id);
                var roles = await _userManager.GetRolesAsync(user);
                usr.Roles = roles.ToArray();
                usr.IsLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
            }

            var vm = new DashboardViewModel
            {
                TotalUsers = totalUsers,
                TotalManagers = totalManagers,
                TotalParkings = totalParkings,
                TotalReservations = totalReservations,
                Users = users
            };

            return View(vm);
        }

        // =========================
        // USERS MANAGEMENT
        // =========================
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.Select(u => new UserSummaryViewModel
            {
                Id = u.Id,
                Email = u.Email,
                LockoutEnd = u.LockoutEnd
            }).ToListAsync();

            foreach (var usr in users)
            {
                var user = await _userManager.FindByIdAsync(usr.Id);
                usr.Roles = (await _userManager.GetRolesAsync(user)).ToArray();
                usr.IsLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
            }

            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteToRole(string userId, string role)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
                return RedirectToAction("Users");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return RedirectToAction("Users");

            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));

            if (!await _userManager.IsInRoleAsync(user, role))
                await _userManager.AddToRoleAsync(user, role);

            TempData["SuccessMessage"] = $"Rôle '{role}' attribué à {user.Email}.";
            return RedirectToAction("Users");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRole(string userId, string role)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
                return RedirectToAction("Users");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return RedirectToAction("Users");

            if (await _userManager.IsInRoleAsync(user, role))
                await _userManager.RemoveFromRoleAsync(user, role);

            TempData["SuccessMessage"] = $"Rôle '{role}' retiré à {user.Email}.";
            return RedirectToAction("Users");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Users");
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return RedirectToAction("Users");

            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
            {
                // unlock
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow);
                TempData["SuccessMessage"] = $"Compte {user.Email} déverrouillé.";
            }
            else
            {
                // lock for long time
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
                TempData["SuccessMessage"] = $"Compte {user.Email} verrouillé.";
            }

            return RedirectToAction("Users");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Users");
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return RedirectToAction("Users");

            // Prevent deleting self
            if (user.UserName == User.Identity?.Name)
            {
                TempData["ErrorMessage"] = "Vous ne pouvez pas supprimer votre propre compte.";
                return RedirectToAction("Users");
            }

            await _userManager.DeleteAsync(user);
            TempData["SuccessMessage"] = "Utilisateur supprimé.";
            return RedirectToAction("Users");
        }

        // =========================
        // PARKINGS OVERVIEW
        // =========================
        public async Task<IActionResult> Parkings()
        {
            var parkings = await _db.Parkings
                .Include(p => p.Spots)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var vm = parkings.Select(p => new ParkingSummaryViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Address = p.Address,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                OwnerId = p.OwnerId
            }).ToList();

            // resolve owner email
            foreach (var item in vm)
            {
                var owner = await _userManager.FindByIdAsync(item.OwnerId);
                item.OwnerEmail = owner?.Email ?? "-";
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleParkingActive(int id)
        {
            var parking = await _db.Parkings.FindAsync(id);
            if (parking == null) return RedirectToAction("Parkings");

            parking.IsActive = !parking.IsActive;
            _db.Parkings.Update(parking);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "État du parking mis à jour.";
            return RedirectToAction("Parkings");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteParking(int id)
        {
            var parking = await _db.Parkings.FindAsync(id);
            if (parking == null) return RedirectToAction("Parkings");

            _db.Parkings.Remove(parking);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Parking supprimé.";
            return RedirectToAction("Parkings");
        }

        // =========================
        // RESERVATIONS OVERVIEW
        // =========================
        public async Task<IActionResult> Reservations()
        {
            var list = await _db.Reservations
                .Include(r => r.Parking)
                .Include(r => r.ParkingSpot)
                .ToListAsync();

            var vm = list.Select(r => new ReservationSummaryViewModel
            {
                Id = r.Id,
                ParkingName = r.Parking?.Name,
                SpotCode = r.ParkingSpot?.SpotCode,
                UserId = r.UserId,
                StartAt = r.StartAt,
                EndAt = r.EndAt,
                DurationHours = r.DurationHours,
                TotalPrice = r.TotalPrice,
                Status = r.Status.ToString()
            }).ToList();

            // resolve user emails
            foreach (var item in vm)
            {
                var user = await _userManager.FindByIdAsync(item.UserId);
                item.UserEmail = user?.Email ?? "-";
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReservation(int id)
        {
            var r = await _db.Reservations.FindAsync(id);
            if (r == null) return RedirectToAction("Reservations");

            _db.Reservations.Remove(r);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Réservation supprimée.";
            return RedirectToAction("Reservations");
        }

        // =========================
        // CONTACT MESSAGES
        // =========================
        public async Task<IActionResult> ContactMessages()
        {
            var list = await _db.ContactMessages.OrderByDescending(c => c.CreatedAt).ToListAsync();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteContactMessage(int id)
        {
            var m = await _db.ContactMessages.FindAsync(id);
            if (m == null) return RedirectToAction("ContactMessages");

            _db.ContactMessages.Remove(m);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Message supprimé.";
            return RedirectToAction("ContactMessages");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleContactRead(int id)
        {
            var m = await _db.ContactMessages.FindAsync(id);
            if (m == null) return RedirectToAction("ContactMessages");

            m.IsRead = !m.IsRead;
            _db.ContactMessages.Update(m);
            await _db.SaveChangesAsync();
            return RedirectToAction("ContactMessages");
        }
    }
}
