using ASPCoreFirstApp.Data;
using ASPCoreFirstApp.Models;
using ASPCoreFirstApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ASPCoreFirstApp.Controllers
{
    // Partie avancée — avis clients : le catalogue vient de SQL Server, les avis de MongoDB.
    public class ReviewController : Controller
    {
        private readonly ReviewService _reviews;
        private readonly ApplicationDbContext _db;

        public ReviewController(ReviewService reviews, ApplicationDbContext db)
        {
            _reviews = reviews;
            _db = db;
        }

        // /Review          → tous les avis
        // /Review?movieId=1 → avis d'un film
        public async Task<IActionResult> Index(int? movieId)
        {
            await LoadMoviesAsync(movieId);
            ViewBag.MovieId = movieId;

            try
            {
                var reviews = movieId == null
                    ? await _reviews.GetAllAsync()
                    : await _reviews.GetByMovieAsync(movieId.Value);
                return View(reviews);
            }
            catch (TimeoutException)
            {
                // Le serveur MongoDB n'est pas démarré : on l'indique au lieu de planter.
                ViewBag.MongoError = "MongoDB est injoignable (mongodb://localhost:27017). Démarrez le service MongoDB.";
                return View(new List<Review>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int movieId, int rating, string? comment, string? tags)
        {
            var review = new Review
            {
                MovieId = movieId,
                Rating = Math.Clamp(rating, 1, 5),
                Comment = comment,
                Tags = (tags ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList()
            };

            try
            {
                await _reviews.AddAsync(review);
            }
            catch (TimeoutException)
            {
                TempData["Error"] = "Avis non enregistré : MongoDB est injoignable.";
            }

            return RedirectToAction(nameof(Index), new { movieId });
        }

        private async Task LoadMoviesAsync(int? selected)
        {
            var movies = await _db.Movies.AsNoTracking().OrderBy(m => m.Name).ToListAsync();
            ViewBag.Movies = new SelectList(movies, "Id", "Name", selected);
            ViewBag.MovieNames = movies.ToDictionary(m => m.Id, m => m.Name);
        }
    }
}
