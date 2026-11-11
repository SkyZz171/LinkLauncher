# LinkLauncher

Launcher Roblox indépendant pour Windows, écrit en C# / .NET 10 / WPF selon une architecture MVVM.

LinkLauncher utilise **le client Roblox officiel** : il ne modifie aucun binaire, n'injecte rien,
ne lit aucun identifiant Roblox et ne contourne aucun mécanisme de sécurité ou d'anti-triche.
Le projet n'est pas affilié à Roblox Corporation.

## État actuel — Phase 3 (bibliothèque d'expériences et profils de lancement)

Fonctionnalités réellement opérationnelles dans cette version :

| Fonctionnalité | État |
| --- | --- |
| Fenêtre principale avec chrome personnalisé (réduire / agrandir / fermer, déplacement) | Opérationnel |
| Navigation latérale (Dashboard, Paramètres) avec conservation de l'état des pages | Opérationnel |
| Dashboard : détection réelle de l'installation Roblox, version du client, dossier de version | Opérationnel |
| Dashboard : « Ouvrir le dossier Roblox » dans l'explorateur | Opérationnel |
| Dashboard : vérification des fichiers indispensables au lancement (intégrité) | Opérationnel |
| Lancement du client Roblox officiel (« Jouer ») via le protocole `roblox-player` | Opérationnel |
| Suivi du client : présence du processus, PID, heure de démarrage, code de sortie | Opérationnel |
| Gestion des erreurs de lancement (client absent, protocole non enregistré, échec du shell) | Opérationnel |
| « Installer ou réparer » : ouverture de la page officielle roblox.com/download | Opérationnel |
| Thèmes clair / sombre / système, appliqués à chaud et persistés | Opérationnel |
| Couleur d'accentuation (6 palettes), appliquée à chaud et persistée | Opérationnel |
| Configuration JSON avec sauvegarde, valeurs par défaut et résistance à la corruption | Opérationnel |
| Journalisation fichier quotidienne | Opérationnel |
| Bibliothèque d'expériences : ajout par lien officiel ou identifiant de lieu, retrait, date de dernière ouverture | Opérationnel |
| Profils de lancement : nom et langue transmise au client, profil actif appliqué à « Jouer » | Opérationnel |
| Rejoindre une expérience depuis LinkLauncher | **Non implémenté** — le navigateur s'en charge |
| Installation / réparation automatisée, Discord RPC, notifications, mises à jour, installateur | Non implémenté — phases suivantes |

### Comment le lancement fonctionne

Le bouton « Jouer » transmet à Windows l'URI officielle `roblox-player:1+launchmode:app`, celle
que Roblox enregistre pour son propre client. Ce mode **démarre le client sans session** : aucun
ticket, cookie ou identifiant n'est nécessaire, et LinkLauncher n'en lit ni n'en produit aucun.

Rejoindre une expérience exige un ticket d'authentification que seul le site Roblox délivre dans
la session du navigateur de l'utilisateur. LinkLauncher ne l'imite pas : cette étape reste dans
le navigateur, et le Dashboard le dit explicitement.

Le suivi se limite à la présence du processus `RobloxPlayerBeta`, à son PID, à son heure de
démarrage et au code de sortie rapporté par Windows. Aucune mémoire, aucun module et aucun fil
du client ne sont examinés ou modifiés.

### Bibliothèque et profils

La page **Bibliothèque** conserve une liste d'expériences et un ensemble de profils de lancement.
Une expérience n'est qu'un nom et l'adresse de sa page officielle : LinkLauncher accepte
`https://www.roblox.com/games/<identifiant>/…` ou directement l'identifiant de lieu, refuse tout
autre site, et ouvre la page dans votre navigateur — c'est Roblox qui gère la connexion et le
lancement de la partie.

Un profil de lancement ne contient qu'un nom et une langue (`fr_FR`, `en-US`…). Le profil actif
est transmis au client comme paramètre `robloxLocale`/`gameLocale` lors du lancement sans session :
aucun identifiant, aucun cookie et aucun jeton ne sont enregistrés, ni dans la bibliothèque ni
ailleurs.

## Prérequis

- Windows 10 ou 11
- SDK .NET 10 (`dotnet --version` doit répondre `10.0.4xx`)
- Client Roblox officiel installé par le site de Roblox

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
  LinkLauncher.Infrastructure/  JSON, fichiers, registre Windows, journalisation, Roblox
  LinkLauncher.Features/        ViewModels, navigation, logique de présentation
tests/
  LinkLauncher.Core.Tests/             Règles métier, normalisation, navigation, lancement, bibliothèque
  LinkLauncher.Infrastructure.Tests/   Persistance, journalisation, détection, lancement, intégrité
```

`LinkLauncher.Updater` (phase 5) et `LinkLauncher.Integration.Tests` (phase 6) seront ajoutés
lorsqu'ils contiendront du code réel : aucun projet vide n'est conservé dans la solution.

## Données utilisateur

| Contenu | Emplacement |
| --- | --- |
| Configuration | `%APPDATA%\LinkLauncher\settings.json` |
| Sauvegarde de la configuration | `%APPDATA%\LinkLauncher\settings.bak.json` |
| Configuration illisible conservée | `%APPDATA%\LinkLauncher\settings.invalide-<horodatage>.json` |
| Bibliothèque (profils et expériences) | `%APPDATA%\LinkLauncher\library.json` |
| Bibliothèque illisible conservée | `%APPDATA%\LinkLauncher\library.invalide-<horodatage>.json` |
| Journaux d'activité | `%LOCALAPPDATA%\LinkLauncher\logs\linklauncher-AAAAMMJJ.log` |

Aucun cookie, jeton ou identifiant Roblox n'est lu ni stocké.

## Documentation

- [Architecture](docs/ARCHITECTURE.md) — couches, dépendances, décisions techniques.
- [Tests manuels](docs/MANUAL-TESTS.md) — scénarios de validation des phases 1 à 3.
