namespace ASPCoreFirstApp.Models
{
    // Partie avancée — une location (base RentalDb_TP2).
    public class Rental
    {
        public int Id { get; set; }

        // MovieId : référence LOGIQUE vers un film qui est dans une AUTRE base (CinemaDb_TP2).
        // Pas de propriété de navigation ni de vraie clé étrangère possible entre deux bases.
        public int MovieId { get; set; }

        // CustomerId : vraie clé étrangère, Customer est dans la même base.
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public DateTime RentedAt { get; set; }
    }
}
