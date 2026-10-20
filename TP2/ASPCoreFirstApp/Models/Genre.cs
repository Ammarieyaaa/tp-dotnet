namespace ASPCoreFirstApp.Models
{
    public class Genre
    {
        public int Id { get; set; }
        [System.ComponentModel.DataAnnotations.Display(Name = "Nom")]
        public string Name { get; set; } = string.Empty;

        // Relation 1–N : un genre contient plusieurs films (propriété de navigation).
        public ICollection<Movie> Movies { get; set; } = new List<Movie>();
    }
}
