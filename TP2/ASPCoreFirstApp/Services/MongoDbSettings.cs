namespace ASPCoreFirstApp.Services
{
    // Lu depuis la section "MongoDb" de appsettings.json
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = "mongodb://localhost:27017";
        public string DatabaseName { get; set; } = "CinemaReviews";
        public string ReviewsCollection { get; set; } = "Reviews";
    }
}
