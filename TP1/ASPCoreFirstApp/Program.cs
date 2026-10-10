var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

// ======================================================================
// Q1 à Q3 — Version CONVENTIONNELLE de la route ByRelease.
// Une route spécifique doit être déclarée AVANT la route "default".
//
// Elle est en commentaire car, à la Q4, ByRelease est passée en routage
// par attribut ([Route] dans MovieController). Quand une action a un
// attribut [Route], les routes conventionnelles sont ignorées pour elle.
// Pour tester la version conventionnelle : décommenter ce bloc et
// retirer l'attribut [Route] au-dessus de ByRelease.
//
// app.MapControllerRoute(
//     name: "MovieByRelease",
//     pattern: "Movie/released/{year:int:length(4)}/{month:int:range(1,12)?}",
//     defaults: new { controller = "Movie", action = "ByRelease" });
// ======================================================================

// Routage conventionnel : règle globale {controller}/{action}/{id?}
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// ======================================================================
// Q4 — Faut-il ajouter app.MapControllers() ?
//
// - MapControllers()      → active uniquement le routage PAR ATTRIBUTS.
// - MapControllerRoute()  → ajoute une route CONVENTIONNELLE, et mappe aussi
//                           les contrôleurs qui ont des attributs [Route].
//
// Ici on a déjà MapControllerRoute(), donc les attributs [Route] fonctionnent
// sans MapControllers() (testé : /Movie/released/2020/03 répond bien).
// MapControllers() devient obligatoire quand on n'a AUCUNE route
// conventionnelle (ex. une Web API pure). L'ajouter ici ne gêne pas :
app.MapControllers();
//
// Comparaison attributs vs conventionnel :
// - Lisibilité   : attributs → la route est écrite juste au-dessus de l'action,
//                  on la voit tout de suite. Conventionnel → il faut aller
//                  voir Program.cs.
// - Granularité  : attributs → une route précise par action (URL libres,
//                  contraintes, verbes HTTP). Conventionnel → un même modèle
//                  pour tout le monde, peu flexible pour des URL spéciales.
// - Maintenance  : conventionnel → une seule règle centralisée, ajouter un
//                  contrôleur ne demande rien. Attributs → routes dispersées
//                  dans chaque contrôleur, plus de code à maintenir.
// - API vs vues  : attributs → standard pour les Web API (REST).
//                  Conventionnel → adapté aux applications MVC avec vues.
// ======================================================================

app.Run();
