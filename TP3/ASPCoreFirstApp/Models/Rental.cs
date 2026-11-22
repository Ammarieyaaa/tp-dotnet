namespace ASPCoreFirstApp.Models
{
    // LOCATION (RentalDbContext)
    public class Rental
    {
        public int Id { get; set; }

        public int MovieId { get; set; }   // référence LOGIQUE vers Movie (autre base) : pas de navigation

        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public DateTime RentedAt { get; set; }
    }
}
