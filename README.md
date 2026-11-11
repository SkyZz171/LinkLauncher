# LinkLauncher

Launcher Roblox indépendant pour Windows, écrit en C# / .NET 10 / WPF selon une architecture MVVM.

LinkLauncher utilise **le client Roblox officiel** : il ne modifie aucun binaire, n'injecte rien,
ne lit aucun identifiant Roblox et ne contourne aucun mécanisme de sécurité ou d'anti-triche.
Le projet n'est pas affilié à Roblox Corporation.

## État actuel — Phase 1 (fondations)

Fonctionnalités réellement opérationnelles dans cette version :

| Fonctionnalité | État |
| --- | --- |
| Fenêtre principale avec chrome personnalisé (réduire / agrandir / fermer, déplacement) | Opérationnel |
| Navigation latérale (Dashboard, Paramètres) avec conservation de l'état des pages | Opérationnel |
| Dashboard : détection réelle de l'installation Roblox, version du client, dossier de version | Opérationnel |
| Dashboard : « Ouvrir le dossier Roblox » dans l'explorateur | Opérationnel |
| Thèmes clair / sombre / système, appliqués à chaud et persistés | Opérationnel |
| Couleur d'accentuation (6 palettes), appliquée à chaud et persistée | Opérationnel |
| Configuration JSON avec sauvegarde, valeurs par défaut et résistance à la corruption | Opérationnel |
| Journalisation fichier quotidienne | Opérationnel |
| Lancement du client Roblox (« Jouer ») | **Non implémenté — phase 2** (bouton désactivé et expliqué) |
| Installation / réparation Roblox | Non implémenté — phase 2 |
| Profils, Discord RPC, notifications, mises à jour, diagnostics UI, installateur | Non implémenté — phases suivantes |

Le bouton « Jouer » est volontairement désactivé : lancer le client officiel exige une session
Roblox authentifiée, ce qui relève de la phase 2. Aucun faux indicateur n'est affiché.

## Prérequis

- Windows 10 ou 11
- SDK .NET 10 (`dotnet --version` doit répondre `10.0.4xx`)

## Compiler et tester

```bash
dotnet restore
dotnet build LinkLauncher.slnx -c Release
dotnet test LinkLauncher.slnx -c Release
dotnet run --project src/LinkLauncher.App
```

## Structure

```
src/
  LinkLauncher.App/             Interface WPF : composition, fenêtre, vues, design system
  LinkLauncher.Core/            Modèles, abstractions et règles métier (sans dépendance WPF)
  LinkLauncher.Infrastructure/  JSON, fichiers, registre Windows, journalisation, détection Roblox
  LinkLauncher.Features/        ViewModels, navigation, logique de présentation
tests/
  LinkLauncher.Core.Tests/             Règles métier, normalisation, navigation
  LinkLauncher.Infrastructure.Tests/   Persistance JSON, journalisation, détection Roblox
```

`LinkLauncher.Updater` (phase 5) et `LinkLauncher.Integration.Tests` (phase 6) seront ajoutés
lorsqu'ils contiendront du code réel : aucun projet vide n'est conservé dans la solution.

## Données utilisateur

| Contenu | Emplacement |
| --- | --- |
| Configuration | `%APPDATA%\LinkLauncher\settings.json` |
| Sauvegarde de la configuration | `%APPDATA%\LinkLauncher\settings.bak.json` |
| Configuration illisible conservée | `%APPDATA%\LinkLauncher\settings.invalide-<horodatage>.json` |
| Journaux d'activité | `%LOCALAPPDATA%\LinkLauncher\logs\linklauncher-AAAAMMJJ.log` |

Aucun cookie, jeton ou identifiant Roblox n'est lu ni stocké.

## Documentation

- [Architecture](docs/ARCHITECTURE.md) — couches, dépendances, décisions techniques.
- [Tests manuels](docs/MANUAL-TESTS.md) — scénarios de validation de la phase 1.
