using ASPCoreFirstApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ASPCoreFirstApp.Data
{
    // Partie avancée — deuxième contexte pour la LOCATION (Customer, Rental),
    // qui pointe sur une AUTRE base de données : RentalDb_TP2 (DefaultConnection2).
    //
    // Ce qu'on observe avec deux DbContext :
    // - chaque contexte a son propre modèle, ses propres migrations et sa propre
    //   table __EFMigrationsHistory ;
    // - toutes les commandes dotnet ef demandent --context, sinon :
    //   "More than one DbContext was found" ;
    // - on ne peut pas faire de jointure ni de clé étrangère entre les deux bases
    //   (Rental.MovieId n'est qu'un entier) ;
    // - un SaveChanges() sur un contexte n'enregistre rien dans l'autre :
    //   une opération sur les deux bases n'est plus atomique (voir TP3).
    public class RentalDbContext : DbContext
    {
        public RentalDbContext(DbContextOptions<RentalDbContext> options) : base(options) { }

        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Rental> Rentals => Set<Rental>();

        // Toutes les tables de ce contexte sont créées dans le schéma "rental".
        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.HasDefaultSchema("rental");
    }
}
