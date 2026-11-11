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
  `AccentColorKey`, `RobloxInstallation`, `RobloxInstallationState`, `RobloxBuildCandidate`,
  `RobloxIntegrityReport`, `RobloxProcessInfo`, `RobloxProcessTransition`, `RobloxLaunchResult`
  (issue `RobloxLaunchOutcome`), `ShellLaunchResult`.
- **Règles** : `ThemeResolver` (thème effectif), `RobloxVersionParser` (versions Windows
  `0, 747, 0, 7471481`), `RobloxBuildSelector` (dossier le plus récent), `AppSettingsNormalizer`
  (valeurs par défaut, schéma, énumérations inconnues), `RobloxLaunchUriBuilder` (URI officielle
  et locales validées), `RobloxIntegrityRules` (fichiers indispensables et recommandés),
  `RobloxProcessTracker` (comparaison de relevés → transitions), `RobloxWebUrls` (adresses
  officielles).
- **Abstractions** : `ISettingsService`, `ISettingsStore`, `IRobloxProbe`, `IThemeService`,
  `ISystemThemeSource`, `IProtocolRegistrationReader`, `IExecutableVersionReader`,
  `IFolderLauncher`, `IFileSystem`, `IAppPaths`, `IAppLogger`, `IApplicationVersionProvider`,
  `IShellLauncher`, `IRobloxLaunchService`, `IRobloxIntegrityChecker`, `IRobloxProcessSource`,
  `IRobloxProcessMonitor`.

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
- `WindowsRobloxIntegrityChecker` : lit le dossier de version détecté et délègue le verdict à
  `RobloxIntegrityRules`; les erreurs d'accès deviennent un avertissement journalisé.
- `WindowsShellLauncher` : transmet une cible au shell Windows (`ShellExecute`) — un protocole
  enregistré ou une URL — et rapporte l'échec plutôt que de le masquer.
- `WindowsRobloxLaunchService` : refuse de lancer si le client tourne déjà ou si l'installation
  est inutilisable, vérifie l'enregistrement du protocole `roblox-player`, puis remet l'URI
  `roblox-player:1+launchmode:app` au shell. Produit une `RobloxLaunchResult` explicite.
- `WindowsRobloxProcessSource` : releve `RobloxPlayerBeta` et conserve les handles pour pouvoir
  rapporter un code de sortie après fermeture. Seuls identité, démarrage, chemin et code de
  sortie sont lus.
- `WindowsRobloxProcessMonitor` : relevé périodique (2 s) sur un timer, comparaison par
  `RobloxProcessTracker`, publication de `Changed` hors du thread d'interface ; une erreur
  d'abonné est journalisée sans interrompre la surveillance.
- `WindowsThemeSource`, `RegistryProtocolReader`, `WindowsExecutableVersionReader`,
  `ExplorerFolderLauncher`, `WindowsAppPaths`, `SystemFileSystem`.

### Features

- `NavigationService` : navigation par route, pages créées une seule fois et conservées
  (l'état des pages survit à la navigation), événement `Navigated`.
- `ShellViewModel` : barre latérale, page courante, titre, mode compact selon la largeur.
- `DashboardViewModel` : expose **uniquement** des données issues de `IRobloxProbe`,
  `IRobloxIntegrityChecker`, `IRobloxLaunchService` et `IRobloxProcessMonitor`. États
  chargement / succès / avertissement / erreur, intégrité de l'installation, commandes
  Jouer / Installer ou réparer / Actualiser / Ouvrir le dossier / Paramètres. Après une demande
  de lancement, il attend l'observation réelle du processus (fenêtre de 25 s) avant d'annoncer
  que le client est ouvert ; les événements du moniteur sont ramenés sur le thread d'interface.
- `SettingsViewModel` : thème et accentuation, application immédiate puis persistance ; en cas
  d'échec d'écriture, la sélection précédente et le thème sont restaurés.

### App

- **Composition** : `App.xaml.cs` construit le `ServiceProvider` (Microsoft.Extensions.DependencyInjection),
  charge la configuration, applique le thème puis affiche la fenêtre. La source de processus et le
  moniteur sont des singletons libérés avec le conteneur.
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
5. Affichage de `MainWindow` puis `ShellViewModel.StartAsync()` → Dashboard : démarrage du
   moniteur de processus, détection de l'installation, vérification de l'intégrité.

## Décisions techniques

- **Aucune bibliothèque d'interface tierce** : le design system est écrit en XAML, ce qui évite
  les collisions de palettes et garde une identité visuelle propre, avec un coût de maintenance maîtrisé.
- **Mica natif non utilisé** : WPF ne l'expose pas ; un dégradé discret remplace un faux effet.
- **Lancement sans identifiants** : `launchmode:app` démarre le client officiel sans ticket, alors
  que `launchmode:play` exige un ticket délivré par le site Roblox dans la session du navigateur.
  LinkLauncher utilise donc uniquement `launchmode:app` et laisse le rejoignement d'expérience au
  navigateur : aucun cookie `.ROBLOSECURITY` n'est lu, demandé ou copié.
- **Surveillance minimale du client** : présence du processus, PID, démarrage, code de sortie.
  Aucune lecture de mémoire, aucun module, aucun fil, aucune injection — le client reste
  exactement celui de Roblox, ce qui préserve l'anti-triche.
- **Identifiant lu avant libération** : le PID est lu **avant** toute libération de l'objet
  `Process`, sinon `Process.Id` lève « No process is associated with this object » — défaut
  observé en test réel et couvert par un test de régression.
- **Handle conservé pour le code de sortie** : deux limites de .NET ont été constatées en test
  réel. `Process.ExitCode` refuse de répondre pour un processus démarré par un autre programme, et
  un handle ne peut plus être rouvert par PID une fois le processus terminé. `NativeProcess`
  ouvre donc un handle `PROCESS_QUERY_LIMITED_INFORMATION` dès la première observation, le
  conserve, puis lit le code de sortie par `GetExitCodeProcess`. Seul le code de sortie est lu ;
  un accès refusé dégrade l'affichage en « indisponible » sans erreur utilisateur.
- **Lancement confirmé par observation** : le ViewModel n'annonce pas « Roblox est ouvert » parce
  que le shell a accepté la demande, mais après la détection réelle du processus ; sinon il
  indique que le démarrage est en cours et que la surveillance continue.
- **Journal non verrouillé** : écriture ouverte/fermée par entrée, pour rester consultable.
- **Thème « Système »** : résolu au démarrage ; le suivi en direct des changements Windows
  pourra être ajouté avec un abonnement aux paramètres système.

## Prochaines étapes

Phase 3 : profils et gestion de la bibliothèque d'expériences, puis notifications et présence
Discord. `IRobloxLaunchService` et `IRobloxProcessMonitor` sont prêts à être réutilisés pour
encadrer un lancement depuis un profil, sans jamais manipuler d'identifiants Roblox.
