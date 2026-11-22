# TP3 — Relations, Seed Data et deux DbContext

Projet : `ASPCoreFirstApp` (ASP.NET Core MVC, .NET 10, EF Core 10)

Ce TP est la suite du TP2. Le catalogue (Movie, Genre) reste dans son DbContext, la location (MembershipType, Customer, Rental) a son propre DbContext et sa propre base, et les avis clients sont dans MongoDB. L'objectif est de comparer trois façons de stocker des données dans une même application.

## Objectifs

- Configurer des relations 1–N (convention et API Fluent) et choisir le bon `DeleteBehavior`
- Alimenter la base avec du seed data (`HasData`) en respectant ses règles
- Gérer deux DbContext, deux bases et deux historiques de migrations
- Comprendre pourquoi une opération sur deux bases n'est plus atomique, et la sécuriser
- Connecter MongoDB et analyser ce qui change

## Prérequis

- SDK .NET 10
- SQL Server LocalDB
- Outil EF Core : `dotnet tool install --global dotnet-ef`
- MongoDB local (pour les avis)

## Lancer le projet

```bash
cd TP3/ASPCoreFirstApp
dotnet ef database update --context ApplicationDbContext
dotnet ef database update --context RentalDbContext
dotnet run
```

Pour les avis, démarrer MongoDB dans un terminal administrateur :

```bash
net start MongoDB
```

Si MongoDB est arrêté, la page de détails d'un film s'affiche quand même, avec un message « MongoDB est injoignable » à la place des avis.

## Bases de données

| Base | Contexte | Contenu |
| ---- | -------- | ------- |
| `CinemaDb_TP3` (SQL Server) | `ApplicationDbContext` | Catalogue : `Movies`, `Genres` |
| `RentalDb_TP3` (SQL Server, schéma `rental`) | `RentalDbContext` | Location : `MembershipTypes`, `Customers`, `Rentals` |
| `CinemaReviews_TP3` (MongoDB) | `ReviewService` | Avis clients : collection `Reviews` |

## Migrations

| Dossier | Migrations |
| ------- | ---------- |
| `Migrations/Catalog` | `CatalogV2` (schéma), `SeedCatalog` (genres et films) |
| `Migrations/Rental` | `RentalV2` (schéma), `SeedRental` (abonnements et clients) |

```bash
dotnet ef migrations add SeedCatalog --context ApplicationDbContext --output-dir Migrations/Catalog
dotnet ef migrations add SeedRental  --context RentalDbContext      --output-dir Migrations/Rental
```

Sans `--context`, l'outil répond « More than one DbContext was found ».

**Pourquoi un nouvel historique de migrations ?** Au TP3, l'Id de `Genre` passe de `int` à `Guid` (les Id de `genres.json` sont des Guid). SQL Server ne permet pas de changer le type d'une colonne identité par une migration. Le TP3 repart donc de nouvelles bases (`_TP3`) avec un historique propre.

## Structure

| Fichier | Rôle |
| ------- | ---- |
| `Models/Movie.cs`, `Genre.cs` | Catalogue |
| `Models/MembershipType.cs`, `Customer.cs`, `Rental.cs` | Location |
| `Models/Review.cs` | Document MongoDB (avis) |
| `Data/ApplicationDbContext.cs` | Relation Genre → Movie, seed depuis JSON |
| `Data/RentalDbContext.cs` | Relations de la location, seed |
| `Data/Seed/genres.json` | Genres à semer |
| `Services/RentalService.cs` | Location sur deux bases (concurrence, compensation) |
| `Services/ReviewService.cs` | Avis : lecture, ajout, moyenne |
| `ViewModels/CustomerListItemViewModel.cs` | Liste des clients (colonnes utiles) |
| `ViewModels/MovieDetailsViewModel.cs` | Film + avis + moyenne + clients |
| `Controllers/CustomerController.cs` | Liste (Include / Select) et ajout de clients |
| `Controllers/MovieController.cs` | CRUD, location, avis |

## URLs à tester

