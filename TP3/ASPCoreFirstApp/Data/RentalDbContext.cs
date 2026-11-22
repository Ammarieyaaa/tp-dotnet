using ASPCoreFirstApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ASPCoreFirstApp.Data
{
    // Contexte de LOCATION (MembershipType, Customer, Rental) → base RentalDb_TP3, schéma "rental".
    // Migrations dans Migrations/Rental (--context RentalDbContext).
    public class RentalDbContext : DbContext
    {
        public RentalDbContext(DbContextOptions<RentalDbContext> options) : base(options) { }

        public DbSet<MembershipType> MembershipTypes => Set<MembershipType>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Rental> Rentals => Set<Rental>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.HasDefaultSchema("rental");

            // Customer → MembershipType (N–1)
            // Restrict : on ne peut pas supprimer un type d'abonnement encore utilisé par des clients.
            mb.Entity<Customer>()
                .HasOne(c => c.MembershipType)
                .WithMany()
                .HasForeignKey(c => c.MembershipTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Rental → Customer (N–1)
            // Restrict : l'historique des locations est conservé, on ne supprime pas
            // un client qui a des locations (Cascade effacerait cet historique).
            mb.Entity<Rental>()
                .HasOne(r => r.Customer)
                .WithMany(c => c.Rentals)
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Rental.MovieId : PAS de relation configurée, Movie est dans une autre base.
            // Un index accélère quand même la recherche des locations d'un film.
            mb.Entity<Rental>().HasIndex(r => r.MovieId);

            // ---------- SEED ----------
            SeedRental(mb);
        }

        // Chaque DbContext seed SES propres tables : les clients sont semés ici, pas dans le catalogue.
        private static void SeedRental(ModelBuilder mb)
        {
            mb.Entity<MembershipType>().HasData(
                new MembershipType { Id = 1, Name = "Standard", DiscountPercent = 0 },
                new MembershipType { Id = 2, Name = "Premium",  DiscountPercent = 10 },
                new MembershipType { Id = 3, Name = "Gold",     DiscountPercent = 20 });

            mb.Entity<Customer>().HasData(
                new Customer { Id = 1, Name = "Amal Ben Salah",   Email = "amal@example.com",   MembershipTypeId = 2 },
                new Customer { Id = 2, Name = "Youssef Trabelsi", Email = "youssef@example.com", MembershipTypeId = 1 });
        }
    }
}
