using auth.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace auth.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // =========================
        // SMART PARKING
        // =========================
        public DbSet<Parking> Parkings { get; set; }
        public DbSet<ParkingSpot> ParkingSpots { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // =========================
            // PARKING
            // =========================
            builder.Entity<Parking>()
                .Property(p => p.Latitude)
                .HasColumnType("decimal(9,6)");

            builder.Entity<Parking>()
                .Property(p => p.Longitude)
                .HasColumnType("decimal(9,6)");

            // Parking -> Owner (IdentityUser)
            builder.Entity<Parking>()
                .HasOne<IdentityUser>(p => p.Owner)
                .WithMany()
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Parking>()
                .HasMany(p => p.Spots)
                .WithOne(s => s.Parking)
                .HasForeignKey(s => s.ParkingId)
                .OnDelete(DeleteBehavior.Cascade);

            // =========================
            // PARKING SPOT
            // =========================
            builder.Entity<ParkingSpot>()
                .Property(s => s.PricePerHour)
                .HasColumnType("decimal(10,2)");

            // Unicité du SpotCode par parking
            builder.Entity<ParkingSpot>()
                .HasIndex(s => new { s.ParkingId, s.SpotCode })
                .IsUnique();

            // =========================
            // RESERVATION
            // =========================
            // Use TotalPrice property from Reservation model
            builder.Entity<Reservation>()
                .Property(r => r.TotalPrice)
                .HasColumnType("decimal(10,2)");

            // Reservation -> ParkingSpot
            builder.Entity<Reservation>()
                .HasOne(r => r.ParkingSpot)
                .WithMany(s => s.Reservations)
                .HasForeignKey(r => r.ParkingSpotId)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================
            // PAYMENT
            // =========================
            builder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(10,2)");
            
            // One-to-one Reservation <-> Payment (optional navigation from Reservation)
            builder.Entity<Payment>()
                .HasOne(p => p.Reservation)
                .WithOne(r => r.Payment)
                .HasForeignKey<Payment>(p => p.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);

            // =========================
            // CONTACT
            // =========================
            builder.Entity<ContactMessage>()
                .Property(c => c.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
