# VoxSign

Application .NET MAUI (MVVM) et son API ASP.NET Core.

| Projet | Rôle |
| --- | --- |
| `VoxSign` | Application mobile (Android, iOS, Windows) |
| `VoxSign.Api` | API : comptes, dictionnaire, phrases, traduction, favoris, packs hors ligne |
| `VoxSign.Shared` | Contrats d'échange, catalogue initial, algorithme texte → signes (commun à l'API et à l'application) |
| `VoxSign.Api.Tests` | Tests unitaires et d'intégration de l'API |

## Lancer en développement

```powershell
dotnet run --project VoxSign.Api            # http://localhost:5180 (base SQLite voxsign.db créée au démarrage)
dotnet build -t:Run -f net9.0-android VoxSign/VoxSign.csproj
dotnet test VoxSign.Api.Tests
```

- Compte de démonstration (créé en développement) : `danielacledya@gmail.fr` / `voxsign-2026`.
- L'émulateur Android joint le poste par `http://10.0.2.2:5180/`, les autres plateformes par `http://localhost:5180/`.
  Pour un téléphone physique, lancez l'API sur l'adresse du poste (`--urls http://0.0.0.0:5180`) et changez
  `ApiSettings.BaseUrl` (préférence `api_base_url`).
- Documentation OpenAPI en développement : `http://localhost:5180/openapi/v1.json`.

## Fonctionnement hors ligne

- L'application embarque le catalogue initial et les packs « Les essentiels » et « Au quotidien » : elle fonctionne dès la première ouverture, sans réseau.
- Le dictionnaire et les phrases sont copiés dans une base SQLite locale, rafraîchie dès que le serveur est joignable.
- La saisie texte est traduite par le serveur, ou sur l'appareil avec le même algorithme (`SignSequencer`).
- La synthèse vocale utilise les voix de l'appareil (préférences de voix enregistrées sur le compte).
- Favoris et phrases personnelles sont enregistrés immédiatement sur l'appareil puis envoyés au serveur
  (file d'attente rejouée au retour du réseau ; les appels de l'API sont idempotents).

## Mise en production de l'API

- Fournir une clé de signature : variable d'environnement `Jwt__SigningKey` (32 caractères minimum).
- Servir l'API en HTTPS (l'exception HTTP d'Android ne couvre que `10.0.2.2` et `localhost`).
- La base est créée avec `EnsureCreated` : passer aux migrations EF Core avant de faire évoluer le schéma en production.
