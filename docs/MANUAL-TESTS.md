# Tests manuels — Phases 1 à 6

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

## Bibliothèque et profils

- [ ] Navigation : l'entrée « Bibliothèque » ouvre la page, la barre latérale la marque active.
- [ ] Premier passage : un profil « Par défaut » existe et est actif ; la liste d'expériences est vide
      et l'état vide explique qu'aucun téléchargement n'est effectué.
- [ ] Ajout d'une expérience par lien officiel (`https://www.roblox.com/games/920587237/Adopt-Me`) :
      elle apparaît avec son lieu et l'adresse canonique `https://www.roblox.com/games/920587237`.
- [ ] Ajout par identifiant de lieu seul (`920587237`) : même résultat.
- [ ] Ajout d'un lien étranger (`https://example.com/games/1`) ou d'un texte libre : refusé avec un message clair.
- [ ] Ajout deux fois de la même expérience : refusé avec « Cette expérience est déjà dans la bibliothèque. ».
- [ ] « Ouvrir sur Roblox » ouvre la page officielle dans le navigateur par défaut, puis la ligne
      indique la date de dernière ouverture.
- [ ] « Retirer » supprime l'expérience, et `%APPDATA%\LinkLauncher\library.json` ne la contient plus.
- [ ] Ajout d'un profil (`Anglais`, `en-US`) : il apparaît dans la liste.
- [ ] Sélection d'un profil : il devient actif, le résumé « Profil actif : … » et la ligne du
      Dashboard « Profil de lancement : … » concordent.
- [ ] Lancement après activation du profil « Anglais » : le journal contient
      `robloxLocale:en_US+gameLocale:en_US` dans l'URI transmise.
- [ ] Nom de profil vide ou déjà utilisé : refusé, aucun profil ajouté.
- [ ] Langue invalide (`fr_FR+launchmode:play`) : refusée avec « La langue doit ressembler à fr_FR ou en-US. ».
- [ ] Suppression d'un profil non actif : il disparaît, le profil actif reste inchangé.
- [ ] Suppression du profil actif : le premier profil restant devient actif.
- [ ] Tentative de suppression du dernier profil : refusée.
- [ ] Redémarrage de LinkLauncher : profils, profil actif et expériences sont restaurés depuis `library.json`.

## Notifications (phase 4)

- [ ] Paramètres : la carte « Notifications » est cochée par défaut, la décocher est enregistrée
      dans `settings.json` (`enableNotifications: false`).
- [ ] Notifications activées, client arrêté : « Jouer » affiche une notification « Client Roblox
      lancé » avec le PID, dans le coin de la zone de travail.
- [ ] Fermeture du client : notification « Client Roblox arrêté » avec le code de sortie.
- [ ] Lancement du client depuis le site Roblox (navigateur) : la notification apparaît aussi.
- [ ] Notifications désactivées : aucun affichage, aucune erreur, le journal reste silencieux.
- [ ] Plusieurs démarrages/arrêts rapides : les fenêtres ne s'empilent pas indéfiniment et se ferment seules.

## Présence Discord (phase 4)

- [ ] Sans identifiant d'application : la présence est désactivée, cocher la case l'active sans
      publier d'activité, et le texte d'état indique qu'un identifiant est requis.
- [ ] Identifiant invalide (`abc`, 5 chiffres) : « Enregistrer » affiche « L'identifiant
      d'application Discord doit contenir 17 à 20 chiffres. », rien n'est enregistré.
- [ ] Identifiant valide enregistré : la présence est enregistrée dans `settings.json`.
- [ ] Discord fermé : état « Discord n'est pas joignable… », une seule ligne d'avertissement dans le journal.
- [ ] Discord ouvert : état « Activité publiée sur Discord. », l'activité « Dans LinkLauncher »
      apparaît sur le profil, puis « Client Roblox lancé » quand le client tourne.
- [ ] Décocher la présence : l'activité disparaît du profil Discord.
- [ ] Aucun jeton ni identifiant Roblox n'apparaît dans `settings.json` ni dans le journal.

## Diagnostic (phase 4)

- [ ] « Collecter » remplit toutes les lignes : LinkLauncher, Windows, moteur .NET, architecture,
      configuration, bibliothèque, journaux, dernier journal, version et dossier Roblox, client, intégrité.
