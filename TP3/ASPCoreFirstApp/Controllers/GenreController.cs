using ASPCoreFirstApp.Data;
using ASPCoreFirstApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ASPCoreFirstApp.Controllers
{
    public class GenreController : Controller
    {
        // Le contexte est injecté explicitement par le constructeur.
        private readonly ApplicationDbContext _db;

        public GenreController(ApplicationDbContext db) => _db = db;

        // AsNoTracking : lecture seule, EF ne suit pas les entités → plus rapide.
        public async Task<IActionResult> Index()
            => View(await _db.Genres.AsNoTracking().ToListAsync());

        public IActionResult Create() => View();

        // [ValidateAntiForgeryToken] : protège contre le CSRF (jeton inséré par <form asp-action>).
        // [Bind("Name")]           : seul Name est accepté (protection contre le sur-postage).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] Genre genre)
        {
            if (!ModelState.IsValid) return View(genre);

            _db.Genres.Add(genre);
            await _db.SaveChangesAsync();   // → INSERT INTO [Genres] ...
            return RedirectToAction(nameof(Index));
        }
    }
}
