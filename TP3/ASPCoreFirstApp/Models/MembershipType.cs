using System.ComponentModel.DataAnnotations;

namespace ASPCoreFirstApp.Models
{
    // LOCATION (RentalDbContext)
    public class MembershipType
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string Name { get; set; } = "";

        public int DiscountPercent { get; set; }
    }
}
