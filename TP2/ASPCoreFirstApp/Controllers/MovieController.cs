using ASPCoreFirstApp.Data;
using ASPCoreFirstApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ASPCoreFirstApp.Controllers
{
    // CRUD complet de Movie, lu depuis la base via ApplicationDbContext injecté.
    public class MovieController : Controller
    {
        private readonly ApplicationDbContext _db;
        public MovieController(ApplicationDbContext db) => _db = db;

        // ------------------------------------------------------------------
        // INDEX — liste + tri dynamique + pagination
        // Ex : /Movie?sortOrder=name_desc&page=2&pageSize=5
        // ------------------------------------------------------------------
        public async Task<IActionResult> Index(string? sortOrder, int page = 1, int pageSize = 5)
        {
            // Lien de tri de chaque colonne : un clic inverse l'ordre.
            ViewBag.CurrentSort = sortOrder;
            ViewBag.NameSort = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.GenreSort = sortOrder == "genre" ? "genre_desc" : "genre";

            // Include : charge aussi le Genre de chaque film (jointure SQL)
            // → sinon movie.Genre serait null et on ne pourrait pas afficher son nom.
            IQueryable<Movie> query = _db.Movies.AsNoTracking().Include(m => m.Genre);

            query = sortOrder switch
            {
                "name_desc"  => query.OrderByDescending(m => m.Name),
                "genre"      => query.OrderBy(m => m.Genre!.Name).ThenBy(m => m.Name),
                "genre_desc" => query.OrderByDescending(m => m.Genre!.Name).ThenBy(m => m.Name),
                _            => query.OrderBy(m => m.Name),
            };

            // Pagination : on compte, puis Skip/Take → OFFSET ... FETCH NEXT ... en SQL.
            if (pageSize < 1) pageSize = 5;
            int total = await query.CountAsync();
            int totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            page = Math.Clamp(page, 1, totalPages);

            var movies = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;

            return View(movies);
        }

        // ------------------------------------------------------------------
        // DETAILS
        // ------------------------------------------------------------------
        public async Task<IActionResult> Details(int id)
        {
            var movie = await _db.Movies
                .AsNoTracking()
                .Include(m => m.Genre)
                .FirstOrDefaultAsync(m => m.Id == id);

            return movie == null ? NotFound() : View(movie);
        }

        // ------------------------------------------------------------------
        // CREATE (GET + POST) — le formulaire propose la liste des genres
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
            if (!ModelState.IsValid)
            {
                await LoadGenresAsync(movie.GenreId);
                return View(movie);
            }

            _db.Movies.Add(movie);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ------------------------------------------------------------------
        // EDIT (GET + POST)
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

            // On recharge le film depuis la base puis on modifie seulement les champs du formulaire.
            // EF suit l'entité (tracking) et n'envoie un UPDATE que pour les colonnes modifiées.
            // (Avec _db.Update(input), IsAvailable — absent du formulaire — serait écrasé.)
            var movie = await _db.Movies.FindAsync(id);
            if (movie == null) return NotFound();

            movie.Name = input.Name;
            movie.GenreId = input.GenreId;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ------------------------------------------------------------------
        // DELETE — confirmation (GET) puis suppression (POST)
        // ------------------------------------------------------------------
        public async Task<IActionResult> Delete(int id)
        {
            var movie = await _db.Movies
                .AsNoTracking()
                .Include(m => m.Genre)
                .FirstOrDefaultAsync(m => m.Id == id);

            return movie == null ? NotFound() : View(movie);
        }

        // ActionName("Delete") : même URL /Movie/Delete/5, mais un nom C# différent
        // (deux méthodes ne peuvent pas avoir la même signature Delete(int)).
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movie = await _db.Movies.FindAsync(id);
            if (movie != null)
            {
                _db.Movies.Remove(movie);
                await _db.SaveChangesAsync();   // → DELETE FROM [Movies] WHERE [Id] = @p0
            }
            return RedirectToAction(nameof(Index));
        }

        // ------------------------------------------------------------------
        // Reste du TP1 (routage par attribut)
        // ------------------------------------------------------------------
        [Route("Movie/released/{year:int:length(4)}/{month:int:range(1,12)?}")]
        public IActionResult ByRelease(int year, int? month)
            => Content(month == null ? $"Films sortis en {year}" : $"Films sortis le {month:00}/{year}");

        // Remplit ViewBag.GenreId pour <select asp-for="GenreId" asp-items="ViewBag.GenreId">
        private async Task LoadGenresAsync(int? selectedId = null)
        {
            var genres = await _db.Genres.AsNoTracking().OrderBy(g => g.Name).ToListAsync();
            ViewBag.GenreId = new SelectList(genres, "Id", "Name", selectedId);
        }
    }
}