| URL | Résultat |
| --- | -------- |
| `/Customer` | Clients avec la méthode `Include`, et le SQL généré |
| `/Customer/IndexProjection` | Clients avec la méthode `Select`, et le SQL généré |
| `/Customer/Create` | Ajout d'un client, avec liste des erreurs |
| `/Movie` | Films avec le nom du genre (pas son Guid) |
| `/Movie/Create` | Ajout d'un film avec validation en français |
| `/Movie/Details/1` | Détails, bouton « Louer », avis et note moyenne |

## Ce qui a été fait

### 1. Finalisation des bases de données

- `Customer` n'est plus dans le catalogue : il n'y a aucun `DbSet<Customer>` dans `ApplicationDbContext`, seulement dans `RentalDbContext`.
- `MembershipType`, `Customer` et `Rental` sont dans `RentalDbContext`.
- `Rental.MovieId` est une référence **logique** vers un film d'une autre base : pas de propriété de navigation ni de clé étrangère. Un index accélère la recherche.

**Relations et `DeleteBehavior` (API Fluent)**

| Relation | Comportement | Raison |
| -------- | ------------ | ------ |
| Genre → Movies | `Restrict` | `Cascade` supprimerait tous les films du genre ; `SetNull` est impossible car `GenreId` n'est pas nullable |
| MembershipType → Customers | `Restrict` | On ne supprime pas un abonnement encore utilisé |
| Customer → Rentals | `Restrict` | On garde l'historique des locations |

Par convention, EF trouve déjà ces relations tout seul. L'API Fluent permet de les écrire explicitement et de choisir le comportement à la suppression.

### 2. Clients : affichage et formulaire d'ajout

**Include ou Select : que remarque-t-on dans le SQL ?**

Avec `Include` :

```sql
SELECT [c].[Id], [c].[Email], [c].[MembershipTypeId], [c].[Name],
       [m].[Id], [m].[DiscountPercent], [m].[Name]
FROM [rental].[Customers] AS [c]
INNER JOIN [rental].[MembershipTypes] AS [m] ON [c].[MembershipTypeId] = [m].[Id]
ORDER BY [c].[Name]
```

Avec `Select` vers un ViewModel :

```sql
SELECT [c].[Id], [c].[Name], [c].[Email],
       [m].[Name] AS [MembershipName], [m].[DiscountPercent]
FROM [rental].[Customers] AS [c]
INNER JOIN [rental].[MembershipTypes] AS [m] ON [c].[MembershipTypeId] = [m].[Id]
ORDER BY [c].[Name]
```

- `Include` charge **toutes** les colonnes des deux tables.
- `Select` ne charge que les **5 colonnes affichées** : moins de données transférées, et aucune entité créée.
- Dans les deux cas c'est un `INNER JOIN`, car `MembershipTypeId` est obligatoire.

Le SQL est affiché sur la page (`ToQueryString()`) et dans la console (`LogTo(Console.WriteLine)` dans `Program.cs`).

**Effet de `AsNoTracking()`** : EF ne garde pas les entités dans son « change tracker ». C'est plus rapide et prend moins de mémoire, ce qui est idéal pour de la lecture seule. Le SQL ne change pas. En contrepartie, modifier ces objets puis appeler `SaveChanges()` n'enverrait aucun `UPDATE`. Avec un `Select` vers un ViewModel, `AsNoTracking()` est inutile : un ViewModel n'est jamais suivi.

**Liste des erreurs de `ModelState`** : `ModelState` contient une entrée par champ, chacune avec sa liste d'erreurs :
- erreurs de **liaison** (ex. `abc` dans un champ numérique)
- erreurs de **validation** (`[Required]`, `[EmailAddress]`, `[StringLength]`)

`SelectMany` aplatit toutes ces listes en une seule, affichée en haut du formulaire. Exemple obtenu :

- Le nom est obligatoire.
- L'adresse email n'est pas valide.
- The value 'abc' is not valid for Type d'abonnement.

Le dernier message est en anglais : c'est un message de liaison par défaut d'ASP.NET Core, pas un attribut de validation.

### 3. Seed data

