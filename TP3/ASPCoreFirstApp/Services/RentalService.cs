using ASPCoreFirstApp.Data;
using ASPCoreFirstApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ASPCoreFirstApp.Services
{
    public record RentalResult(bool Success, string? Error)
    {
        public static RentalResult Ok() => new(true, null);
        public static RentalResult Fail(string error) => new(false, error);
    }

    // 4.2 — Location d'un film : une opération qui modifie DEUX bases.
    //   1) CinemaDb_TP3 : Movie.IsAvailable = false   (ApplicationDbContext)
    //   2) RentalDb_TP3 : INSERT d'une ligne Rental   (RentalDbContext)
    //
    // Pourquoi ce n'est plus atomique ?
    //   Chaque DbContext a sa propre connexion et sa propre transaction. SaveChanges()
    //   sur le catalogue est validé (COMMIT) AVANT que la location soit écrite.
    //   Si l'étape 2 échoue, l'étape 1 est déjà en base → film marqué "loué" sans location.
    //   Il n'y a pas de transaction commune aux deux bases (pas de transaction distribuée).
    //
    // Comment on sécurise :
    //   - Concurrence optimiste (Movie.RowVersion) : si deux clients louent le même film
    //     en même temps, le 2e UPDATE ne trouve plus la bonne RowVersion → exception → refus.
    //   - Compensation : si l'étape 2 échoue, on ANNULE l'étape 1 à la main
    //     (IsAvailable = true) pour revenir à un état cohérent.
    public class RentalService
    {
        private readonly ApplicationDbContext _catalog;
        private readonly RentalDbContext _rental;
        private readonly ILogger<RentalService> _logger;

        public RentalService(ApplicationDbContext catalog, RentalDbContext rental, ILogger<RentalService> logger)
        {
            _catalog = catalog;
            _rental = rental;
            _logger = logger;
        }

        public async Task<RentalResult> RentAsync(int movieId, int customerId)
        {
            // Vérifications préalables (lecture seule)
            if (!await _rental.Customers.AnyAsync(c => c.Id == customerId))
                return RentalResult.Fail("Client introuvable.");

            var movie = await _catalog.Movies.FirstOrDefaultAsync(m => m.Id == movieId);
            if (movie == null)
                return RentalResult.Fail("Film introuvable.");

            if (!movie.IsAvailable)
                return RentalResult.Fail("Ce film est déjà loué.");

            // ---- Étape 1 : catalogue ----
            // UPDATE Movies SET IsAvailable = 0 WHERE Id = @id AND RowVersion = @rowVersion
            movie.IsAvailable = false;
            try
            {
                await _catalog.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return RentalResult.Fail("Ce film vient d'être loué par un autre client. Réessayez.");
            }

            // ---- Étape 2 : location ----
            try
            {
                _rental.Rentals.Add(new Rental
                {
                    MovieId = movieId,
                    CustomerId = customerId,
                    RentedAt = DateTime.UtcNow
                });
                await _rental.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Échec de l'écriture de la location, compensation du film {MovieId}", movieId);

                // ---- Compensation : on annule l'étape 1 ----
                movie.IsAvailable = true;
                try
                {
                    await _catalog.SaveChangesAsync();
                }
                catch (Exception ex2)
                {
                    // Cas rare : la compensation échoue aussi → incohérence à corriger à la main.
                    _logger.LogCritical(ex2, "Compensation impossible pour le film {MovieId}", movieId);
                }
                return RentalResult.Fail("La location a échoué. Le film a été remis disponible.");
            }

            return RentalResult.Ok();
        }
    }
}
