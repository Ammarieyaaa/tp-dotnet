using System.Text.Json;
using ASPCoreFirstApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ASPCoreFirstApp.Data
{
    // Contexte du CATALOGUE (Movie, Genre) → base CinemaDb_TP3.
    // Migrations dans Migrations/Catalog (--context ApplicationDbContext).
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Movie> Movies => Set<Movie>();
        public DbSet<Genre> Genres => Set<Genre>();
        // Plus de DbSet<Customer> ici : Customer appartient au contexte de location.

        protected override void OnModelCreating(ModelBuilder mb)
        {
            // ---------- Relation 1–N Genre → Movies (API Fluent) ----------
            // Par CONVENTION, EF trouve déjà la relation tout seul (GenreId + Genre + Movies).
            // L'API Fluent permet de l'écrire explicitement et surtout de choisir le DeleteBehavior :
            //  - Cascade  : supprimer un genre supprimerait TOUS ses films → dangereux pour un catalogue.
            //  - SetNull  : impossible ici, GenreId n'est pas nullable.
            //  - Restrict : SQL Server refuse de supprimer un genre qui contient encore des films. ✔
            mb.Entity<Movie>()
                .HasOne(m => m.Genre)
                .WithMany(g => g.Movies)
                .HasForeignKey(m => m.GenreId)
                .OnDelete(DeleteBehavior.Restrict);

            mb.Entity<Genre>()
                .Property(g => g.Name)
                .HasMaxLength(50);

            // ---------- SEED ----------
            SeedCatalog(mb);
        }

        private static void SeedCatalog(ModelBuilder mb)
        {
            // 3.2 — Genres lus depuis Data/Seed/genres.json (copié dans bin/ grâce au .csproj).
            var path = Path.Combine(AppContext.BaseDirectory, "Data", "Seed", "genres.json");
            var genres = JsonSerializer.Deserialize<List<Genre>>(
                             File.ReadAllText(path),
                             new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                         ?? new List<Genre>();

            mb.Entity<Genre>().HasData(genres);

            // Guid STABLES (écrits en dur) : un Guid.NewGuid() ici changerait à chaque
            // exécution et EF croirait que les données ont changé à chaque migration.
            var action = Guid.Parse("84ca0bcd-082c-49cb-aa77-ea2f1f5f8285");
            var drame = Guid.Parse("79e6f638-d7e7-4f63-8365-f172cb925381");
            // Remarque : l'énoncé écrit "var Science-fiction = ..." → ce n'est pas un nom de
            // variable C# valide (le "-" est un opérateur). On l'appelle scienceFiction.
            var scienceFiction = Guid.Parse("3f2b8c4e-5a1d-4e7b-9c60-1d2e3f4a5b6c");

            // Clés primaires explicites + uniquement la clé étrangère (jamais la navigation Genre).
            mb.Entity<Movie>().HasData(
                new Movie { Id = 1, Name = "Inception",    GenreId = scienceFiction, IsAvailable = true },
                new Movie { Id = 2, Name = "Gladiator",    GenreId = action,         IsAvailable = true },
                new Movie { Id = 3, Name = "Interstellar", GenreId = scienceFiction, IsAvailable = true },
                new Movie { Id = 4, Name = "Forrest Gump", GenreId = drame,          IsAvailable = true });
        }
    }
}
