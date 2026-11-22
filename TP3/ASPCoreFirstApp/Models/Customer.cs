using System.ComponentModel.DataAnnotations;

namespace ASPCoreFirstApp.Models
{
    // LOCATION (RentalDbContext).
    // Customer n'appartient plus au catalogue : il n'y a aucun DbSet<Customer>
    // dans ApplicationDbContext, seulement dans RentalDbContext.
    public class Customer
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire.")]
        [StringLength(100, ErrorMessage = "Le nom ne doit pas dépasser 100 caractères.")]
        [Display(Name = "Nom")]
        public string Name { get; set; } = "";

        [EmailAddress(ErrorMessage = "L'adresse email n'est pas valide.")]
        public string? Email { get; set; }

        [Display(Name = "Type d'abonnement")]
        public int MembershipTypeId { get; set; }
        public MembershipType? MembershipType { get; set; }

        public ICollection<Rental> Rentals { get; set; } = new List<Rental>();
    }
}
