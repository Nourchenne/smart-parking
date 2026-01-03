using System.Diagnostics;
using auth.Data;
using auth.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace auth.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        // =========================
        // HOME
        // =========================
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // =========================
        // PRIVACY
        // =========================
        [HttpGet]
        public IActionResult Privacy()
        {
            return View();
        }

        // =========================
        // ABOUT
        // =========================
        [HttpGet]
        public IActionResult About()
        {
            return View();
        }

        // =========================
        // CONTACT (GET)
        // =========================
        [HttpGet]
        public IActionResult Contact()
        {
            return View(new ContactViewModel());
        }

        // =========================
        // CONTACT (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var msg = new ContactMessage
            {
                FullName = model.FullName,
                Email = model.Email,
                Subject = model.Subject,
                Message = model.Message,
                CreatedAt = DateTime.UtcNow
            };

            _db.ContactMessages.Add(msg);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Message envoyé avec succès. Nous vous répondrons rapidement.";

            return RedirectToAction(nameof(Contact));
        }

        // =========================
        // CONTACTS LIST FOR ADMINS
        // =========================
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Manager")]
        public async Task<IActionResult> ContactMessages()
        {
            var list = await _db.ContactMessages.OrderByDescending(c => c.CreatedAt).ToListAsync();
            return View(list);
        }

        // =========================
        // ERROR
        // =========================
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
