# Publier une release — LinkLauncher

Procédure à suivre pour qu'une nouvelle version soit réellement détectée, téléchargée et vérifiée
par la carte **Paramètres → Mises à jour**.

La carte lit `https://api.github.com/repos/SkyZz171/LinkLauncher/releases` sans authentification
(constante `GitHubReleasesUpdateFeed.RepositorySlug`). Une publication n'est proposée que si elle
remplit **toutes** les conditions suivantes :

1. elle n'est pas un **brouillon** ;
2. son étiquette (`tag_name`) est une version lisible : `v0.2.0`, `0.2.0`, `0.2.0.1`… ;
3. elle contient un fichier joint dont le nom se termine par `.exe` (l'installeur) ;
4. ses notes contiennent une empreinte **SHA-256** de 64 caractères hexadécimaux, par exemple
   `SHA-256 : 73ce01c0…` ;
5. sa version est **supérieure** à la version installée.

Sans empreinte, la publication est listée mais le bouton « Télécharger » reste inactif : le
téléchargeur refuse par principe tout paquet non vérifiable.

## 1. Choisir la version

Deux endroits doivent rester cohérents :

| Fichier | Rôle |
| --- | --- |
| `src/LinkLauncher.App/LinkLauncher.App.csproj` (`<Version>`) | version annoncée par l'application, affichée dans les Paramètres et comparée par la carte Mises à jour |
| `installer/LinkLauncher.iss` (`#define AppVersion`) | valeur par défaut si `build.ps1` ne transmet pas `-Version` |

`installer/build.ps1 -Version 0.2.0` remplace ces deux valeurs au moment de la compilation
(publication `dotnet publish -p:Version=0.2.0` et `/DAppVersion=0.2.0` pour Inno Setup), sans
modifier les fichiers du dépôt.

## 2. Compiler le paquet

```powershell
installer\build.ps1 -Version 0.2.0 -Installer
```

Le script publie `artifacts\publish\LinkLauncher.exe`, compile l'installeur puis affiche :

```
Installeur : ...\artifacts\installer\LinkLauncher-0.2.0-setup.exe
SHA-256    : 73ce01c034ef204504cbbca0db9e37dd1640f337da327f122d1be5c20c600152
```

L'empreinte affichée est celle du **setup.exe**, jamais celle de `LinkLauncher.exe` : c'est le
fichier joint à la release que LinkLauncher recalcule après téléchargement.

## 3. Vérifier le paquet avant publication

```powershell
# Installation silencieuse dans un dossier de test
$target = Join-Path $env:TEMP 'LinkLauncherInstallTest'
Start-Process artifacts\installer\LinkLauncher-0.2.0-setup.exe `
    -ArgumentList "/VERYSILENT","/SUPPRESSMSGBOXES","/NORESTART",'/SP-',"/DIR=`"$target`"" -Wait

# Lancer, vérifier Paramètres → Mises à jour, puis désinstaller
Start-Process (Join-Path $target 'LinkLauncher.exe')
Start-Process (Join-Path $target 'unins000.exe') -ArgumentList '/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART' -Wait
```

Contrôles attendus : la fenêtre s'ouvre, la version affichée dans Paramètres correspond, la
désinstallation retire l'application et ses raccourcis, et `%APPDATA%\LinkLauncher` survit.

## 4. Créer la release GitHub

Interface web : **Releases → Draft a new release**.

- **Tag** : `v0.2.0` (créer le tag sur `main`).
- **Titre** : `LinkLauncher 0.2.0`.
- **Fichier joint** : `LinkLauncher-0.2.0-setup.exe`, renommé sans espaces ni accents.
- **Notes** : résumé des changements **et** l'empreinte, sur sa propre ligne.

Exemple de notes acceptées :

```markdown
## Nouveautés

- Notifications locales au démarrage du client Roblox.
- Diagnostic copiable dans le presse-papiers.

## Vérification du fichier

SHA-256 : 73ce01c034ef204504cbbca0db9e37dd1640f337da327f122d1be5c20c600152
```

L'extraction tolère les formats `SHA-256 : <empreinte>`, `sha256=<empreinte>` et une empreinte
seule entre accents graves ; un texte sans empreinte de 64 caractères hexadécimaux est refusé.

Équivalent en ligne de commande, si `gh` est installé :

```powershell
$setup = 'artifacts\installer\LinkLauncher-0.2.0-setup.exe'
$hash = (Get-FileHash -Algorithm SHA256 $setup).Hash.ToLowerInvariant()

gh release create v0.2.0 $setup `
    --title "LinkLauncher 0.2.0" `
    --notes "## Nouveautés`n`n- ...`n`n## Vérification du fichier`n`nSHA-256 : $hash"
```

**Publier la release** (ne pas la laisser en brouillon) : un brouillon n'est jamais proposé par
la carte Mises à jour.

## 5. Contrôler depuis LinkLauncher

1. Ouvrir **Paramètres → Mises à jour**, cliquer sur **Rechercher** : la version publiée doit
   apparaître (« Version 0.2.0 »), sinon lire le message d'état.
2. Cliquer sur **Télécharger** : le fichier arrive dans
   `%LOCALAPPDATA%\LinkLauncher\updates\0.2.0\`, le statut annonce l'empreinte vérifiée.
3. Cliquer sur **Installer** : l'installeur vérifié est lancé par Windows, LinkLauncher ne
   remplace aucun de ses fichiers lui-même.

## Dépannage

| Symptôme | Cause probable |
| --- | --- |
| « LinkLauncher x.y.z est à jour. » alors qu'une release existe | version du tag non supérieure, release en brouillon, ou uniquement des fichiers `.zip` |
| « Cette publication ne fournit pas d'empreinte SHA-256 » | notes sans empreinte de 64 caractères hexadécimaux |
| « Impossible de joindre le dépôt des publications » | pas de réseau, ou quota GitHub épuisé (60 requêtes par heure et par adresse IP, sans authentification) |
| « L'empreinte du paquet ne correspond pas » | fichier joint remplacé après calcul, empreinte erronée, ou notes mises à jour sans reconstruire le paquet |
| Le téléchargement fonctionne mais l'installation échoue | paquet corrompu côté GitHub, ou droits insuffisants pour le dossier choisi |

## Limites assumées

- L'installeur n'est pas signé Authenticode : SmartScreen avertit au premier lancement, et la
  confiance repose uniquement sur l'empreinte publiée.
- GitHub peut modifier un fichier joint (suppression puis réenvoi) : l'empreinte doit être
  recalculée à chaque remplacement, sinon les utilisateurs verront un refus de téléchargement.
- La vérification ne protège pas contre une publication malveillante faite avec un accès au
  dépôt : elle garantit l'intégrité du transfert, pas l'identité de l'auteur.
