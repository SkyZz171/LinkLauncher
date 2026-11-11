# Tests manuels — Phases 1 et 2

Scénarios à valider sur une machine Windows. Cocher après exécution réelle.

## Démarrage

- [ ] Premier démarrage : la fenêtre s'ouvre, le Dashboard affiche un état réel.
- [ ] `%APPDATA%\LinkLauncher\settings.json` est créé dès le premier changement de préférence.
- [ ] Redémarrage : le thème et la couleur d'accentuation choisis sont restaurés.
- [ ] `%LOCALAPPDATA%\LinkLauncher\logs\linklauncher-<date>.log` contient les lignes de démarrage.

## Interface

- [ ] Réduire, agrandir, restaurer et fermer fonctionnent.
- [ ] Double-clic sur la barre de titre : bascule agrandi / restauré.
- [ ] Maximiser ne recouvre pas la barre des tâches.
- [ ] Navigation Dashboard ↔ Paramètres : la page précédente conserve son état.
- [ ] Fenêtre réduite sous ~960 px : la barre latérale passe en mode icônes.
- [ ] Navigation clavier : Tab atteint les boutons (Actualiser, Jouer, Ouvrir le dossier Roblox,
      Installer ou réparer), Entrée les active, le focus est visible.

## Thèmes

- [ ] Thème sombre : contrastes lisibles sur toutes les pages.
- [ ] Thème clair : contrastes lisibles sur toutes les pages.
- [ ] Thème système : reprend le réglage Windows au démarrage.
- [ ] Changement d'accentuation : appliqué immédiatement partout, sans redémarrage.

## Détection et intégrité

- [ ] Roblox installé : état « Roblox est prêt », version et dossier affichés.
- [ ] Roblox supprimé ou profil vierge : état « Roblox n'est pas installé », sans erreur.
- [ ] Dossier `Versions` présent mais sans `RobloxPlayerBeta.exe` : état « Installation incomplète ».
- [ ] Pastille d'intégrité « Complète » et message « Les fichiers indispensables au lancement
      sont présents. » sur une installation saine.
- [ ] Bibliothèque renommée (`RobloxPlayerBeta.dll`) : pastille « Incomplète », fichier listé,
      bouton « Jouer » désactivé.
- [ ] « Ouvrir le dossier Roblox » ouvre bien le dossier détecté.

## Lancement officiel

- [ ] Client Roblox fermé : « Jouer » est actif, la carte « Client Roblox » indique « Client arrêté ».
- [ ] Clic sur « Jouer » : le client Roblox officiel s'ouvre (fenêtre Roblox), sans demande de mot de passe.
- [ ] Après le lancement, la carte « Client Roblox » passe à « Roblox est ouvert » avec le PID et
      l'heure de démarrage réels.
- [ ] Aucune oscillation « ouvert / arrêté » pendant que le client tourne.
- [ ] Le journal contient une seule ligne « Client Roblox observé (PID …) » par lancement, sans
      avertissement « Processus Roblox illisible ignoré ».
- [ ] Fermeture du client : la carte repasse à « Client arrêté » avec le code de sortie rapporté
      par Windows (ou « indisponible »).
- [ ] « Jouer » alors que le client tourne déjà : message « Client déjà en cours d'exécution »,
      aucun second processus lancé.
- [ ] « Installer ou réparer (site officiel) » ouvre `roblox.com/download` dans le navigateur par défaut.

## Erreurs de lancement

- [ ] Roblox non installé : « Jouer » est désactivé, sans erreur affichée.
- [ ] Association `roblox-player` supprimée du registre utilisateur : clic sur « Jouer » →
      message expliquant l'absence d'association, aucune exception non gérée.
- [ ] Navigateur par défaut indisponible : « Installer ou réparer » affiche un message clair et
      journalise l'échec.

## Robustesse

- [ ] `settings.json` corrompu à la main : l'application démarre, un fichier
      `settings.invalide-<horodatage>.json` est conservé, les valeurs par défaut s'appliquent.
- [ ] Dossier `%APPDATA%\LinkLauncher` en lecture seule : message d'erreur compréhensible,
      l'application reste utilisable.
- [ ] Fermeture pendant une vérification ou pendant un lancement : aucune exception non gérée.
- [ ] Double-clic rapide sur « Jouer » : une seule demande transmise (bouton neutralisé pendant
      l'attente).
