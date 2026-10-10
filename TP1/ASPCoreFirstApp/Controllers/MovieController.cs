using ASPCoreFirstApp.Data;
using ASPCoreFirstApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace ASPCoreFirstApp.Controllers
{
    public class MovieController : Controller
    {
        // ------------------------------------------------------------------
        // Index — liste des films (Q7 : vue "Liste des Films")
        // Q4 : routage PAR ATTRIBUTS → /Movie et /Movie/Index
        // ------------------------------------------------------------------
        [Route("Movie")]
        [Route("Movie/Index")]
        public IActionResult Index()
        {
            // Version de l'énoncé (section III), gardée pour référence :
            //   Movie movie = new Movie { Id = 1, Name = "Movie 1" };
            //   List<Movie> movies = new List<Movie>() {
            //       new Movie { Id=2, Name="Movie 2" }, ... };
            //   return View(movies);
            // Pour la Q7 on utilise la liste statique (Inception, The Matrix, ...).
            List<Movie> movies = DonneesStatiques.Movies;
            return View(movies);
        }

        // ------------------------------------------------------------------
        // Details — fiche d'un film (Q7 : vue "Détails du Film")
        // Routage CONVENTIONNEL (pas d'attribut) → /Movie/Details/1
        // via la route "default" {controller}/{action}/{id?} de Program.cs
        // ------------------------------------------------------------------
        public IActionResult Details(int id)
        {
            Movie? movie = DonneesStatiques.Movies.FirstOrDefault(m => m.Id == id);
            if (movie == null)
                return NotFound();
            return View(movie);
        }

        // ------------------------------------------------------------------
        // Edit — passer un paramètre dans l'URL : /Movie/Edit/1
        // Q4 : routage par attribut. {id:int} → l'id doit être un entier.
        // ------------------------------------------------------------------
        [Route("Movie/Edit/{id:int}")]
        public IActionResult Edit(int id)
        {
            return Content("Test Id " + id);
        }

        // ------------------------------------------------------------------
        // ByRelease — Q1, Q2, Q3, Q4
        //
        // Q1 : URL /Movie/released/2020/03 → ByRelease(year, month), retourne un Content.
        //
        // Q2 : /Movie/released/2020 (sans le mois) → erreur 404, car le segment
        //      {month} est obligatoire dans la route : aucune route ne correspond.
        //      Pour le rendre optionnel : on ajoute "?" dans la route ({month?})
        //      ET on rend le paramètre nullable (int? month).
        //
        // Q3 : contraintes de type :
        //      {year:int:length(4)}   → entier de exactement 4 chiffres
        //      {month:int:range(1,12)} → entier entre 1 et 12
        //      Ex : /Movie/released/20/03 ou /Movie/released/2020/13 → 404.
        //
        // Q4 : ici la route est définie PAR ATTRIBUT. La version conventionnelle
        //      équivalente se trouve (en commentaire) dans Program.cs.
        // ------------------------------------------------------------------
        [Route("Movie/released/{year:int:length(4)}/{month:int:range(1,12)?}")]
        public IActionResult ByRelease(int year, int? month)
        {
            if (month == null)
                return Content($"Films sortis en {year}");

            return Content($"Films sortis le {month:00}/{year}");
        }
    }
}