- [ ] La date de génération correspond à l'heure réelle et le résumé l'indique.
- [ ] « Copier le rapport » place un texte lisible dans le presse-papiers.
- [ ] « Ouvrir les journaux » ouvre `%LOCALAPPDATA%\LinkLauncher\logs`.
- [ ] Roblox non installé : les lignes Roblox affichent « — » et l'intégrité « Non vérifiée ».
- [ ] Le rapport ne contient aucun cookie, jeton ou identifiant de compte.

## Mises à jour (phase 5)

- [ ] « Rechercher » sans accès réseau : message « Impossible de joindre le dépôt des publications… »,
      aucune exception, bouton réactivé.
- [ ] Dépôt sans publication plus récente : « LinkLauncher 0.1.0 est à jour. ».
- [ ] Publication plus récente avec installeur et empreinte dans les notes : version affichée,
      « Télécharger » devient actif.
- [ ] Publication sans empreinte SHA-256 : le message le dit et « Télécharger » reste inactif.
- [ ] Téléchargement réussi : le fichier apparaît dans
      `%LOCALAPPDATA%\LinkLauncher\updates\<version>\` et le statut annonce l'empreinte vérifiée.
- [ ] Empreinte volontairement fausse dans les notes : le téléchargement est refusé et le fichier
      est supprimé du disque.
- [ ] « Installer » lance l'installeur vérifié ; aucun fichier de LinkLauncher n'est modifié par l'application.

## Installeur (phase 6)

- [ ] `installer\build.ps1` produit `artifacts\publish\LinkLauncher.exe` et l'application se lance depuis ce dossier.
- [ ] `installer\build.ps1 -Installer` produit `artifacts\installer\LinkLauncher-0.1.0-setup.exe`
      et affiche son empreinte SHA-256.
- [ ] Installation : l'icône, le raccourci du menu Démarrer et la désinstallation sont présents.
- [ ] L'application installée démarre, détecte Roblox et conserve les préférences existantes.
- [ ] Réinstallation par-dessus une version antérieure : les données utilisateur restent intactes.
- [ ] Désinstallation : l'application et ses raccourcis disparaissent, `%APPDATA%\LinkLauncher` est conservé.

## Activité (reprise de Bloxstrap)

- [ ] Navigation : l'entrée « Activité » (Ctrl+3) ouvre la page et la marque active ; Paramètres
      passe à Ctrl+4.
- [ ] Premier passage : les trois totaux affichent « 0 min » et l'état vide explique le
      fonctionnement.
- [ ] Lancement du client Roblox : une session apparaît, sans durée tant que le client tourne.
- [ ] Fermeture du client : la session est close, sa durée s'affiche et le code de sortie est
      mentionné.
- [ ] Les totaux « Aujourd'hui » et « 7 derniers jours » correspondent aux sessions observées.
- [ ] Fermeture de LinkLauncher pendant que le client tourne, puis redémarrage : la session
      apparaît comme « interrompue » et sa durée n'est pas comptée.
- [ ] « Effacer » demande une seconde confirmation, puis vide la page et `activity.json`.
- [ ] `activity.json` supprimé ou corrompu à la main : la page reste vide, sans erreur, et un
      fichier `activity.invalide-<horodatage>.json` est conservé.
- [ ] Aucun contenu de jeu, aucun nom de serveur et aucune donnée de compte dans `activity.json`.

## Robustesse

- [ ] `settings.json` corrompu à la main : l'application démarre, un fichier
      `settings.invalide-<horodatage>.json` est conservé, les valeurs par défaut s'appliquent.
- [ ] `library.json` corrompu à la main : la page Bibliothèque s'ouvre avec un profil par défaut,
      un fichier `library.invalide-<horodatage>.json` est conservé et l'incident est journalisé.
- [ ] `library.json` contenant un lien étranger ou un doublon : l'entrée est écartée au chargement,
      le reste de la bibliothèque est conservé.
- [ ] Dossier `%APPDATA%\LinkLauncher` en lecture seule : message d'erreur compréhensible,
      l'application reste utilisable.
- [ ] Fermeture pendant une vérification ou pendant un lancement : aucune exception non gérée.
- [ ] Double-clic rapide sur « Jouer » : une seule demande transmise (bouton neutralisé pendant
      l'attente).
