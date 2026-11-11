# Parité fonctionnelle avec Bloxstrap — ce que LinkLauncher reprend, et ce qu'il refuse

[Bloxstrap](https://github.com/bloxstraplabs/bloxstrap) est un bootstrapper Roblox qui remplace le
programme de lancement officiel. LinkLauncher suit un autre parti pris : il ne remplace **rien**
du client Roblox. Ce document recense les fonctionnalités de Bloxstrap, dit lesquelles sont
reprises ici, et pourquoi les autres sont écartées.

Aucun code de Bloxstrap n'est copié : les fonctionnalités reprises sont réécrites à partir de
leur description publique et des interfaces officielles de Windows, Discord et Roblox.

## Pourquoi la copie directe est exclue

La majorité des fonctions propres à Bloxstrap reposent sur des mécanismes que le projet
LinkLauncher s'interdit formellement (`AGENTS.md`) :

- remplacer ou modifier `RobloxPlayerLauncher.exe` / `RobloxPlayerBeta.exe` ;
- injecter une DLL dans le processus Roblox (`BloxstrapRPC`) ;
- lire ou modifier la mémoire du client ;
- écrire des FastFlags, c'est-à-dire des indicateurs internes du moteur ;
- contourner le verrou d'instance unique du client.

Ces mécanismes sont aussi ceux que Roblox restreint de son côté : Bloxstrap a retiré son éditeur
de FastFlags pour les joueurs (les indicateurs sont désormais sur liste blanche) et a
successivement retiré puis remis le multi-instance en le marquant « à vos risques ».

## Matrice de parité

| Fonctionnalité Bloxstrap | Décision LinkLauncher | Raison |
| --- | --- | --- |
| Lancement du client officiel | **Déjà présent** | URI officielle `roblox-player:1+launchmode:app`, sans aucun identifiant |
| Suivi de l'activité, temps de jeu, sessions | **Repris** (page Activité) | Observation seule du processus : PID, démarrage, code de sortie |
| Notifications de démarrage / d'arrêt | **Déjà présent** | Notifications locales maison, désactivables |
| Intégration Discord (présence) | **Déjà présent, autrement** | IPC local officiel de Discord, sans injection dans Roblox |
| Thèmes et apparence | **Déjà présent** | Design system WPF : thème clair/sombre/système, 6 accentuations |
| Raccourcis Bureau et menu Démarrer | **Déjà présent** | Installeur Inno Setup |
| Vérification de mise à jour de LinkLauncher | **Déjà présent** | Publications GitHub + empreinte SHA-256 vérifiée |
| Désinstallation propre | **Déjà présent** | Désinstalleur qui conserve `%APPDATA%\LinkLauncher` |
| Remplacement du bootstrapper Roblox | **Refusé** | Modifie le mécanisme de lancement officiel |
| Éditeur de FastFlags et presets graphiques | **Refusé** | Configuration non officielle du moteur, sur liste blanche côté Roblox |
| Multi-instance | **Refusé** | Contourne le verrou d'instance unique du client |
| Mods, polices, sons, curseurs | **Refusé** | Modifie les fichiers du client |
| BloxstrapRPC (présence enrichie) | **Refusé** | Injection d'une DLL dans le processus Roblox |
| Changement de canal de déploiement | **Refusé** | Passe par un lanceur modifié |
| Navigateur de serveurs, localisation des serveurs | **Refusé** | Requêtes non officielles et jointure exigeant un ticket de session |
| Rejoindre un serveur précis (identifiant de tâche) | **Refusé** | Ticket délivré uniquement par le site Roblox dans la session du navigateur |
| Fermer Roblox avec le launcher | **Non retenu** | LinkLauncher ne ferme jamais le client de l'utilisateur |
| Interface traduite | **Non retenu pour l'instant** | Interface en français uniquement |

## Ce que la page Activité apporte

- Chaque démarrage du client ouvre une session horodatée (PID, heure de démarrage, profil de
  lancement actif).
- Chaque arrêt clôt la session et enregistre le code de sortie rapporté par Windows.
- Les totaux **aujourd'hui**, **7 derniers jours** et **depuis le début** sont calculés à partir
  des sessions réellement observées.
- Une session laissée ouverte par un arrêt brutal de LinkLauncher est marquée **interrompue** :
  elle reste visible, mais sa durée n'est jamais inventée ni comptée.

Données conservées dans `%APPDATA%\LinkLauncher\activity.json`, limitées aux 200 dernières
sessions. Aucun contenu de jeu, aucune conversation et aucune donnée de compte n'y figure.

## Pistes autorisées, non implémentées

- Démarrage de LinkLauncher avec Windows (clé `HKCU\…\Run`).
- Export et import de la configuration et de la bibliothèque au format JSON.
- Détection du changement de version du client Roblox entre deux relevés.
