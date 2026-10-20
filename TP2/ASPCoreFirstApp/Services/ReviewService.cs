using ASPCoreFirstApp.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ASPCoreFirstApp.Services
{
    // Partie avancée — accès à MongoDB avec le driver officiel (MongoDB.Driver).
    //
    // Ce qui change par rapport à EF Core / SQL Server :
    // - SCHÉMA     : aucun schéma imposé. La collection est créée au premier insert,
    //                chaque document peut avoir des champs différents (Spoiler, Language...).
    //                Pas de CREATE TABLE.
    // - MIGRATIONS : pas de dotnet ef migrations. Ajouter un champ = juste modifier la classe C#.
    //                Les anciens documents n'ont simplement pas ce champ.
    // - RELATIONS  : pas de clé étrangère ni de JOIN avec SQL Server. Review.MovieId est
    //                une référence logique ; la cohérence est à la charge de l'application.
    // - TRANSACTIONS : chaque écriture sur UN document est atomique. Les transactions
    //                multi-documents exigent un replica set, et aucune transaction ne
    //                couvre à la fois MongoDB et SQL Server.
    // - OUTILS     : pas de DbContext, pas de change tracking, pas de SaveChanges().
    //                On utilise IMongoCollection<T> (InsertOne, Find...) et on consulte
    //                les données avec MongoDB Compass ou mongosh au lieu de SSMS.
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

        public async Task<List<Review>> GetAllAsync()
            => await _reviews.Find(_ => true).SortByDescending(r => r.CreatedAt).ToListAsync();

        public async Task<List<Review>> GetByMovieAsync(int movieId)
            => await _reviews.Find(r => r.MovieId == movieId)
                             .SortByDescending(r => r.CreatedAt)
                             .ToListAsync();

        public async Task AddAsync(Review review)
            => await _reviews.InsertOneAsync(review);
    }
}
