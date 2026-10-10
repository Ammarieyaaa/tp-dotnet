# TP1 — ASP.NET Core MVC et systèmes de routage

Projet : `ASPCoreFirstApp` (ASP.NET Core MVC, .NET 10)

## Objectifs

- Comprendre les bases d'une application ASP.NET Core MVC (Modèle, Vue, Contrôleur)
- Manipuler le routage conventionnel et le routage par attributs
- Afficher plusieurs modèles dans une même vue grâce à un ViewModel

## Lancer le projet

```bash
cd TP1/ASPCoreFirstApp
dotnet run
```

Puis ouvrir l'URL affichée dans la console (ex. `http://localhost:5xxx`).

Aucune base de données n'est utilisée dans ce TP : les données sont des listes statiques en mémoire (`Data/DonneesStatiques.cs`).

## Structure

| Fichier | Rôle |
| ------- | ---- |
| `Models/Movie.cs`, `Models/Customer.cs` | Modèles (Id, Name) |
| `ViewModels/MovieCustomerViewModel.cs` | Un film + la liste de ses clients |
| `Data/DonneesStatiques.cs` | Films et clients en mémoire |
| `Controllers/MovieController.cs` | Index, Details, Edit, ByRelease |
| `Controllers/MovieCustomerController.cs` | Page film + clients (ViewModel) |
| `Controllers/CustomerController.cs` | Recherche d'un client par Id |
| `Views/Movie/Index.cshtml`, `Details.cshtml` | Liste et détails des films (HTML + CSS) |
| `Program.cs` | Configuration des routes |

## URLs à tester

| URL | Résultat |
| --- | -------- |
| `/Movie` ou `/Movie/Index` | Liste des films |
| `/Movie/Details/1` | Détails du film « Inception » |
| `/Movie/Edit/1` | `Test Id 1` |
| `/Movie/released/2020/03` | `Films sortis le 03/2020` |
| `/Movie/released/2020` | `Films sortis en 2020` (mois optionnel) |
| `/Movie/released/20/03` | 404 (l'année doit avoir 4 chiffres) |
| `/Movie/released/2020/13` | 404 (le mois doit être entre 1 et 12) |
| `/MovieCustomer/Index/1` | Un film et la liste de ses clients |
| `/Customer/CustomerDetails/1` | `Amal Ben Salah` |
| `/Customer/CustomerDetails/99` | `Client introuvable` |

## Réponses aux questions

### 1. Route `Movie/released/{year}/{month}`

L'action `ByRelease(int year, int? month)` retourne un `Content`.
- **Routage conventionnel** : route `MovieByRelease` déclarée dans `Program.cs` avant la route `default` (laissée en commentaire, voir question 4).
- **Routage par attributs** : `[Route(...)]` au-dessus de l'action dans `MovieController`.

### 2. Sans le mois : `/Movie/released/2020`

Si le mois est obligatoire dans la route, on obtient une **erreur 404** : aucune route ne correspond à l'URL.
Pour rendre le mois optionnel, il faut :
- ajouter `?` dans la route : `{month?}`
- rendre le paramètre nullable : `int? month`

### 3. Contraintes de type

```csharp
[Route("Movie/released/{year:int:length(4)}/{month:int:range(1,12)?}")]
```

- `year:int:length(4)` : entier de 4 chiffres exactement
- `month:int:range(1,12)` : entier entre 1 et 12

Si une contrainte n'est pas respectée, la route ne correspond pas : on obtient une 404.

### 4. Routage par attributs et `MapControllers()`

`Index`, `Edit` et `ByRelease` utilisent des attributs `[Route]`. Quand une action a un attribut `[Route]`, les routes conventionnelles sont ignorées pour cette action.

- `app.MapControllers()` active uniquement le routage par attributs.
- `app.MapControllerRoute()` ajoute une route conventionnelle et mappe aussi les contrôleurs qui ont des attributs.

Ici, `MapControllerRoute()` est déjà présent, donc les attributs fonctionnent même sans `MapControllers()` (vérifié en le retirant). `MapControllers()` devient obligatoire quand il n'y a aucune route conventionnelle, par exemple dans une Web API.

| Critère | Routage par attributs | Routage conventionnel |
| ------- | --------------------- | --------------------- |
| Lisibilité | La route est écrite au-dessus de l'action | Il faut aller voir `Program.cs` |
| Granularité | Une route précise par action, URL libres | Un seul modèle pour tous les contrôleurs |
| Maintenance | Routes dispersées dans chaque contrôleur | Une règle centralisée, rien à ajouter pour un nouveau contrôleur |
| Usage | Standard pour les Web API (REST) | Adapté aux applications MVC avec vues |

### 5. ViewModel `MovieCustomerViewModel`

Une vue Razor n'accepte qu'**un seul** `@model`. On ne peut donc pas écrire `@model Movie` et `@model List<Customer>` dans la même vue.

Le ViewModel est une classe faite pour la vue : il regroupe dans un seul objet toutes les données dont la page a besoin (le film et la liste des clients), avec un typage fort.

### 6. `CustomerDetails(int id)`

Recherche du client avec `FirstOrDefault` dans la liste statique. Si le client n'existe pas, on retourne `Content("Client introuvable")`.

### 7. Interfaces Razor

- **Liste des films** : tableau centré, colonnes ID / Nom / Image, lignes alternées, le nom est un lien vers les détails.
- **Détails du film** : image à gauche, carte avec l'ID et le nom alignés à droite.

Le CSS est écrit directement dans les vues (`<style>`).