- `HasData` exige des clés primaires explicites : c'est ainsi qu'EF sait quelle ligne modifier ou supprimer à la migration suivante.
- Les données sont figées dans la migration : modifier le seed puis relancer `migrations add` génère un `UPDATE` ou un `DELETE`.
- On ne renseigne que les clés étrangères (`GenreId`), jamais les propriétés de navigation.
- Les Guid sont écrits en dur : un `Guid.NewGuid()` changerait à chaque exécution et EF croirait que les données ont changé.
- Chaque DbContext sème ses propres tables : le catalogue ne peut pas semer des clients.
- `genres.json` est copié dans `bin/` grâce au `.csproj` (`CopyToOutputDirectory`) ; sans cela, la lecture échoue.

Remarque : l'énoncé écrit `var Science-fiction = ...`, qui n'est pas un nom de variable C# valide (le `-` est un opérateur). La variable s'appelle `scienceFiction`.

### 4. Gestion d'un film

**4.1 Ajout d'un film**
- Liste déroulante des genres depuis `ApplicationDbContext`
- `[Required]` et `[StringLength(100, MinimumLength = 2)]` avec messages en français
- `IsAvailable` mis à `true` par le contrôleur ; il n'est pas dans `[Bind]`, donc l'utilisateur ne peut pas le saisir
- `[ValidateAntiForgeryToken]` et redirection vers Index
- La liste affiche le nom du genre, pas son Guid

**4.2 Location d'un film (deux bases)**

`RentalService` modifie deux bases :
1. `CinemaDb_TP3` : `Movie.IsAvailable = false`
2. `RentalDb_TP3` : nouvelle ligne `Rental`

**Pourquoi ce n'est plus atomique ?** Chaque DbContext a sa propre connexion et sa propre transaction. Le `SaveChanges()` du catalogue est validé avant que la location soit écrite. Si la deuxième étape échoue, la première est déjà en base : le film serait marqué « loué » sans location. Il n'y a pas de transaction commune aux deux bases.

**Comment on sécurise :**
- **Concurrence optimiste** : `Movie.RowVersion` (`[Timestamp]`). EF ajoute `WHERE RowVersion = @ancienne_valeur` à l'`UPDATE`. Si un autre client a loué le film entre-temps, aucune ligne n'est modifiée et EF lève `DbUpdateConcurrencyException` : la location est refusée. Test : 3 locations envoyées en même temps pour le même film, une seule est enregistrée.
- **Compensation** : si l'écriture de la location échoue, le service remet `IsAvailable = true` pour revenir à un état cohérent.

L'action `Rent` (POST, jeton anti-falsification) appelle le service. Le bouton « Louer » n'apparaît que si le film est disponible, et le message d'erreur du service est affiché sur la page (« Ce film est déjà loué. », « Client introuvable. »…).

### 5. MongoDB : avis des clients

`ReviewService` permet de :
- récupérer tous les avis d'un film
- insérer un avis
- calculer la note moyenne, **par MongoDB** (pipeline d'agrégation `$match` + `$group`), sans rapatrier tous les avis

`MovieController.Details` charge le film depuis SQL Server, puis ses avis et sa moyenne depuis MongoDB, et transmet le tout à la vue via `MovieDetailsViewModel`. La page contient un formulaire « Donner un avis » (note, commentaire, étiquettes, spoiler, langue).

**Ce qui change avec MongoDB**

| Point | SQL Server + EF Core | MongoDB |
| ----- | -------------------- | ------- |
| Schéma | Tables et colonnes fixes | Aucun : `Spoiler` et `Language` n'existent que dans certains documents |
| Relations | Clés étrangères, `JOIN` | `MovieId` est une référence logique : on charge le film, puis ses avis |
| Transactions | Transactions SQL | Écriture atomique par document ; aucune transaction commune avec SQL Server |
| Migrations | `dotnet ef migrations` | Aucune : on modifie la classe C# |
| Outils | `DbContext`, `SaveChanges()`, SSMS | `IMongoCollection<T>`, agrégations, MongoDB Compass ou `mongosh` |
