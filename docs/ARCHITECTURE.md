# Architecture — LinkLauncher

## Vue d'ensemble

L'application suit MVVM avec une séparation stricte des responsabilités. Les dépendances
pointent toujours vers l'intérieur : `App` → `Features` / `Infrastructure` → `Core`, jamais
l'inverse. `Core` ne référence aucune API WPF.

```
LinkLauncher.App             (WPF, net10.0-windows)
 ├─ LinkLauncher.Features    (ViewModels, navigation, net10.0)
 └─ LinkLauncher.Infrastructure (Windows, disque, registre, net10.0-windows)
     └─ LinkLauncher.Core    (modèles, abstractions, règles, net10.0)
```

`Features` ne dépend que de `Core` : les ViewModels sont testables sans interface et sans Windows.

## Rôles des couches

### Core

Aucune dépendance technique. Contient :

- **Modèles** : `AppSettings` (record immuable, versionné par `SchemaVersion`), `AppTheme`,
  `AccentColorKey`, `RobloxInstallation`, `RobloxInstallationState`, `RobloxBuildCandidate`.
- **Règles** : `ThemeResolver` (thème effectif), `RobloxVersionParser` (versions Windows
  `0, 747, 0, 7471481`), `RobloxBuildSelector` (dossier le plus récent), `AppSettingsNormalizer`
  (valeurs par défaut, schéma, énumérations inconnues).
- **Abstractions** : `ISettingsService`, `ISettingsStore`, `IRobloxProbe`, `IThemeService`,
  `ISystemThemeSource`, `IProtocolRegistrationReader`, `IExecutableVersionReader`,
  `IFolderLauncher`, `IFileSystem`, `IAppPaths`, `IAppLogger`, `IApplicationVersionProvider`.

### Infrastructure

Implémentations techniques :

- `JsonSettingsStore` : écriture atomique (fichier temporaire puis remplacement), sauvegarde
  `.bak` de la version précédente, sérialisation JSON lisible (énumérations en texte), fichier
  illisible conservé puis valeurs par défaut, accès sérialisé par `SemaphoreSlim`.
- `SettingsService` : état courant en mémoire, publication de `Changed`, mutations par fonction pure.
- `FileAppLogger` : file d'attente non bloquante (`Channel`), écriture par ajout ouvert/fermé
  pour que le fichier reste lisible, exception et contexte inclus, rotation quotidienne.
- `WindowsRobloxProbe` : énumération de `%LOCALAPPDATA%\Roblox\Versions`, présence de
  `RobloxPlayerBeta.exe`, version lue dans les métadonnées, recoupement avec le protocole
  `roblox-player` du registre utilisateur. Ne modifie, n'injecte et ne lance rien.
- `WindowsThemeSource`, `RegistryProtocolReader`, `WindowsExecutableVersionReader`,
  `ExplorerFolderLauncher`, `WindowsAppPaths`, `SystemFileSystem`.

### Features

- `NavigationService` : navigation par route, pages créées une seule fois et conservées
  (l'état des pages survit à la navigation), événement `Navigated`.
- `ShellViewModel` : barre latérale, page courante, titre, mode compact selon la largeur.
- `DashboardViewModel` : expose **uniquement** des données issues de `IRobloxProbe`, avec états
  chargement / succès / avertissement / erreur, commande de rafraîchissement et ouverture du dossier.
- `SettingsViewModel` : thème et accentuation, application immédiate puis persistance ; en cas
  d'échec d'écriture, la sélection précédente et le thème sont restaurés.

### App

- **Composition** : `App.xaml.cs` construit le `ServiceProvider` (Microsoft.Extensions.DependencyInjection),
  charge la configuration, applique le thème puis affiche la fenêtre. Aucune logique métier.
- **Thème** : `WpfThemeService` remplace le dictionnaire `Themes/Dark.xaml` ou `Themes/Light.xaml`
  et redéfinit les pinceaux d'accentuation ; les styles utilisent `DynamicResource`, donc le
  changement est immédiat et cohérent sur toutes les pages.
- **Design system** : `Themes/Tokens.xaml` (typographie, rayons, espacements, dimensions, glyphes),
  `Themes/Controls/*` (boutons, cartes, listes, barres de défilement, typographie). Aucune valeur
  visuelle arbitraire dans les vues.
- **Chrome** : `WindowChrome` sans caption système, barre de titre personnalisée, `WindowInterop`
  (coins arrondis Windows 11, bordure sombre DWM, ajustement à la zone de travail au maximiser).
- **Erreurs** : gestionnaires globaux (`DispatcherUnhandledException`, `AppDomain.UnhandledException`,
  `TaskScheduler.UnobservedTaskException`) → journal + message utilisateur, sans masquer l'incident.

## Flux de démarrage

1. Création du journal (`%LOCALAPPDATA%\LinkLauncher\logs`) et des gestionnaires d'exceptions.
2. Construction du conteneur d'injection de dépendances (`ValidateOnBuild`).
3. `ISettingsService.InitializeAsync()` : lecture JSON, normalisation, valeurs par défaut si besoin.
4. Application du thème (`ThemeResolver` + `IThemeService`).
5. Affichage de `MainWindow` puis `ShellViewModel.StartAsync()` → Dashboard → `IRobloxProbe`.

## Décisions techniques

- **Aucune bibliothèque d'interface tierce** : le design system est écrit en XAML, ce qui évite
  les collisions de palettes et garde une identité visuelle propre, avec un coût de maintenance maîtrisé.
- **Mica natif non utilisé** : WPF ne l'expose pas ; un dégradé discret remplace un faux effet.
- **Pas de lancement Roblox en phase 1** : le mécanisme officiel exige un ticket d'authentification.
  Le bouton est désactivé et annoté plutôt que simulé.
- **Journal non verrouillé** : écriture ouverte/fermée par entrée, pour rester consultable.
- **Thème « Système »** : résolu au démarrage ; le suivi en direct des changements Windows
  pourra être ajouté avec un abonnement aux paramètres système.

## Prochaines étapes

Phase 2 (launcher core) : installation et réparation via les mécanismes officiels, lancement du
client officiel avec gestion des erreurs, suivi du processus. La couche actuelle est prête :
`IRobloxProbe` sera complété par `IRobloxLaunchService` et `IRobloxDiagnosticsService`.
