using auth.Data;
using auth.Models;
using auth.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace auth.Controllers
{
    [Authorize(Roles = "User")]
    public class ReservationsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public ReservationsController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // GET: /Reservations/Details/5  (reservation details)
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);

            var reservation = await _db.Reservations
                .Include(r => r.ParkingSpot)
                .ThenInclude(s => s!.Parking)
                .Include(r => r.Payment)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (reservation == null) return NotFound();

            return View(reservation);
        }

        // GET: /Reservations/MyReservations
        [HttpGet]
        public async Task<IActionResult> MyReservations()
        {
            var userId = _userManager.GetUserId(User);
            var list = await _db.Reservations
                .Include(r => r.Parking)
                .Include(r => r.ParkingSpot)
                .Include(r => r.Payment)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.StartAt)
                .ToListAsync();

            return View(list);
        }

        // GET: /Reservations/Create?parkingId=1&startAt=2026-01-02T10:00&durationHours=2
        [HttpGet]
        public async Task<IActionResult> Create(int parkingId, DateTime? startAt, int? durationHours)
        {
            var parking = await _db.Parkings
                .Include(p => p.Spots)
                .FirstOrDefaultAsync(p => p.Id == parkingId && p.IsActive);

            if (parking == null) return NotFound();

            // Use provided values or defaults
            var start = startAt ?? DateTime.Now;
            var duration = durationHours ?? 1;

            var vm = new ReservationCreateViewModel
            {
                ParkingId = parkingId,
                StartAt = start,
                DurationHours = duration
            };

            // Determine unavailable spots for the requested interval
            var newStart = vm.StartAt;
            var newEnd = newStart.AddHours(vm.DurationHours);

            var conflicts = await _db.Reservations
                .Where(r => r.ParkingId == parkingId && r.StartAt < newEnd && r.EndAt > newStart)
                .Select(r => r.ParkingSpotId)
                .ToListAsync();

            var unavailable = new HashSet<int>(conflicts);

            ViewBag.Parking = parking;
            ViewBag.UnavailableSpots = unavailable;
            ViewBag.CanSelectSpot = true;
            ViewBag.StartAtQuery = vm.StartAt.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.DurationQuery = vm.DurationHours;

            return View(vm);
        }

        // POST: /Reservations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReservationCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var parking = await _db.Parkings.Include(p => p.Spots).FirstOrDefaultAsync(p => p.Id == model.ParkingId);
                ViewBag.Parking = parking;
                ViewBag.UnavailableSpots = new HashSet<int>();
                ViewBag.CanSelectSpot = true;
                return View(model);
            }

            // Validate spot exists and is active
            var spot = await _db.ParkingSpots.Include(s => s.Parking).FirstOrDefaultAsync(s => s.Id == model.ParkingSpotId && s.ParkingId == model.ParkingId && s.IsActive);
            if (spot == null)
            {
                ModelState.AddModelError(string.Empty, "Place invalide.");
                var parking = await _db.Parkings.Include(p => p.Spots).FirstOrDefaultAsync(p => p.Id == model.ParkingId);
                ViewBag.Parking = parking;
                ViewBag.UnavailableSpots = new HashSet<int>();
                ViewBag.CanSelectSpot = true;
                return View(model);
            }

            // Re-check availability to prevent race condition
            var newStart = model.StartAt;
            var newEnd = newStart.AddHours(model.DurationHours);
            var overlap = await _db.Reservations.AnyAsync(r => r.ParkingSpotId == model.ParkingSpotId && r.StartAt < newEnd && r.EndAt > newStart);
            if (overlap)
            {
                ModelState.AddModelError(string.Empty, "La place est déjà réservée pour l'intervalle choisi. Veuillez choisir une autre place ou modifier l'horaire.");
                var parking = await _db.Parkings.Include(p => p.Spots).FirstOrDefaultAsync(p => p.Id == model.ParkingId);
                // compute unavailable for view
                var conflicts = await _db.Reservations
                    .Where(r => r.ParkingId == model.ParkingId && r.StartAt < newEnd && r.EndAt > newStart)
                    .Select(r => r.ParkingSpotId)
                    .ToListAsync();
                ViewBag.Parking = parking;
                ViewBag.UnavailableSpots = new HashSet<int>(conflicts);
                ViewBag.CanSelectSpot = true;
                return View(model);
            }

            // Compute end time and price
            var endAt = model.StartAt.AddHours(model.DurationHours);
            var totalPrice = spot.PricePerHour * model.DurationHours;

            var userId = _userManager.GetUserId(User);

            var reservation = new Reservation
            {
                ParkingId = model.ParkingId,
                ParkingSpotId = model.ParkingSpotId,
                UserId = userId ?? string.Empty,
                StartAt = model.StartAt,
                EndAt = endAt,
                DurationHours = model.DurationHours,
                TotalPrice = totalPrice,
                Status = ReservationStatus.PendingPayment
            };

            await _db.Reservations.AddAsync(reservation);
            await _db.SaveChangesAsync();

            // Redirect to fake payment
            return RedirectToAction("Pay", "Payment", new { reservationId = reservation.Id });
        }
    }
}
