using ASPCoreFirstApp.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ASPCoreFirstApp.ViewModels
{
    // 5 — Une seule vue, trois sources de données :
    //   Movie (SQL Server, catalogue) + Customers (SQL Server, location) + Reviews (MongoDB)
    public class MovieDetailsViewModel
    {
        public Movie Movie { get; set; } = new Movie();

        public List<Review> Reviews { get; set; } = new();
        public double? AverageRating { get; set; }

        // false si le serveur MongoDB est injoignable (la page s'affiche quand même)
        public bool ReviewsAvailable { get; set; } = true;

        // Pour la liste déroulante du bouton "Louer"
        public List<SelectListItem> Customers { get; set; } = new();
    }
}
