using ASPCoreFirstApp.Data;
using ASPCoreFirstApp.Models;
using ASPCoreFirstApp.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ASPCoreFirstApp.Controllers
{
    // Q5 — Afficher sur une seule page un film + les clients qui l'ont réservé.
    // URL : /MovieCustomer/Index/1  (ou /MovieCustomer → film 1 par défaut)
    public class MovieCustomerController : Controller
    {
        public IActionResult Index(int id = 1)
        {
            Movie? movie = DonneesStatiques.Movies.FirstOrDefault(m => m.Id == id);
            if (movie == null)
                return NotFound();

            // Pas encore de table "réservations" : on simule les clients ayant réservé ce film.
            var viewModel = new MovieCustomerViewModel
            {
                Movie = movie,
                Customers = DonneesStatiques.Customers
            };

            return View(viewModel);
        }
    }
}
