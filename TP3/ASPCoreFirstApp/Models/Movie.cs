using System.ComponentModel.DataAnnotations;

namespace ASPCoreFirstApp.Models
{
    // CATALOGUE (ApplicationDbContext)
    public class Movie
    {
        public int Id { get; set; }

        // 4.1 — validation avec messages en français
        [Required(ErrorMessage = "Le titre du film est obligatoire.")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "Le titre doit contenir entre 2 et 100 caractères.")]
        [Display(Name = "Titre")]
        public string Name { get; set; } = string.Empty;

        // Clé étrangère vers Genre (Guid depuis le TP3)
        [Display(Name = "Genre")]
        public Guid GenreId { get; set; }
        public Genre? Genre { get; set; }

        [Display(Name = "Disponible")]
        public bool IsAvailable { get; set; } = true;

        // Concurrence optimiste : SQL Server change cette valeur à chaque UPDATE.
        // EF ajoute "WHERE RowVersion = @ancienne_valeur" ; si quelqu'un a modifié le film
        // entre-temps, 0 ligne est mise à jour → DbUpdateConcurrencyException.
        // Utilisé par RentalService pour éviter que deux clients louent le même film.
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}
