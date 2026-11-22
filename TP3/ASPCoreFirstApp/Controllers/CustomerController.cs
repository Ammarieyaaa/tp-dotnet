using ASPCoreFirstApp.Data;
using ASPCoreFirstApp.Models;
using ASPCoreFirstApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ASPCoreFirstApp.Controllers
{
    public class CustomerController : Controller
    {
        // On injecte le contexte de LOCATION (les clients ne sont plus dans le catalogue).
        private readonly RentalDbContext _db;
        public CustomerController(RentalDbContext db) => _db = db;

        // ------------------------------------------------------------------
        // MÉTHODE 1 — Include   (/Customer)
        // ------------------------------------------------------------------
        public async Task<IActionResult> Index()
        {
            var query = _db.Customers
                .AsNoTracking()
                .Include(c => c.MembershipType)
                .OrderBy(c => c.Name);

            // ToQueryString() : affiche le SQL que EF va envoyer (sans l'exécuter).
            // Le même SQL apparaît aussi dans la console grâce à LogTo(Console.WriteLine) (Program.cs).
            ViewBag.Sql = query.ToQueryString();
            ViewBag.Method = "Include";

            var customers = await query.ToListAsync();

            // On transforme les entités en ViewModel pour utiliser la même vue que la méthode 2.
            var model = customers.Select(c => new CustomerListItemViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Email = c.Email,
                MembershipName = c.MembershipType?.Name ?? "",
                DiscountPercent = c.MembershipType?.DiscountPercent ?? 0
            }).ToList();

            return View("Index", model);
        }

        // ------------------------------------------------------------------
        // MÉTHODE 2 — Select vers un ViewModel   (/Customer/IndexProjection)
        // ------------------------------------------------------------------
        public async Task<IActionResult> IndexProjection()
        {
            var query = _db.Customers
                .OrderBy(c => c.Name)
                .Select(c => new CustomerListItemViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Email = c.Email,
                    MembershipName = c.MembershipType!.Name,
                    DiscountPercent = c.MembershipType!.DiscountPercent
                });

            ViewBag.Sql = query.ToQueryString();
            ViewBag.Method = "Select (projection)";

            return View("Index", await query.ToListAsync());
        }

        // Que remarque-t-on dans le SQL ?
        // - Include : SELECT de TOUTES les colonnes de Customers ET de MembershipTypes
        //   (c.Id, c.Email, c.MembershipTypeId, c.Name, m.Id, m.DiscountPercent, m.Name)
        //   avec un INNER JOIN (MembershipTypeId est obligatoire). On charge plus que ce qui est affiché.
        // - Select  : SELECT de SEULEMENT les 5 colonnes utiles (Id, Name, Email,
        //   m.Name, m.DiscountPercent). Moins de données transférées, pas d'entités créées.
        //   Le JOIN est fait automatiquement par EF à partir de c.MembershipType.
        //
        // Effet de AsNoTracking() :
        //   EF ne garde pas les entités chargées dans son "change tracker". C'est plus
        //   rapide et utilise moins de mémoire, idéal pour la LECTURE seule.
        //   Contrepartie : modifier ces objets puis SaveChanges() n'enverrait aucun UPDATE.
        //   Le SQL est le même ; la différence est côté mémoire / C#.
        //   Avec Select vers un ViewModel, AsNoTracking est inutile : un ViewModel
        //   n'est pas une entité, il n'est jamais suivi.

        // ------------------------------------------------------------------
        // CREATE
        // ------------------------------------------------------------------
        public IActionResult Create()
        {
            LoadMemberships();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([Bind("Name,Email,MembershipTypeId")] Customer c)
        {
            if (!ModelState.IsValid)
            {
                // ModelState contient une entrée par champ du formulaire ; chacune a sa liste
                // d'erreurs : erreurs de LIAISON (ex. "abc" pour un int) et erreurs de
                // VALIDATION ([Required], [EmailAddress], [StringLength]...).
                // SelectMany aplatit toutes ces listes en une seule liste de messages,
                // affichée en haut du formulaire (comme un résumé des erreurs).
                ViewBag.Errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                LoadMemberships();
                return View(c);
            }

            _db.Customers.Add(c);
            _db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        private void LoadMemberships()
        {
            ViewBag.Memberships = _db.MembershipTypes
                .AsNoTracking()
                .Select(m => new SelectListItem { Text = m.Name, Value = m.Id.ToString() })
                .ToList();
        }
    }
}
