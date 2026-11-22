using ASPCoreFirstApp.Data;
using ASPCoreFirstApp.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// 1) Contexte du catalogue (Movie, Genre) → base CinemaDb_TP3
// AddDbContext enregistre le contexte dans le conteneur d'injection de dépendances
// avec une durée de vie "Scoped" (un contexte par requête HTTP).
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// 2) Partie avancée — contexte de location (Customer, Rental) → base RentalDb_TP3
// LogTo(Console.WriteLine) : affiche dans la console le SQL envoyé par ce contexte
// (utile pour comparer Include et Select dans CustomerController).
builder.Services.AddDbContext<RentalDbContext>(options =>
    options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection2"))
        .LogTo(Console.WriteLine, new[] { DbLoggerCategory.Database.Command.Name }, LogLevel.Information));

// 3) Partie avancée — MongoDB pour les avis clients
// Singleton : MongoClient est thread-safe et gère lui-même son pool de connexions.
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDb"));
builder.Services.AddSingleton<ReviewService>();

// 4) TP3 — service de location (utilise les DEUX DbContext) : Scoped comme eux.
builder.Services.AddScoped<RentalService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllers();

app.Run();
