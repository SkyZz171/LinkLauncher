# Architecture — LinkLauncher

## Vue d'ensemble

L'application suit MVVM avec une séparation stricte des responsabilités. Les dépendances
pointent toujours vers l'intérieur : `App` → `Features` / `Infrastructure` → `Core`, jamais
l'inverse. `Core` ne référence aucune API WPF.

```
LinkLauncher.App             (WPF, net10.0-windows)
 ├─ LinkLauncher.Features    (ViewModels, navigation, net10.0)
 ├─ LinkLauncher.Infrastructure (Windows, disque, registre, Discord, net10.0-windows)
 └─ LinkLauncher.Updater     (publications GitHub, SHA-256, net10.0)
     └─ LinkLauncher.Core    (modèles, abstractions, règles, net10.0)
```

`Features` ne dépend que de `Core` : les ViewModels sont testables sans interface et sans Windows.

## Rôles des couches

### Core

Aucune dépendance technique. Contient :

- **Modèles** : `AppSettings` (record immuable, versionné par `SchemaVersion`), `AppTheme`,
  `AccentColorKey`, `RobloxInstallation`, `RobloxInstallationState`, `RobloxBuildCandidate`,
  `RobloxIntegrityReport`, `RobloxProcessInfo`, `RobloxProcessTransition`, `RobloxLaunchResult`
  (issue `RobloxLaunchOutcome`), `ShellLaunchResult`, `LaunchProfile`, `Experience`,
  `LauncherLibrary` (versionnée par `SchemaVersion`), `LibraryMutation`, `AppNotification`
  (issue `NotificationKind`), `DiscordActivity`, `DiscordPresenceState`, `DiagnosticsReport`,
  `AppVersion`, `UpdateRelease`, `UpdateCheckResult` (issue `UpdateCheckOutcome`),
  `UpdateDownloadResult`.
