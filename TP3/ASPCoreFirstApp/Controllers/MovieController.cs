using ASPCoreFirstApp.Data;
using ASPCoreFirstApp.Models;
using ASPCoreFirstApp.Services;
using ASPCoreFirstApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ASPCoreFirstApp.Controllers
{
    public class MovieController : Controller
    {
        private readonly ApplicationDbContext _db;        // catalogue (SQL Server)
        private readonly RentalDbContext _rentalDb;       // location (SQL Server, autre base)
        private readonly RentalService _rentalService;    // opération sur les deux bases
        private readonly ReviewService _reviews;          // avis (MongoDB)

        public MovieController(ApplicationDbContext db, RentalDbContext rentalDb,
                               RentalService rentalService, ReviewService reviews)
        {
            _db = db;
            _rentalDb = rentalDb;
            _rentalService = rentalService;
            _reviews = reviews;
        }

        // ------------------------------------------------------------------
        // INDEX — liste + tri + pagination (TP2). Affiche le NOM du genre (Include), pas son Guid.
        // ------------------------------------------------------------------
        public async Task<IActionResult> Index(string? sortOrder, int page = 1, int pageSize = 5)
        {
            ViewBag.CurrentSort = sortOrder;
            ViewBag.NameSort = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.GenreSort = sortOrder == "genre" ? "genre_desc" : "genre";

            IQueryable<Movie> query = _db.Movies.AsNoTracking().Include(m => m.Genre);

            query = sortOrder switch
            {
                "name_desc"  => query.OrderByDescending(m => m.Name),
                "genre"      => query.OrderBy(m => m.Genre!.Name).ThenBy(m => m.Name),
                "genre_desc" => query.OrderByDescending(m => m.Genre!.Name).ThenBy(m => m.Name),
                _            => query.OrderBy(m => m.Name),
            };

            if (pageSize < 1) pageSize = 5;
            int total = await query.CountAsync();
            int totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            page = Math.Clamp(page, 1, totalPages);

            var movies = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            return View(movies);
        }

        // ------------------------------------------------------------------
        // DETAILS — film (SQL Server) + avis et moyenne (MongoDB) → MovieDetailsViewModel
        // ------------------------------------------------------------------
        public async Task<IActionResult> Details(int id)
        {
            // 1) Le film depuis SQL Server
            var movie = await _db.Movies
                .AsNoTracking()
                .Include(m => m.Genre)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null) return NotFound();

            var vm = new MovieDetailsViewModel { Movie = movie };

            // 2) Les clients (base de location) pour le bouton "Louer"
            vm.Customers = await _rentalDb.Customers
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Text = c.Name, Value = c.Id.ToString() })
                .ToListAsync();

            // 3) Les avis et la moyenne depuis MongoDB
            try
            {
                vm.Reviews = await _reviews.GetByMovieAsync(id);
                vm.AverageRating = await _reviews.GetAverageRatingAsync(id);
            }
            catch (TimeoutException)
            {
                vm.ReviewsAvailable = false;   // MongoDB arrêté : la page reste utilisable
            }

            return View(vm);
        }

        // ------------------------------------------------------------------
        // CREATE — 4.1
        // ------------------------------------------------------------------
        public async Task<IActionResult> Create()
        {
            await LoadGenresAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,GenreId")] Movie movie)
        {
            // IsAvailable n'est pas dans [Bind] : l'utilisateur ne peut pas le saisir.
            if (!await _db.Genres.AnyAsync(g => g.Id == movie.GenreId))
                ModelState.AddModelError(nameof(Movie.GenreId), "Veuillez choisir un genre valide.");

            if (!ModelState.IsValid)
            {
                await LoadGenresAsync(movie.GenreId);
                return View(movie);
            }

            movie.IsAvailable = true;   // un nouveau film est toujours disponible
            _db.Movies.Add(movie);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ------------------------------------------------------------------
        // EDIT (TP2)
        // ------------------------------------------------------------------
        public async Task<IActionResult> Edit(int id)
        {
            var movie = await _db.Movies.FindAsync(id);
            if (movie == null) return NotFound();

            await LoadGenresAsync(movie.GenreId);
            return View(movie);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,GenreId")] Movie input)
        {
            if (id != input.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                await LoadGenresAsync(input.GenreId);
                return View(input);
            }

            var movie = await _db.Movies.FindAsync(id);
            if (movie == null) return NotFound();

            movie.Name = input.Name;
            movie.GenreId = input.GenreId;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ------------------------------------------------------------------
        // DELETE (TP2)
        // ------------------------------------------------------------------
        public async Task<IActionResult> Delete(int id)
        {
            var movie = await _db.Movies.AsNoTracking().Include(m => m.Genre)
                                 .FirstOrDefaultAsync(m => m.Id == id);
            return movie == null ? NotFound() : View(movie);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movie = await _db.Movies.FindAsync(id);
            if (movie != null)
            {
                _db.Movies.Remove(movie);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // ------------------------------------------------------------------
        // RENT — 4.2 : location (POST + jeton anti-falsification)
        // ------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rent(int id, int customerId)
        {
            var result = await _rentalService.RentAsync(id, customerId);

            // TempData survit à la redirection → message affiché dans Details
            if (result.Success)
                TempData["Success"] = "Film loué avec succès.";
            else
                TempData["Error"] = result.Error;

            return RedirectToAction(nameof(Details), new { id });
        }

        // ------------------------------------------------------------------
        // ADD REVIEW — 5 : formulaire "Donner un avis" (MongoDB)
        // ------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int id, int rating, string? comment,
                                                   string? tags, bool spoiler, string? language)
        {
            if (!await _db.Movies.AnyAsync(m => m.Id == id)) return NotFound();

            var review = new Review
            {
                MovieId = id,
                Rating = Math.Clamp(rating, 1, 5),
                Comment = comment,
                Tags = (tags ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList(),
                // Champs facultatifs : absents du document s'ils ne sont pas renseignés
                Spoiler = spoiler ? true : null,
                Language = string.IsNullOrWhiteSpace(language) ? null : language
            };

            try
            {
                await _reviews.AddAsync(review);
                TempData["Success"] = "Merci pour votre avis !";
            }
            catch (TimeoutException)
            {
                TempData["Error"] = "Avis non enregistré : MongoDB est injoignable.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task LoadGenresAsync(Guid? selectedId = null)
        {
            var genres = await _db.Genres.AsNoTracking().OrderBy(g => g.Name).ToListAsync();
            ViewBag.GenreId = new SelectList(genres, "Id", "Name", selectedId);
        }
    }
}
