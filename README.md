# LinkLauncher

Launcher Roblox indépendant pour Windows, écrit en C# / .NET 10 / WPF selon une architecture MVVM.

LinkLauncher utilise **le client Roblox officiel** : il ne modifie aucun binaire, n'injecte rien,
ne lit aucun identifiant Roblox et ne contourne aucun mécanisme de sécurité ou d'anti-triche.
Le projet n'est pas affilié à Roblox Corporation.

## État actuel — Phase 6 (notifications, Discord, diagnostic, mises à jour et installeur)

Fonctionnalités réellement opérationnelles dans cette version :

| Fonctionnalité | État |
| --- | --- |
| Fenêtre principale avec chrome personnalisé (réduire / agrandir / fermer, déplacement) | Opérationnel |
| Navigation latérale (Dashboard, Bibliothèque, Activité, Paramètres) avec conservation de l'état des pages | Opérationnel |
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
| Activité : sessions observées, temps de jeu du jour, sur 7 jours et depuis le début | Opérationnel |
| Notifications locales au démarrage et à la fermeture du client Roblox (y compris lancé depuis le navigateur) | Opérationnel, désactivables |
| Présence Discord (activité LinkLauncher et client Roblox) via le client Discord local | Opérationnel une fois un identifiant d'application fourni |
| Rapport de diagnostic (Windows, .NET, chemins, Roblox, intégrité, dernier journal) et copie dans le presse-papiers | Opérationnel |
| Recherche de mise à jour sur les publications GitHub, vérification SHA-256, téléchargement et lancement de l'installeur | Opérationnel |
| Installeur Windows (Inno Setup) avec raccourcis et désinstallation | Opérationnel |
| Rejoindre une expérience depuis LinkLauncher | **Non implémenté** — le navigateur s'en charge |
| Installation ou réparation automatisée du client Roblox | **Non implémenté** — redirige vers le site officiel |
| Signature de code de l'installeur | Non fournie — l'empreinte SHA-256 publiée est vérifiée à la place |

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

### Notifications, présence Discord et diagnostic

Les notifications sont des fenêtres WPF maison, sans dépendance tierce : elles signalent le
démarrage et la fermeture du client Roblox observés par le moniteur de processus, donc aussi
lorsque le client a été lancé depuis un navigateur. Elles sont activées par défaut et se coupent
depuis les Paramètres.

La présence Discord utilise le canal IPC local du client Discord (`discord-ipc-0` à `-9`), sans
bibliothèque et sans accès réseau. Elle reste **inactive** tant qu'aucun identifiant
d'application Discord (17 à 20 chiffres) n'est enregistré : cet identifiant est public, se crée
sur `discord.com/developers/applications`, et aucun jeton, secret ou donnée de compte Roblox
n'est demandé ni stocké. Si Discord n'est pas démarré, la présence est simplement indisponible et
le journal le mentionne une fois.

Le rapport de diagnostic rassemble la version de LinkLauncher, Windows, le moteur .NET,
l'architecture, les chemins des données et des journaux, la version et le dossier du client
Roblox, le verdict d'intégrité et le dernier journal. Il ne contient aucun identifiant et peut
être copié dans le presse-papiers pour un rapport de bogue.

### Mises à jour

LinkLauncher interroge les publications de `github.com/SkyZz171/LinkLauncher`. Une publication
n'est proposée que si elle contient un installeur `.exe` **et** une empreinte SHA-256 dans ses
notes ; le paquet est téléchargé dans `%LOCALAPPDATA%\LinkLauncher\updates`, son empreinte est
recalculée puis comparée, et un fichier différent est supprimé. LinkLauncher ne remplace jamais
ses propres fichiers : il lance l'installeur vérifié et l'utilisateur garde la main.

## Prérequis

- Windows 10 version 1809 ou ultérieure, ou Windows 11
- SDK .NET 10 (`dotnet --version` doit répondre `10.0.4xx`) pour compiler
- Client Roblox officiel installé par le site de Roblox
- Inno Setup 6 uniquement pour fabriquer l'installeur

## Compiler et tester

```bash
dotnet restore
dotnet build LinkLauncher.slnx -c Release
dotnet test LinkLauncher.slnx -c Release
dotnet run --project src/LinkLauncher.App
```

## Publier et installer

```powershell
# Publication autonome (win-x64, fichier unique) dans artifacts\publish
installer\build.ps1

# Publication puis compilation de l'installeur Inno Setup dans artifacts\installer
installer\build.ps1 -Installer
installer\build.ps1 -Version 0.2.0 -Installer
```

`build.ps1` affiche l'empreinte SHA-256 de l'installeur : reportez-la dans les notes de la
publication GitHub, c'est elle que LinkLauncher vérifie avant d'installer une mise à jour.

L'icône de l'application est générée par `tools\make-icon.ps1` vers
`src\LinkLauncher.App\Assets\linklauncher.ico`.

## Structure

```
src/
  LinkLauncher.App/             Interface WPF : composition, fenêtre, vues, design system
  LinkLauncher.Core/            Modèles, abstractions et règles métier (sans dépendance WPF)
  LinkLauncher.Infrastructure/  JSON, fichiers, registre Windows, journalisation, Roblox, Discord
  LinkLauncher.Features/        ViewModels, navigation, logique de présentation
  LinkLauncher.Updater/         Publications GitHub, vérification SHA-256, téléchargement
tests/
  LinkLauncher.Core.Tests/             Règles métier, normalisation, versions, trames IPC
  LinkLauncher.Infrastructure.Tests/   Persistance, journalisation, détection, lancement, diagnostic
  LinkLauncher.Integration.Tests/      Cycle complet des préférences et de la mise à jour
installer/
  LinkLauncher.iss              Script Inno Setup
  build.ps1                     Publication et compilation de l'installeur
tools/
  make-icon.ps1                 Génération de l'icône
```

## Données utilisateur

| Contenu | Emplacement |
| --- | --- |
| Configuration | `%APPDATA%\LinkLauncher\settings.json` |
| Sauvegarde de la configuration | `%APPDATA%\LinkLauncher\settings.bak.json` |
| Configuration illisible conservée | `%APPDATA%\LinkLauncher\settings.invalide-<horodatage>.json` |
| Bibliothèque (profils et expériences) | `%APPDATA%\LinkLauncher\library.json` |
| Historique des sessions Roblox | `%APPDATA%\LinkLauncher\activity.json` |
| Bibliothèque illisible conservée | `%APPDATA%\LinkLauncher\library.invalide-<horodatage>.json` |
| Paquets de mise à jour vérifiés | `%LOCALAPPDATA%\LinkLauncher\updates\<version>\` |
| Journaux d'activité | `%LOCALAPPDATA%\LinkLauncher\logs\linklauncher-AAAAMMJJ.log` |

Aucun cookie, jeton ou identifiant Roblox n'est lu ni stocké. Le désinstalleur ne supprime pas
ces dossiers : vos préférences et votre bibliothèque survivent à une désinstallation.

## Documentation

- [Architecture](docs/ARCHITECTURE.md) — couches, dépendances, décisions techniques.
- [Tests manuels](docs/MANUAL-TESTS.md) — scénarios de validation des phases 1 à 6.
- [Publier une release](docs/RELEASE.md) — paquet à joindre, notes avec empreinte SHA-256, contrôle
  depuis la carte « Mises à jour ».
- [Parité avec Bloxstrap](docs/BLOXSTRAP-PARITY.md) — fonctionnalités reprises, et refus assumés
  (FastFlags, multi-instance, mods, injection).