- **Règles** : `ThemeResolver` (thème effectif), `RobloxVersionParser` (versions Windows
  `0, 747, 0, 7471481`), `RobloxBuildSelector` (dossier le plus récent), `AppSettingsNormalizer`
  (valeurs par défaut, schéma, énumérations inconnues), `RobloxLaunchUriBuilder` (URI officielle
  et locales validées), `RobloxIntegrityRules` (fichiers indispensables et recommandés),
  `RobloxProcessTracker` (comparaison de relevés → transitions), `RobloxWebUrls` (adresses
  officielles), `RobloxExperienceLink` (interprétation stricte des liens d'expériences),
  `LauncherLibraryRules` (validation, normalisation et modifications de la bibliothèque),
  `AppNotificationRules` (transition → notification), `DiscordActivityRules` (activité et
  troncature à 128 caractères), `DiscordApplicationIdRules` (identifiant public à 17–20 chiffres),
  `DiscordIpcMessages` (trames binaires du canal IPC Discord), `UpdateRules` (sélection de la
  publication la plus récente, extraction et validation d'une empreinte SHA-256).
- **Abstractions** : `ISettingsService`, `ISettingsStore`, `IRobloxProbe`, `IThemeService`,
  `ISystemThemeSource`, `IProtocolRegistrationReader`, `IExecutableVersionReader`,
  `IFolderLauncher`, `IFileSystem`, `IAppPaths`, `IAppLogger`, `IApplicationVersionProvider`,
  `IShellLauncher`, `IRobloxLaunchService`, `IRobloxIntegrityChecker`, `IRobloxProcessSource`,
  `IRobloxProcessMonitor`, `ILibraryStore`, `ILauncherLibrary`, `IActiveProfileProvider`,
  `INotificationPresenter`, `IDiscordIpcTransport`, `IDiscordPresenceService`,
  `IDiagnosticsProvider`, `IClipboardService`, `IUpdateFeed`, `IUpdateChecker`,
  `IUpdateDownloader`.

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
  `roblox-player:1+launchmode:app` au shell. La langue provient du profil actif fourni par
  `IActiveProfileProvider`. Produit une `RobloxLaunchResult` explicite.
- `JsonLibraryStore` : conserve profils et expériences dans `library.json`, écriture atomique,
  fichier illisible conservé puis contenu normalisé ou recréé.
- `LauncherLibraryService` : état courant de la bibliothèque, mutations appliquées par les règles
  du domaine puis enregistrées, événement `Changed` après chaque écriture réussie ; implémente
  aussi `IActiveProfileProvider` pour le service de lancement.
- `WindowsRobloxProcessSource` : releve `RobloxPlayerBeta` et conserve les handles pour pouvoir
  rapporter un code de sortie après fermeture. Seuls identité, démarrage, chemin et code de
  sortie sont lus.
- `WindowsRobloxProcessMonitor` : relevé périodique (2 s) sur un timer, comparaison par
  `RobloxProcessTracker`, publication de `Changed` hors du thread d'interface ; une erreur
  d'abonné est journalisée sans interrompre la surveillance.
- `DiscordIpcClient` : pipe nommé `discord-ipc-0..9`, poignée de main `client_id`, trames
  binaires opcode + longueur + JSON ; aucune bibliothèque Discord et aucun accès réseau.
- `DiscordPresenceService` : n'expose une activité que si l'utilisateur l'a activée **et** si un
  identifiant d'application valide existe ; un Discord absent dégrade l'état en « indisponible »
  avec une seule ligne de journal.
- `WindowsDiagnosticsProvider` : agrège version de l'application, Windows, moteur .NET,
  architecture, chemins, détection Roblox, intégrité et dernier journal. Lecture seule.
- `WindowsThemeSource`, `RegistryProtocolReader`, `WindowsExecutableVersionReader`,
  `ExplorerFolderLauncher`, `WindowsAppPaths`, `SystemFileSystem`.

### Features

- `NavigationService` : navigation par route, pages créées une seule fois et conservées
  (l'état des pages survit à la navigation), événement `Navigated`.
- `ShellViewModel` : barre latérale, page courante, titre, mode compact selon la largeur.
- `LibraryViewModel` : présente les profils de lancement et les expériences ; délègue toute
  validation à `ILauncherLibrary` et n'ouvre que des pages officielles via `IShellLauncher`.
  Les commandes d'article reçoivent l'élément affiché en paramètre, sans closure dans la vue.
- `DashboardViewModel` : expose **uniquement** des données issues de `IRobloxProbe`,
  `IRobloxIntegrityChecker`, `IRobloxLaunchService` et `IRobloxProcessMonitor`. États
  chargement / succès / avertissement / erreur, intégrité de l'installation, commandes
  Jouer / Installer ou réparer / Actualiser / Ouvrir le dossier / Paramètres. Après une demande
  de lancement, il attend l'observation réelle du processus (fenêtre de 25 s) avant d'annoncer
  que le client est ouvert ; les événements du moniteur sont ramenés sur le thread d'interface.
- `SettingsViewModel` : thème et accentuation, application immédiate puis persistance ; en cas
  d'échec d'écriture, la sélection précédente et le thème sont restaurés. Expose aussi les
  notifications, la présence Discord (avec validation de l'identifiant), le diagnostic et la
  carte des mises à jour.
- `RobloxActivityNotifier` : abonné au moniteur de processus, transforme chaque transition en
  notification via `AppNotificationRules` et ne publie que si les notifications sont activées.
- `DiscordPresenceCoordinator` : abonné au moniteur, à la bibliothèque et à la configuration ;
  construit l'activité par `DiscordActivityRules` et la publie via `IDiscordPresenceService`.
- `UpdateViewModel` : recherche, téléchargement vérifié puis lancement de l'installeur par
  `IShellLauncher` ; aucune écriture dans les fichiers de l'application.

### App

- **Composition** : `App.xaml.cs` construit le `ServiceProvider` (Microsoft.Extensions.DependencyInjection),
  charge la configuration, applique le thème puis affiche la fenêtre. La source de processus et le
  moniteur sont des singletons libérés avec le conteneur.
- **Thème** : `WpfThemeService` remplace le dictionnaire `Themes/Dark.xaml` ou `Themes/Light.xaml`
  et redéfinit les pinceaux d'accentuation ; les styles utilisent `DynamicResource`, donc le
  changement est immédiat et cohérent sur toutes les pages.
- **Design system** : `Themes/Tokens.xaml` (typographie, rayons, espacements, dimensions, glyphes),
  `Themes/Controls/*` (boutons, cartes, champs de saisie, listes, barres de défilement,
  typographie). Aucune valeur visuelle arbitraire dans les vues.
- **Chrome** : `WindowChrome` sans caption système, barre de titre personnalisée, `WindowInterop`
  (coins arrondis Windows 11, bordure sombre DWM, ajustement à la zone de travail au maximiser).
- **Notifications** : `WpfNotificationPresenter` affiche une `NotificationWindow` maison
  (position dans la zone de travail, fermeture automatique) — aucun paquet UWP ni WinForms.
- **Presse-papiers** : `WpfClipboardService` isole l'API Windows et rapporte un échec plutôt
  que de le masquer (`COMException` capturée, message utilisateur).
- **Erreurs** : gestionnaires globaux (`DispatcherUnhandledException`, `AppDomain.UnhandledException`,
  `TaskScheduler.UnobservedTaskException`) → journal + message utilisateur, sans masquer l'incident.

## Flux de démarrage

1. Création du journal (`%LOCALAPPDATA%\LinkLauncher\logs`) et des gestionnaires d'exceptions.
2. Construction du conteneur d'injection de dépendances (`ValidateOnBuild`).
3. `ISettingsService.InitializeAsync()` : lecture JSON, normalisation, valeurs par défaut si besoin.
4. Application du thème (`ThemeResolver` + `IThemeService`).
5. `ILauncherLibrary.InitializeAsync()` : lecture de `library.json`, normalisation, profil actif disponible
   pour le service de lancement.
6. Démarrage de `RobloxActivityNotifier` et de `DiscordPresenceCoordinator` : ils s'abonnent au
   moniteur et n'agissent que selon les préférences enregistrées.
7. Affichage de `MainWindow` puis `ShellViewModel.StartAsync()` → Dashboard : démarrage du
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
- **Liens d'expériences stricts** : `RobloxExperienceLink` n'accepte que les hôtes officiels et
  les chemins `/games/<identifiant>` ; la page est reconstruite sous forme canonique
  `https://www.roblox.com/games/<identifiant>`. Une saisie ne peut donc pas détourner
  l'ouverture vers un autre site, et l'ouverture passe par le navigateur de l'utilisateur.
- **Profils sans identifiants** : un profil de lancement ne stocke qu'un nom et une langue,
  utilisée pour les paramètres `robloxLocale`/`gameLocale` du lancement sans session. Aucune
  donnée de session n'est jamais enregistrée dans `library.json`.
- **Notifications maison** : une fenêtre WPF dédiée évite WinForms (`NotifyIcon`) et les paquets
  UWP ; elle reste dans la zone de travail et se ferme seule.
- **Discord en IPC brut** : le protocole local (pipe nommé, opcode, JSON) est implémenté dans
  `Core`/`Infrastructure`, sans SDK tiers ; l'absence de Discord ou d'identifiant d'application
  laisse la présence inactive sans erreur utilisateur.
- **Mise à jour vérifiée, jamais appliquée en silence** : l'installeur n'est proposé qu'avec une
  empreinte SHA-256 publiée, recalculée après téléchargement ; un écart supprime le fichier et
  une publication sans empreinte est refusée. LinkLauncher lance l'installeur, il ne remplace
  jamais ses propres fichiers, et l'absence de signature Authenticode reste documentée.
- **Journal non verrouillé** : écriture ouverte/fermée par entrée, pour rester consultable.
- **Thème « Système »** : résolu au démarrage ; le suivi en direct des changements Windows
  pourra être ajouté avec un abonnement aux paramètres système.

## Limites assumées

- L'installeur n'est pas signé : la vérification repose sur l'empreinte SHA-256 publiée dans les
  notes de la release, pas sur un certificat Authenticode.
- La présence Discord dépend du client Discord installé et d'un identifiant d'application fourni
  par l'utilisateur ; sans l'un des deux, elle reste inactive.
- Le suivi du client se limite à ce que Windows expose publiquement (PID, démarrage, sortie).
- Rejoindre une expérience et réparer automatiquement l'installation Roblox restent hors
  périmètre : ces étapes passent par le site officiel et le navigateur.
