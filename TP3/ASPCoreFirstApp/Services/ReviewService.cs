using ASPCoreFirstApp.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ASPCoreFirstApp.Services
{
    // 5 — Avis clients dans MongoDB.
    //
    // Ce qui change par rapport à EF Core / SQL Server :
    // - SCHÉMA       : aucun. La collection est créée au premier insert ; chaque document
    //                  peut avoir des champs différents (Spoiler, Language...).
    // - RELATIONS    : pas de clé étrangère ni de JOIN avec SQL Server. Review.MovieId est une
    //                  référence logique : on charge le film (SQL) PUIS ses avis (Mongo).
    // - TRANSACTIONS : une écriture sur un document est atomique ; les transactions
    //                  multi-documents exigent un replica set ; aucune transaction ne couvre
    //                  Mongo + SQL Server en même temps.
    // - MIGRATIONS   : aucune (pas de dotnet ef). Ajouter un champ = modifier la classe.
    // - OUTILS       : pas de DbContext / tracking / SaveChanges ; on utilise IMongoCollection
    //                  (InsertOne, Find, Aggregate) et MongoDB Compass / mongosh.
    public class ReviewService
    {
        private readonly IMongoCollection<Review> _reviews;

        public ReviewService(IOptions<MongoDbSettings> options)
        {
            var settings = options.Value;
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);
            _reviews = database.GetCollection<Review>(settings.ReviewsCollection);
        }

        // Récupérer tous les avis d'un film
        public async Task<List<Review>> GetByMovieAsync(int movieId)
            => await _reviews.Find(r => r.MovieId == movieId)
                             .SortByDescending(r => r.CreatedAt)
                             .ToListAsync();

        // Insérer un avis
        public async Task AddAsync(Review review)
            => await _reviews.InsertOneAsync(review);

        // Moyenne des notes, calculée PAR MONGODB (pipeline d'agrégation $match + $group),
        // pas en C# : on ne rapatrie pas tous les avis pour faire la moyenne.
        public async Task<double?> GetAverageRatingAsync(int movieId)
        {
            var result = await _reviews.Aggregate()
                .Match(r => r.MovieId == movieId)
                .Group(r => r.MovieId, g => new { Average = g.Average(r => r.Rating) })
                .FirstOrDefaultAsync();

            return result?.Average;   // null s'il n'y a aucun avis
        }
    }
}
