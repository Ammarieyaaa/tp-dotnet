using ASPCoreFirstApp.Data;
using ASPCoreFirstApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace ASPCoreFirstApp.Controllers
{
    public class CustomerController : Controller
    {
        // Q6 — Rechercher un client par son Id dans la liste statique.
        // URL : /Customer/CustomerDetails/1  → "Amal Ben Salah"
        //       /Customer/CustomerDetails/99 → "Client introuvable"
        public IActionResult CustomerDetails(int id)
        {
            Customer? customer = DonneesStatiques.Customers.FirstOrDefault(c => c.Id == id);

            if (customer == null)
                return Content("Client introuvable");

            return Content(customer.Name ?? "");
        }
    }
}
