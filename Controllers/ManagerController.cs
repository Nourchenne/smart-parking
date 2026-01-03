using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using auth.Data;
using auth.Models;
using auth.Services;
using auth.ViewModels.Manager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace auth.Controllers
{
    [Authorize(Roles = "Manager")]
    public class ManagerController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ManagerParkingService _service;
        private readonly ApplicationDbContext _db;

        public ManagerController(UserManager<IdentityUser> userManager, ManagerParkingService service, ApplicationDbContext db)
        {
            _userManager = userManager;
            _service = service;
            _db = db;
        }

        public async Task<IActionResult> Dashboard()
        {
            var ownerId = _userManager.GetUserId(User);
            var parkings = await _service.GetMyParkingsAsync(ownerId);
            return View(parkings);
        }

        [HttpGet]
        public IActionResult CreateParking()
        {
            var vm = new CreateParkingViewModel();
            vm.Spots.Add(new ParkingSpotViewModel());
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateParking(CreateParkingViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var ownerId = _userManager.GetUserId(User);

            // Parse lat/lng
            decimal lat = 0, lng = 0;
            if (!string.IsNullOrWhiteSpace(model.LatitudeString))
            {
                decimal.TryParse(model.LatitudeString.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out lat);
            }
            if (!string.IsNullOrWhiteSpace(model.LongitudeString))
            {
                decimal.TryParse(model.LongitudeString.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out lng);
            }

            var parking = new Parking
            {
                Name = model.Name,
                Address = model.Address,
                Latitude = lat,
                Longitude = lng,
                OwnerId = ownerId
            };

            // Create parking
            await _db.Parkings.AddAsync(parking);
            await _db.SaveChangesAsync();

            // Add spots, ensure uniqueness per parking
            var toAdd = new List<ParkingSpot>();
            foreach (var s in model.Spots.Where(x => x != null))
            {
                var code = (s.SpotCode ?? string.Empty).Trim();
                // Skip empty completely
                if (string.IsNullOrEmpty(code) && s.PricePerHour == 0 && string.IsNullOrEmpty(s.SpotType))
                    continue;

                var spot = new ParkingSpot
                {
                    ParkingId = parking.Id,
                    SpotCode = string.IsNullOrEmpty(code) ? Guid.NewGuid().ToString() : code,
                    PricePerHour = s.PricePerHour,
                    SpotType = s.SpotType
                };

                toAdd.Add(spot);
            }

            // Ensure unique SpotCode per parking: if duplicates, append suffix
            var existingCodes = new HashSet<string>(await _db.ParkingSpots.Where(sp => sp.ParkingId == parking.Id).Select(sp => sp.SpotCode).ToListAsync(), StringComparer.OrdinalIgnoreCase);
            foreach (var sp in toAdd)
            {
                var baseCode = string.IsNullOrEmpty(sp.SpotCode) ? "S" : sp.SpotCode;
                var code = baseCode;
                int i = 1;
                while (existingCodes.Contains(code))
                {
                    code = baseCode + "-" + i++;
                }
                sp.SpotCode = code;
                existingCodes.Add(code);
                _db.ParkingSpots.Add(sp);
            }

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Parking et places créés avec succès.";
            return RedirectToAction(nameof(Details), new { id = parking.Id });
        }

        [HttpGet]
        public async Task<IActionResult> EditParking(int id)
        {
            var ownerId = _userManager.GetUserId(User);
            var parking = await _db.Parkings.FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == ownerId);
            if (parking == null) return NotFound();

            var vm = new EditParkingViewModel
            {
                Id = parking.Id,
                Name = parking.Name,
                Address = parking.Address,
                LatitudeString = parking.Latitude.ToString("0.000000", CultureInfo.InvariantCulture),
                LongitudeString = parking.Longitude.ToString("0.000000", CultureInfo.InvariantCulture),
                IsActive = parking.IsActive
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditParking(EditParkingViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var ownerId = _userManager.GetUserId(User);
            var parking = await _db.Parkings.FirstOrDefaultAsync(p => p.Id == model.Id && p.OwnerId == ownerId);
            if (parking == null) return NotFound();

            decimal lat = 0, lng = 0;
            if (!string.IsNullOrWhiteSpace(model.LatitudeString))
                decimal.TryParse(model.LatitudeString.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out lat);
            if (!string.IsNullOrWhiteSpace(model.LongitudeString))
                decimal.TryParse(model.LongitudeString.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out lng);

            parking.Name = model.Name;
            parking.Address = model.Address;
            parking.Latitude = lat;
            parking.Longitude = lng;
            parking.IsActive = model.IsActive;

            _db.Parkings.Update(parking);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Parking mis à jour.";
            return RedirectToAction(nameof(Details), new { id = parking.Id });
        }

        public async Task<IActionResult> Details(int id, DateTime? day, string? spotCode)
        {
            var ownerId = _userManager.GetUserId(User);
            var parking = await _db.Parkings.Include(p => p.Spots).FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == ownerId);
            if (parking == null) return NotFound();

            // Load spots and reservations with optional filters
            var query = _db.Reservations.Include(r => r.ParkingSpot).Where(r => r.ParkingId == id);
            if (day.HasValue)
            {
                var start = day.Value.Date;
                var end = start.AddDays(1);
                query = query.Where(r => r.StartAt >= start && r.StartAt < end);
            }
            if (!string.IsNullOrWhiteSpace(spotCode))
            {
                query = query.Where(r => r.ParkingSpot.SpotCode.Contains(spotCode));
            }

            var reservations = await query.OrderByDescending(r => r.StartAt).ToListAsync();

            // resolve user emails
            var reservationsVm = new List<ManagerReservationViewModel>();
            foreach (var r in reservations)
            {
                var user = await _userManager.FindByIdAsync(r.UserId);
                reservationsVm.Add(new ManagerReservationViewModel
                {
                    Id = r.Id,
                    UserEmail = user?.Email ?? "-",
                    SpotCode = r.ParkingSpot?.SpotCode,
                    StartAt = r.StartAt,
                    EndAt = r.EndAt,
                    DurationHours = r.DurationHours,
                    TotalPrice = r.TotalPrice,
                    Status = r.Status.ToString()
                });
            }

            ViewBag.Reservations = reservationsVm;
            ViewBag.NewSpot = new ParkingSpotViewModel();
            ViewBag.Day = day?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd");
            ViewBag.SpotCode = spotCode ?? string.Empty;
            return View(parking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSpot(int parkingId, ParkingSpotViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Veuillez vérifier les champs de la place.";
                return RedirectToAction(nameof(Details), new { id = parkingId });
            }

            var ownerId = _userManager.GetUserId(User);
            var parking = await _db.Parkings.FirstOrDefaultAsync(p => p.Id == parkingId && p.OwnerId == ownerId);
            if (parking == null) return NotFound();

            // normalize
            var code = (vm.SpotCode ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(code)) code = Guid.NewGuid().ToString();

            var price = vm.PricePerHour;

            // ensure unique
            var existing = await _db.ParkingSpots.AnyAsync(s => s.ParkingId == parkingId && s.SpotCode == code);
            if (existing)
            {
                var baseCode = code;
                int i = 1;
                while (await _db.ParkingSpots.AnyAsync(s => s.ParkingId == parkingId && s.SpotCode == code))
                {
                    code = baseCode + "-" + i++;
                }
            }

            var spot = new ParkingSpot
            {
                ParkingId = parkingId,
                SpotCode = code,
                PricePerHour = price,
                SpotType = vm.SpotType,
                IsActive = true
            };

            _db.ParkingSpots.Add(spot);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Place ajoutée.";
            return RedirectToAction(nameof(Details), new { id = parkingId });
        }

        [HttpGet]
        public async Task<IActionResult> EditSpot(int id)
        {
            var spot = await _db.ParkingSpots.Include(s => s.Parking).FirstOrDefaultAsync(s => s.Id == id);
            if (spot == null) return NotFound();
            var ownerId = _userManager.GetUserId(User);
            if (spot.Parking.OwnerId != ownerId) return Forbid();

            var vm = new ParkingSpotViewModel
            {
                SpotCode = spot.SpotCode,
                PricePerHour = spot.PricePerHour,
                SpotType = spot.SpotType
            };
            ViewBag.ParkingId = spot.ParkingId;
            ViewBag.SpotId = spot.Id;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSpot(int spotId, ParkingSpotViewModel vm)
        {
            var spot = await _db.ParkingSpots.Include(s => s.Parking).FirstOrDefaultAsync(s => s.Id == spotId);
            if (spot == null) return NotFound();
            var ownerId = _userManager.GetUserId(User);
            if (spot.Parking.OwnerId != ownerId) return Forbid();

            var code = (vm.SpotCode ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(code)) code = Guid.NewGuid().ToString();

            // ensure unique
            var exists = await _db.ParkingSpots.AnyAsync(s => s.ParkingId == spot.ParkingId && s.SpotCode == code && s.Id != spot.Id);
            if (exists)
            {
                var baseCode = code; int i = 1;
                while (await _db.ParkingSpots.AnyAsync(s => s.ParkingId == spot.ParkingId && s.SpotCode == code && s.Id != spot.Id))
                {
                    code = baseCode + "-" + i++;
                }
            }

            spot.SpotCode = code;
            spot.PricePerHour = vm.PricePerHour;
            spot.SpotType = vm.SpotType;
            _db.ParkingSpots.Update(spot);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Place mise à jour.";
            return RedirectToAction(nameof(Details), new { id = spot.ParkingId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSpot(int spotId)
        {
            var spot = await _db.ParkingSpots.Include(s => s.Parking).FirstOrDefaultAsync(s => s.Id == spotId);
            if (spot == null) return NotFound();
            var ownerId = _userManager.GetUserId(User);
            if (spot.Parking.OwnerId != ownerId) return Forbid();

            _db.ParkingSpots.Remove(spot);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Place supprimée.";
            return RedirectToAction(nameof(Details), new { id = spot.ParkingId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSpotActive(int spotId)
        {
            var spot = await _db.ParkingSpots.Include(s => s.Parking).FirstOrDefaultAsync(s => s.Id == spotId);
            if (spot == null) return NotFound();
            var ownerId = _userManager.GetUserId(User);
            if (spot.Parking.OwnerId != ownerId) return Forbid();

            spot.IsActive = !spot.IsActive;
            _db.ParkingSpots.Update(spot);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "État de la place mis à jour.";
            return RedirectToAction(nameof(Details), new { id = spot.ParkingId });
        }

        public async Task<IActionResult> Reservations(int parkingId, DateTime? day, string? spotCode)
        {
            var ownerId = _userManager.GetUserId(User);
            var parking = await _db.Parkings.FirstOrDefaultAsync(p => p.Id == parkingId && p.OwnerId == ownerId);
            if (parking == null) return NotFound();

            var query = _db.Reservations.Include(r => r.ParkingSpot).Where(r => r.ParkingId == parkingId);
            if (day.HasValue)
            {
                var start = day.Value.Date;
                var end = start.AddDays(1);
                query = query.Where(r => r.StartAt >= start && r.StartAt < end);
            }
            if (!string.IsNullOrWhiteSpace(spotCode))
            {
                query = query.Where(r => r.ParkingSpot.SpotCode.Contains(spotCode));
            }

            var list = await query.OrderBy(r => r.StartAt).ToListAsync();
            var vm = new List<ManagerReservationViewModel>();
            foreach (var r in list)
            {
                var user = await _userManager.FindByIdAsync(r.UserId);
                vm.Add(new ManagerReservationViewModel
                {
                    Id = r.Id,
                    UserEmail = user?.Email ?? "-",
                    SpotCode = r.ParkingSpot?.SpotCode,
                    StartAt = r.StartAt,
                    EndAt = r.EndAt,
                    DurationHours = r.DurationHours,
                    TotalPrice = r.TotalPrice,
                    Status = r.Status.ToString()
                });
            }

            ViewBag.Parking = parking;
            ViewBag.Day = day?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd");
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteParking(int id)
        {
            var ownerId = _userManager.GetUserId(User);
            var parking = await _db.Parkings.Include(p => p.Spots).FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == ownerId);
            if (parking == null) return NotFound();

            // Remove reservations for parking (and payments) safely
            var reservations = await _db.Reservations.Where(r => r.ParkingId == id).ToListAsync();
            if (reservations.Any())
            {
                _db.Reservations.RemoveRange(reservations);
            }

            // Remove spots
            if (parking.Spots.Any())
            {
                _db.ParkingSpots.RemoveRange(parking.Spots);
            }

            _db.Parkings.Remove(parking);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Parking supprimé.";
            return RedirectToAction("Dashboard");
        }
    }
}
