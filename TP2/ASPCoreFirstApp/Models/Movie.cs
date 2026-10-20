using System.ComponentModel.DataAnnotations;

namespace ASPCoreFirstApp.Models
{
    public class Movie
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire.")]
        [Display(Name = "Nom")]
        public string Name { get; set; } = string.Empty;

        // Clé étrangère : EF la reconnaît par convention (NomNavigation + Id).
        [Display(Name = "Genre")]
        public int GenreId { get; set; }

        // Propriété de navigation (nullable : elle n'est remplie que si on fait .Include).
        public Genre? Genre { get; set; }

        // Partie avancée : disponibilité du film (migration AddIsAvailableToMovie).
        [Display(Name = "Disponible")]
        public bool IsAvailable { get; set; } = true;
    }
}
