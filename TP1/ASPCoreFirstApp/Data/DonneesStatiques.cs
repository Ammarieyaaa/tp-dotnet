using ASPCoreFirstApp.Models;

namespace ASPCoreFirstApp.Data
{
    // Pas encore de base de données au TP1 : on utilise des listes statiques en mémoire.
    // (La base de données arrive au TP2 avec Entity Framework Core.)
    public static class DonneesStatiques
    {
        public static readonly List<Movie> Movies = new()
        {
            new Movie { Id = 1, Name = "Inception" },
            new Movie { Id = 2, Name = "The Matrix" },
            new Movie { Id = 3, Name = "Interstellar" },
            new Movie { Id = 4, Name = "Blade Runner 2049" },
        };

        public static readonly List<Customer> Customers = new()
        {
            new Customer { Id = 1, Name = "Amal Ben Salah" },
            new Customer { Id = 2, Name = "Youssef Trabelsi" },
            new Customer { Id = 3, Name = "Sarra Gharbi" },
        };
    }
}
