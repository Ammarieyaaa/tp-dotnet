using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ASPCoreFirstApp.Models
{
    // Partie avancée — un avis client, stocké dans MongoDB (collection "Reviews").
    // C'est un DOCUMENT (JSON/BSON), pas une ligne de table : sa structure peut varier.
    [BsonIgnoreExtraElements]   // si un document a des champs en plus, on les ignore au lieu de planter
    public class Review
    {
        // Clé MongoDB : ObjectId généré automatiquement (pas d'identité SQL).
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        // Référence logique vers Movie.Id (SQL Server) : aucune clé étrangère possible.
        public int MovieId { get; set; }

        public int Rating { get; set; }               // note de 1 à 5
        public string? Comment { get; set; }
        public List<string> Tags { get; set; } = new();

        // Champs optionnels : absents de certains documents, sans migration.
        [BsonIgnoreIfNull] public bool? Spoiler { get; set; }
        [BsonIgnoreIfNull] public string? Language { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
