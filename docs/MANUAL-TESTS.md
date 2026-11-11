# Tests manuels — Phase 1

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
- [ ] Navigation clavier : Tab atteint les boutons, Entrée les active, le focus est visible.

## Thèmes

- [ ] Thème sombre : contrastes lisibles sur toutes les pages.
- [ ] Thème clair : contrastes lisibles sur toutes les pages.
- [ ] Thème système : reprend le réglage Windows au démarrage.
- [ ] Changement d'accentuation : appliqué immédiatement partout, sans redémarrage.

## Roblox

- [ ] Roblox installé : état « Roblox est prêt », version et dossier affichés.
- [ ] Roblox supprimé ou profil vierge : état « Roblox n'est pas installé », sans erreur.
- [ ] Dossier `Versions` présent mais sans `RobloxPlayerBeta.exe` : état « Installation incomplète ».
- [ ] « Ouvrir le dossier Roblox » ouvre bien le dossier détecté.
- [ ] « Jouer » est désactivé et l'infobulle explique la phase 2.

## Robustesse

- [ ] `settings.json` corrompu à la main : l'application démarre, un fichier
  `settings.invalide-<horodatage>.json` est conservé, les valeurs par défaut s'appliquent.
- [ ] Dossier `%APPDATA%\LinkLauncher` en lecture seule : message d'erreur compréhensible,
  l'application reste utilisable.
- [ ] Fermeture pendant une vérification : aucune exception non gérée.
