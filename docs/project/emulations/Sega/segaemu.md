# SegaEmu — décisions et périmètre

> **Arrêt demandé le 13 novembre 2026.** Les composants ajoutés sous
> `src/RetroMediaLab.Chips/EmuChip/` et les fichiers de l’implémentation SG-1000 sous
> `src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/` ont été supprimés à la demande de
> l’utilisateur. Aucun retour aux puces originales ni nouveau câblage n’a été effectué.
> Les décisions ci-dessous sont conservées pour référence ; elles ne constituent pas un travail
> en cours ni une autorisation de recréer l’émulateur. Acorn reste suspendu.

Décisions de l’utilisateur du 11 novembre 2026. Le développement Acorn reste suspendu. SegaEmu est le nom retenu pour notre cœur natif SEGA ; les adaptateurs existants à d’autres émulateurs ne constituent pas son implémentation.

## Organisation du travail

Les tâches décrivent uniquement le code à réaliser. La tenue des documents est obligatoire et intégrée au travail, sans tâche ni case dédiée.

Les anciens suivis SG-1000 et d’optimisation EmuChip sont supprimés avec ce nettoyage.
Les résultats durables restent dans `docs/chips/` et `docs/project/emulations/Sega/`.
Le dossier `docs/tasks/chips` est conservé pour les prochains composants. Une éventuelle reprise
demandera de nouvelles actions correspondant au code alors présent, sans réactiver ces anciens plans.

## Directives et vitesse de travail

Relire et suivre [la procédure puces/machines](../../../directives/Emulations/procedure-puces-machines.md) à chaque relance. Les décisions explicites consignées ici s’appliquent au chantier SEGA, notamment les états et l’attente des réponses utilisateur.

Regrouper les modifications cohérentes et leurs contrôles utiles ; éviter les micro-modifications, les compilations répétées, les recherches déjà consignées et les tests redondants. Préserver la fidélité matérielle et ne déclarer réalisé que ce qui est effectivement réalisé et vérifié. Un lot doit avoir un résultat concret ; la rédaction de suivis ne remplace pas l’implémentation.

Avant la finalisation d’une puce, réaliser toutes ses fonctions et appliquer les optimisations pertinentes du tableau des directives, autant que possible sans la machine fonctionnelle. Établir les conditions d’application et conserver tous les effets matériels observables. Rechercher des gains significatifs, mesurer les parcours concernés et ne pas multiplier les petites tentatives. Une fois la machine fonctionnelle, reprendre les optimisations à partir de ses mesures complètes ; un gain isolé n’est pas un gain de la machine.

Rechercher les réponses techniques sur plusieurs pistes documentaires, en privilégiant les sources matérielles et constructeur. Les projets d’autres émulateurs ne prouvent pas le comportement matériel. Demander uniquement les décisions ou informations nécessitant réellement la réponse de l’utilisateur. Dans le fichier thématique concerné, consigner la question précise, les éléments établis, ce qui dépend de la réponse et la réponse reçue. En attente, laisser le point et la puce inachevés et continuer les travaux indépendants sur cette puce ou une autre ; ne pas inventer de réponse ni clôturer implicitement le point.

## Modèles et extensions

Une même machine peut proposer plusieurs modèles si leurs différences matérielles et leur configuration le permettent. Le regroupement sera établi depuis les matériels documentés ; cette liste n’impose donc pas une machine logicielle distincte par entrée. Chaque modèle reste identifiable et doit être vérifié.

Les extensions se choisissent depuis la console associée et se réalisent avec elle avant de passer à la suivante. Si Mega Drive 1 et Mega Drive 2 deviennent deux machines distinctes, toutes deux doivent proposer Mega-CD 1, Mega-CD 2 et 32X, avec les combinaisons matériellement compatibles. Sega CDX / Multi-Mega et Wondermega sont des consoles à part entière. Ne pas confondre Mark III et Master System.

Ordre de référence, sans omission, issu du [document SEGA de l’utilisateur](https://docs.google.com/document/d/1orH8_BOIe6IZ6cRaJyMIuWQ_Gce7-ND9ddO30AaX4gA/edit?tab=t.0#heading=h.1fzuuwkfmpyx) :

1. SG-1000
2. SG-1000 II
3. SC-3000
4. SC-3000H
5. SF-7000
6. Mark III
7. Master System
8. Master System II
9. Master System III
10. Master System Compact
11. Game Gear
12. Game Gear Micro
13. Mega Drive
14. Mega Drive II
15. Genesis Model 1
16. Genesis Model 2
17. Genesis Model 3
18. Nomad
19. Mega Jet
20. Mega-CD
21. Sega CD Model 1
22. Sega CD Model 2
23. 32X
24. Sega CDX / Multi-Mega
25. Wondermega
26. Sega Saturn
27. Sega Saturn Model 2
28. Hi-Saturn
29. V-Saturn
30. Dreamcast
31. Dreamcast R7
32. Sega Pico
33. Advanced Pico Beena

L’ordre des consoles est conservé ; les extensions sont traitées avec leur console et ne sont pas reportées derrière toutes ses variantes.

## Première machine : SG-1000

Commencer par établir la carte, ses variantes et ses composants depuis les sources matérielles. Identifier les références exactes avant de choisir les profils Z80, VDP et générateur sonore ; vérifier l’adéquation du SN76489 existant, sans présumer qu’il couvre la variante montée. Recenser aussi décodage, mémoire, buffers, contrôleurs, cartouches et périphériques compatibles ; ne pas limiter silencieusement la console à une manette standard.

Terminer les composants, puis le câblage et la validation de la machine. Les sauvegardes et chargements d’état sont requis pour SEGA avant de passer à la machine suivante : couvrir les états internes nécessaires à la reprise, y compris bus, interruptions, compteurs, vidéo en cours et continuité audio. L’ancien report des états pour Acorn ne s’applique pas à SegaEmu.

Les tests de puces vont dans `tests/RetroMediaLab.Chips.Tests`, les tests d’émulation dans `tests/RetroMediaLab.LocalEmulationTests`, les tests de médias locaux dans `tests/RetroMediaLab.LocalDiskImageTests`.

État actuel : décisions consignées ; aucune puce ni machine SegaEmu n’est déclarée implémentée ou validée par ce document.
