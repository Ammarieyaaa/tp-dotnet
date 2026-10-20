using ASPCoreFirstApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ASPCoreFirstApp.Data
{
    // Contexte du CATALOGUE (Movie, Genre) → base CinemaDb_TP2.
    // C'est la passerelle entre les classes C# et la base de données.
    public class ApplicationDbContext : DbContext
    {
        // Version générique DbContextOptions<ApplicationDbContext> : indispensable dès qu'il y a
        // plusieurs contextes (ici aussi RentalDbContext), sinon l'injection ne sait pas
        // quelles options donner à quel contexte.
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Movie> Movies => Set<Movie>();
        public DbSet<Genre> Genres => Set<Genre>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Données initiales (SEED) → migration "SeedGenres"
            modelBuilder.Entity<Genre>().HasData(
                new Genre { Id = 1, Name = "Action" },
                new Genre { Id = 2, Name = "Drame" },
                new Genre { Id = 3, Name = "Comédie" });

            // Quelques films pour tester le tri et la pagination (5 par page) → migration "SeedMovies"
            // On ne renseigne que la clé étrangère GenreId, jamais la navigation Genre.
            modelBuilder.Entity<Movie>().HasData(
                new Movie { Id = 1, Name = "Inception",         GenreId = 1 },
                new Movie { Id = 2, Name = "The Matrix",        GenreId = 1 },
                new Movie { Id = 3, Name = "Interstellar",      GenreId = 2 },
                new Movie { Id = 4, Name = "Blade Runner 2049", GenreId = 1 },
                new Movie { Id = 5, Name = "Gladiator",         GenreId = 1 },
                new Movie { Id = 6, Name = "Le Fabuleux Destin d'Amélie Poulain", GenreId = 3 },
                new Movie { Id = 7, Name = "Les Intouchables",  GenreId = 3 },
                new Movie { Id = 8, Name = "Forrest Gump",      GenreId = 2 });
        }
    }
}
