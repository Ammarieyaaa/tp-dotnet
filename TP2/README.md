# TP2 — Entity Framework Core & CRUD

Projet : `ASPCoreFirstApp` (ASP.NET Core MVC, .NET 10, EF Core 10)

Ce TP reprend le projet du TP1 et remplace les listes statiques par une vraie base de données SQL Server.

## Objectifs

- Configurer la connexion à une base relationnelle avec EF Core (DbContext, DbSet, injection de dépendances)
- Générer et faire évoluer le schéma en approche Code First (migrations)
- Implémenter un CRUD complet dans ASP.NET Core MVC
- Avancé : utiliser deux DbContext dans une même application, puis comparer avec MongoDB

## Prérequis

- SDK .NET 10
- SQL Server LocalDB (installé avec Visual Studio)
- Outil EF Core : `dotnet tool install --global dotnet-ef`
- MongoDB (seulement pour la partie avancée sur les avis)

Packages NuGet utilisés :

- `Microsoft.EntityFrameworkCore`
- `Microsoft.EntityFrameworkCore.Design`
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.EntityFrameworkCore.Tools`
- `MongoDB.Driver`

## Lancer le projet

```bash
cd TP2/ASPCoreFirstApp
dotnet ef database update --context ApplicationDbContext
dotnet ef database update --context RentalDbContext
dotnet run
```

Les deux commandes `database update` créent les bases si elles n'existent pas.

Pour les avis (MongoDB), démarrer le service dans un terminal administrateur :

```bash
net start MongoDB
```

Si MongoDB est arrêté, l'application fonctionne quand même : la page des avis affiche un message « MongoDB est injoignable ».

## Bases de données

| Base | Contexte | Contenu |
| ---- | -------- | ------- |
| `CinemaDb_TP2` (SQL Server) | `ApplicationDbContext` | Catalogue : `Movies`, `Genres` |
| `RentalDb_TP2` (SQL Server, schéma `rental`) | `RentalDbContext` | Location : `Customers`, `Rentals` |
| `CinemaReviews_TP2` (MongoDB) | `ReviewService` | Avis clients : collection `Reviews` |

Les chaînes de connexion sont dans `appsettings.json` (`DefaultConnection`, `DefaultConnection2`, section `MongoDb`).

## Migrations

Catalogue (`Migrations/`) :

1. `InitialCreate` : tables `Movies` et `Genres`
2. `SeedGenres` : genres Action, Drame, Comédie
3. `AddGenreToMovie` : relation 1–N Genre → Movie
4. `AddIsAvailableToMovie` : colonne `IsAvailable`
5. `SeedMovies` : 8 films pour tester le tri et la pagination

Location (`Migrations/Rental/`) :

1. `InitialRental` : tables `Customers` et `Rentals` dans le schéma `rental`

Avec deux contextes, l'option `--context` est obligatoire pour toutes les commandes `dotnet ef`.

## Structure

| Fichier | Rôle |
| ------- | ---- |
| `Models/Movie.cs`, `Genre.cs`, `Customer.cs`, `Rental.cs` | Entités |
| `Models/Review.cs` | Document MongoDB (avis) |
| `Data/ApplicationDbContext.cs` | Contexte du catalogue + seed |
| `Data/RentalDbContext.cs` | Contexte de location |
| `Services/ReviewService.cs` | Accès à MongoDB |
| `Controllers/MovieController.cs` | CRUD des films, tri, pagination |
| `Controllers/GenreController.cs` | Liste et ajout des genres |
| `Controllers/ReviewController.cs` | Liste et ajout des avis |
| `Program.cs` | Enregistrement des deux DbContext et de MongoDB |

## URLs à tester

| URL | Résultat |
| --- | -------- |
| `/Movie` | Liste des films avec leur genre, 5 par page |
| `/Movie?sortOrder=name_desc` | Tri par nom décroissant |
| `/Movie?sortOrder=genre` | Tri par genre |
| `/Movie?page=2` | Page suivante |
| `/Movie/Create` | Ajout d'un film avec liste déroulante des genres |
| `/Movie/Edit/1` | Modification d'un film |
| `/Movie/Delete/1` | Confirmation puis suppression |
| `/Movie/Details/1` | Détails d'un film |
| `/Genre` et `/Genre/Create` | Liste et ajout des genres |
| `/Review` | Avis clients (MongoDB) |

## Ce qui a été fait

### Partie 1 — Configuration et migrations

- `ApplicationDbContext` avec `DbContextOptions<ApplicationDbContext>` (version générique, nécessaire dès qu'il y a plusieurs contextes)
- Enregistrement dans `Program.cs` avec `AddDbContext` : un contexte est créé par requête HTTP et injecté dans les contrôleurs
- Seed des genres avec `HasData` dans `OnModelCreating`

### Partie 2 — Contrôleurs et vues

- `MovieController` et `GenreController` reçoivent le contexte par leur constructeur
- `AsNoTracking()` pour les lectures : EF ne suit pas les entités, c'est plus rapide
- Les vues Index, Create, Edit et Delete suivent la structure générée par le scaffolding

**Sécurité**
- `[ValidateAntiForgeryToken]` protège contre le CSRF. Le jeton est ajouté automatiquement par `<form asp-action>`. Un formulaire envoyé sans jeton est refusé (erreur 400).
- `[Bind("Name,GenreId")]` limite les champs acceptés (protection contre le sur-postage).
- La protection contre le XSS vient de l'encodage HTML automatique de Razor.

### C'est à vous

**1. Relation Genre – Movie** : `Genre.Movies` (collection) et `Movie.GenreId` + `Movie.Genre`. EF trouve la relation par convention.

**2. CRUD complet**
- Index : `Include(m => m.Genre)` pour afficher le nom du genre
- Create : liste des genres avec `SelectList` et `asp-items`
- Edit : le film est rechargé depuis la base, puis seuls le nom et le genre sont modifiés. Avec `_db.Update(movie)`, `IsAvailable` (absent du formulaire) serait écrasé.
- Delete : page de confirmation (GET) puis suppression (POST, `[ActionName("Delete")]`)

**3. Tri et pagination** : paramètres `sortOrder`, `page` et `pageSize` (5 par défaut). Liens de tri sur les colonnes Nom et Genre, boutons Précédent / Suivant qui gardent le tri. En SQL, `Skip` / `Take` deviennent `OFFSET ... FETCH NEXT`.

### Partie avancée

**1. Deux DbContext**

Ce qu'on observe :
- chaque contexte a son propre modèle, ses propres migrations et sa propre table `__EFMigrationsHistory`
- sans `--context`, `dotnet ef` répond « More than one DbContext was found »
- aucune jointure ni clé étrangère n'est possible entre les deux bases : `Rental.MovieId` est un simple entier (référence logique)
- un `SaveChanges()` sur un contexte n'enregistre rien dans l'autre : une opération sur les deux bases n'est pas atomique (traité au TP3)

**2. MongoDB pour les avis**

| Point | SQL Server + EF Core | MongoDB |
| ----- | -------------------- | ------- |
| Schéma | Tables et colonnes fixes | Aucun schéma : chaque document peut avoir des champs différents (`Spoiler`, `Language`…) |
| Migrations | `dotnet ef migrations add` | Aucune : on modifie la classe C# |
| Relations | Clés étrangères, `JOIN` | Pas de clé étrangère : `MovieId` est une référence logique |
| Transactions | Transactions SQL classiques | Écriture atomique sur un document ; multi-documents seulement avec un replica set ; aucune transaction commune avec SQL Server |
| Outils | `DbContext`, change tracking, `SaveChanges()`, SSMS | `IMongoCollection<T>` (`InsertOne`, `Find`), MongoDB Compass ou `mongosh` |

`MongoClient` est enregistré en singleton : il est thread-safe et gère lui-même ses connexions.
