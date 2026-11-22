using System.ComponentModel.DataAnnotations;

namespace ASPCoreFirstApp.Models
{
    // CATALOGUE (ApplicationDbContext)
    public class Genre
    {
        // TP3 : la clé devient un Guid (les Id du fichier genres.json sont des Guid).
        public Guid Id { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "Nom")]
        public string Name { get; set; } = string.Empty;

        // Côté "N" de la relation 1–N Genre → Movies
        public ICollection<Movie> Movies { get; set; } = new List<Movie>();
    }
}
