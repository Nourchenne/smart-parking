using auth.Data;
using auth.Models;
using Microsoft.EntityFrameworkCore;

namespace auth.Services
{
    public class ManagerParkingService
    {
        private readonly ApplicationDbContext _db;

        public ManagerParkingService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<Parking>> GetMyParkingsAsync(string ownerId)
        {
            return await _db.Parkings
                .Where(p => p.OwnerId == ownerId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Parking?> GetParkingAsync(int id, string ownerId)
        {
            return await _db.Parkings
                .Include(p => p.Spots.OrderBy(s => s.SpotCode))
                .FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == ownerId);
        }

        public async Task<int> CreateParkingAsync(Parking parking)
        {
            _db.Parkings.Add(parking);
            await _db.SaveChangesAsync();
            return parking.Id;
        }

        public async Task AddSpotAsync(string ownerId, int parkingId, ParkingSpot spot)
        {
            var parking = await _db.Parkings.FirstOrDefaultAsync(p => p.Id == parkingId && p.OwnerId == ownerId);
            if (parking == null) throw new InvalidOperationException("Parking introuvable ou accès refusé.");

            spot.ParkingId = parkingId;
            _db.ParkingSpots.Add(spot);
            await _db.SaveChangesAsync();
        }

        public async Task<List<Reservation>> GetReservationsForParkingAsync(string ownerId, int parkingId, DateTime day)
        {
            var exists = await _db.Parkings.AnyAsync(p => p.Id == parkingId && p.OwnerId == ownerId);
            if (!exists) return new List<Reservation>();

            var start = day.Date;
            var end = start.AddDays(1);

            return await _db.Reservations
                .Include(r => r.ParkingSpot)
                .Where(r => r.ParkingId == parkingId && r.StartAt >= start && r.StartAt < end)
                .OrderBy(r => r.StartAt)
                .ToListAsync();
        }
    }
}
