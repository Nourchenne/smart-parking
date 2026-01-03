using auth.Data;
using auth.Models;
using Microsoft.EntityFrameworkCore;

namespace auth.Services
{
    public class UserParkingService
    {
        private readonly ApplicationDbContext _db;

        public UserParkingService(ApplicationDbContext db)
        {
            _db = db;
        }

        // Recherche unique: parking name OR spot code/type (et optionnellement address)
        public async Task<List<Parking>> SearchParkingsAsync(string? q)
        {
            q = (q ?? string.Empty).Trim();

            var query = _db.Parkings
                .AsNoTracking()
                .Include(p => p.Spots)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(p =>
                    p.Name.Contains(q) ||
                    p.Address.Contains(q) ||   // <-- si tu ne veux pas chercher par address, supprime cette ligne
                    p.Spots.Any(s =>
                        s.IsActive &&
                        (
                            s.SpotCode.Contains(q) ||
                            (s.SpotType != null && s.SpotType.Contains(q))
                        )
                    )
                );
            }

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
    }
}
