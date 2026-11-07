# Master 128 — Boulder Dash, 7 novembre 2026

Carte Master 128 MOS 3.50/BASIC IV/DFS intégré, sans coprocesseur, WD1770 et interface Winchester activés mais sans HDD ni tape montés. SSD source ouverte en lecture seule via le SDK : `F:\Retro\ef_tosec_backup_2024\Acorn BBC - Games - [SSD] (TOSEC-v2024-01-15)\Boulder Dash (1988)(Tynesoft)\Boulder Dash (1988)(Tynesoft).ssd`.

Le vrai CPU exécute `*DISC` puis `*EXEC !BOOT`. Après 800 images, le compteur indique 28 672 octets physiques lus. Deux appuis Space de 30 images, séparés de 180 images relâchées chacun, font passer au niveau du jeu. Captures réelles conservées et inspectées sous `artifacts/TestsMachines/Acorn/BBC Master 128/` et `artifacts/TestsChips/Mos65C12/BBC Master 128/` :

- `20261107-102824-master128-boulderdash-title.bmp` : étape après chargement.
- `20261107-102833-master128-boulderdash-start.bmp` : niveau commencé, compteur 149.
- `20261107-102838-master128-boulderdash-after.bmp` : niveau en cours, compteur 146.

Mesure Debug sans limite hôte, sans RML, sortie audio Windows, compilation ou autre test concurrent : 240 images, rendu et drainage audio inclus, **20,484538 ms/image**, **48,8173 images/s**, **1,949569 MHz**, **97,48 %** de 2 MHz ; **9 584 643 cycles**. Cadence matérielle conservée à environ 50,08 Hz. Aucune lecture disque pendant cette fenêtre, zéro échantillon PCM non nul : phase silencieuse, donc aucune preuve de fidélité sonore tirée de cette fenêtre. Aucun avant/après ni gain de code revendiqué.

Le SHA256 de la SSD est identique avant et après : `C62420CA3D101854685658AD72EF0E84D98B88FE5F728AFC5F5CB553547BF7C5`. Entrées et callbacks détachés, média retiré avant Dispose dans finally ; code de sortie 0. Source de banc temporaire supprimée puis assembly reconstruit avec sa source antérieure. Aucune modification de production ni nouveau build All nécessaire pour ce seul essai. Le budget Debug n'est pas entièrement tenu ; ce jeu ne clôture pas à lui seul la machine.
