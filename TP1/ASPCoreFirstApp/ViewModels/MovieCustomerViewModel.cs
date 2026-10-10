using ASPCoreFirstApp.Models;

namespace ASPCoreFirstApp.ViewModels
{
    // Q5 — Une vue Razor n'accepte qu'UN SEUL @model.
    // On ne peut donc pas écrire @model Movie ET @model List<Customer> dans la même vue.
    // Le ViewModel résout ce problème : c'est une classe "sur mesure" pour la vue,
    // qui regroupe dans un seul objet toutes les données dont la page a besoin
    // (ici un film + la liste des clients qui l'ont réservé), avec un typage fort.
    public class MovieCustomerViewModel
    {
        public Movie Movie { get; set; } = new Movie();
        public List<Customer> Customers { get; set; } = new List<Customer>();
    }
}
