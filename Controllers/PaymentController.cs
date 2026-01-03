using auth.Data;
using auth.Models;
using auth.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace auth.Controllers
{
    [Authorize(Roles = "User")] // Paiement réservé aux utilisateurs
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public PaymentController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // GET: /Payment/Pay?reservationId=1
        [HttpGet]
        public async Task<IActionResult> Pay(int reservationId)
        {
            var userId = _userManager.GetUserId(User);

            var reservation = await _db.Reservations
                .Include(r => r.Payment)
                .FirstOrDefaultAsync(r => r.Id == reservationId && r.UserId == userId);

            if (reservation == null) return NotFound();

            // Créer un payment si non existant
            var payment = reservation.Payment;
            if (payment == null)
            {
                payment = new Payment
                {
                    ReservationId = reservation.Id,
                    Amount = reservation.TotalPrice,
                    Currency = "EUR",
                    Status = PaymentStatus.Pending,
                    FakeTransactionRef = $"FAKE-{Guid.NewGuid():N}".Substring(0, 18)
                };
                _db.Payments.Add(payment);

                // forcer statut réservation
                reservation.Status = ReservationStatus.PendingPayment;

                await _db.SaveChangesAsync();
            }

            var vm = new FakePayViewModel
            {
                ReservationId = reservation.Id,
                Amount = payment.Amount,
                Currency = payment.Currency,
                FakeRef = payment.FakeTransactionRef ?? "",
                CurrentStatus = payment.Status.ToString()
            };

            return View(vm);
        }

        // POST: /Payment/Succeed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Succeed(int reservationId)
        {
            var userId = _userManager.GetUserId(User);

            var reservation = await _db.Reservations
                .Include(r => r.Payment)
                .FirstOrDefaultAsync(r => r.Id == reservationId && r.UserId == userId);

            if (reservation == null) return NotFound();
            if (reservation.Payment == null) return BadRequest("Payment non initialisé.");

            reservation.Payment.Status = PaymentStatus.Paid;
            reservation.Payment.PaidAt = DateTime.UtcNow;

            reservation.Status = ReservationStatus.Confirmed;

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Success), new { reservationId });
        }

        // POST: /Payment/Fail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Fail(int reservationId)
        {
            var userId = _userManager.GetUserId(User);

            var reservation = await _db.Reservations
                .Include(r => r.Payment)
                .FirstOrDefaultAsync(r => r.Id == reservationId && r.UserId == userId);

            if (reservation == null) return NotFound();
            if (reservation.Payment == null) return BadRequest("Payment non initialisé.");

            reservation.Payment.Status = PaymentStatus.Failed;

            // Choix : on laisse en PendingPayment pour pouvoir réessayer
            reservation.Status = ReservationStatus.PendingPayment;

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Failed), new { reservationId });
        }

        [HttpGet]
        public async Task<IActionResult> Success(int reservationId)
        {
            var userId = _userManager.GetUserId(User);
            var reservation = await _db.Reservations
                .Include(r => r.Parking)
                .Include(r => r.ParkingSpot)
                .Include(r => r.Payment)
                .FirstOrDefaultAsync(r => r.Id == reservationId && r.UserId == userId);

            if (reservation == null) return NotFound();

            var user = await _userManager.FindByIdAsync(reservation.UserId);

            var vm = new PaymentReceiptViewModel
            {
                ReservationId = reservation.Id,
                UserEmail = user?.Email ?? "-",
                ParkingName = reservation.Parking?.Name,
                SpotCode = reservation.ParkingSpot?.SpotCode,
                StartAt = reservation.StartAt,
                EndAt = reservation.EndAt,
                DurationHours = reservation.DurationHours,
                TotalPrice = reservation.TotalPrice,
                PaymentRef = reservation.Payment?.FakeTransactionRef,
                PaidAt = reservation.Payment?.PaidAt
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Failed(int reservationId)
        {
            return View(model: reservationId);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadReceipt(int reservationId)
        {
            var userId = _userManager.GetUserId(User);
            var reservation = await _db.Reservations
                .Include(r => r.Parking)
                .Include(r => r.ParkingSpot)
                .Include(r => r.Payment)
                .FirstOrDefaultAsync(r => r.Id == reservationId && r.UserId == userId);

            if (reservation == null) return NotFound();
            if (reservation.Payment == null || reservation.Payment.Status != PaymentStatus.Paid)
                return BadRequest("Receipt only available for paid reservations.");

            var user = await _userManager.FindByIdAsync(reservation.UserId);

            // Generate PDF using QuestPDF
            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A4);
                    page.Content().Column(col =>
                    {
                        col.Item().Text("Smart Parking").FontSize(20).Bold();
                        col.Item().Text($"Reçu de paiement - Réservation #{reservation.Id}").FontSize(14).SemiBold();

                        col.Item().LineHorizontal(1);

                        col.Item().Column(info =>
                        {
                            info.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Utilisateur: ").Bold();
                                r.RelativeItem().Text(user?.Email ?? "-");
                            });

                            info.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Parking: ").Bold();
                                r.RelativeItem().Text(reservation.Parking?.Name ?? "-");
                            });

                            info.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Place: ").Bold();
                                r.RelativeItem().Text(reservation.ParkingSpot?.SpotCode ?? "-");
                            });

                            info.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Début: ").Bold();
                                r.RelativeItem().Text(reservation.StartAt.ToString("g"));
                            });

                            info.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Fin: ").Bold();
                                r.RelativeItem().Text(reservation.EndAt.ToString("g"));
                            });

                            info.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Durée (h): ").Bold();
                                r.RelativeItem().Text(reservation.DurationHours.ToString());
                            });

                            info.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Montant: ").Bold();
                                r.RelativeItem().Text(reservation.TotalPrice.ToString("0.00") + " " + (reservation.Payment?.Currency ?? "EUR"));
                            });

                            info.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Réf paiement: ").Bold();
                                r.RelativeItem().Text(reservation.Payment?.FakeTransactionRef ?? "-");
                            });

                            info.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Payé le: ").Bold();
                                r.RelativeItem().Text(reservation.Payment?.PaidAt?.ToString("g") ?? "-");
                            });
                        });

                        col.Item().Text("Merci pour votre paiement.");
                    });
                });
            }).GeneratePdf();

            var fileName = $"receipt_{reservation.Id}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
    }
}
