# Master 128 — SAVE/CHAIN tape, 7 novembre 2026

Deux cartes Master 128 successives, MOS 3.50/BASIC IV externes, WD1770 et interface Winchester présents mais sans disque monté ; aucun coprocesseur. Le transport réel est utilisé, pas le chargement direct. Aucun changement de code de production, de fréquence, de phase, de mode Debug ou de débit tape.

La première carte exécute `*TAPE`, puis les lignes BASIC `10 PRINT"MASTER TAPE PASS"` et `20 PRINT 12+34`. Une tape en mémoire est insérée avec écriture autorisée, REC activé ; SAVE"RML" et Return sont saisis au clavier invité. Le MOS affiche `RML 00 0026` et rend la main. Le signal enregistré contient **527 308 échantillons à 48 000 Hz**. Après Stop et capture, les entrées/callbacks de la première carte sont détachés et la tape éjectée.

Une nouvelle carte démarre ses ROM, sélectionne `*TAPE`, monte uniquement ce signal et exécute CHAIN"RML". Elle affiche Searching, Loading, le bloc RML, puis **MASTER TAPE PASS** et **46**. Aucun octet du programme BASIC n'est injecté dans la RAM du récepteur. Le calcul est vérifié après les six images de stabilisation hors mesure.

| Fenêtre Debug | Images | Cycles | ms/image | Images/s | MHz | % de 2 MHz |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Après validation Return du SAVE | 600 | 23 961 599 | 19,703009 | 50,7537 | 2,026898 | 101,34 % |
| CHAIN jusqu'au texte PASS | 283 | 11 301 888 | 20,836396 | 47,9929 | 1,916646 | 95,83 % |

Rendu et drainage PCM inclus, sans limite hôte, sans RML/audio Windows, compilation ou autre test concurrent. Pour CHAIN, la vérification de texte entre images est exclue du chronométrage ; la saisie précède la fenêtre. La fenêtre SAVE comprend également la fin de bande et l'attente au prompt ; ce ne sont pas les durées complètes des commandes. Cadence vidéo matérielle conservée à environ 50,08 Hz. La lecture reste légèrement au-delà du budget 19,97 ms/image ; aucune optimisation nouvelle ni gain avant/après revendiqué.

Captures réelles inspectées sous `artifacts/TestsMachines/Acorn/BBC Master 128/` et `artifacts/TestsChips/BbcSerialUla/BBC Master 128/` : `20261107-103140-master128-tape-save.bmp`, `20261107-103150-master128-tape-chain-pass.bmp`. Le second montre le résultat 46 ; il ne s'agit pas d'une fenêtre RML ni d'une validation acoustique. Code de sortie 0. Chaque carte est détachée dans finally ; aucun fichier média/configuration utilisateur créé ou modifié. Source temporaire supprimée puis banc reconstruit avec sa source antérieure.
