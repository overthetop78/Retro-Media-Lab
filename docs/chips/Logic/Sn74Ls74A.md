# SN74LS74A — double bascule D

## Référence physique

[Texas Instruments SDLS119](https://www.ti.com/lit/ds/symlink/sn74ls74a.pdf), décembre 1983, révision mars 1988, page 1. PDF, texte et rendu inspecté sous `artifacts/.tmp-archives/chips/Logic/TexasInstruments/SN74xx/SN74LS74A/`.

| PRE | CLR | CLK | Q | /Q |
| --- | --- | --- | --- | --- |
| bas | haut | indifférent | haut | bas |
| haut | bas | indifférent | bas | haut |
| bas | bas | indifférent | haut | haut |
| haut | haut | front montant | D | non D |
| haut | haut | aucun front montant | maintien | maintien |

Les contrôles asynchrones dominent D. Leur libération successive conserve l'état défini par le dernier contrôle actif. Leur libération simultanée depuis PRE=CLR=bas ne garantit pas une résolution complémentaire déterminée : les deux sorties deviennent `null` jusqu'à une capture ou un contrôle valide. Le modèle numérique ne prétend pas caractériser les tensions, la métastabilité ou les violations de setup/hold. Les graines optionnelles du constructeur sont des phases reproductibles, pas des niveaux de power-on garantis.

## Réalisation

`src/RetroMediaLab.Chips/Logic/TexasInstruments/SN74xx/SN74LS74A/` contient constantes, contrat `ISn74Ls74` et service `Sn74Ls74`. Chaque canal (0 ou 1) possède D/CLK/PRE/CLR mémorisés, Q et /Q, fréquence nominale reçue et compteur de fronts montants. La source externe fournit ses fronts ; aucun oscillateur indépendant, registre, bus, IRQ ou reset fictif n'est ajouté.

`SetClocks` capture les deux entrées avant publication des sorties d'un front partagé. Un retour vers D ne change pas la capture simultanée de l'autre canal. Notifications seulement aux changements ; contrôles asynchrones immédiats ; maintien de CLK sans front supplémentaire. `Clock` réalise une période depuis CLK bas. `AdvanceClocks` regroupe seulement les périodes à sorties stables ou sans observateur, sinon conserve les transitions unitaires et réévalue les entrées après notification. Compteurs et horodatages restent locaux ; aucun sondage d'une autre puce, ordonnanceur ou thread.

## Résultats actuels — 9 novembre 2026

**11 contrôles Debug réussis, zéro échec/ignoré, 59 ms.** `tests/RetroMediaLab.Chips.Tests/Logic/TexasInstruments/SN74xx/SN74LS74A/Sn74Ls74Tests.cs` couvre la table sur les deux canaux, contrôles asynchrones sans clock, maintien, état interdit/libération, indépendance, capture partagée avec retour vers D, regroupement comparé aux périodes unitaires avec notifications horodatées, fréquences distinctes et paramètres invalides.

Débit actuel hors RML en Debug net10.0 : 20 000 périodes de chauffe, trois passages de 2 400 000 périodes. Les deux D sont inversés à chaque période, puis `SetClocks(true,true)` et `SetClocks(false,false)` appliqués. Les deux callbacks comptent leurs changements : 4 800 000 par passage, contrôlés ainsi que les compteurs locaux.

| Passage | Durée hôte | Millions de fronts partagés par seconde |
| --- | --- | --- |
| 1 | 204,057800 ms | 11,761373 |
| 2 | 199,110300 ms | 12,053621 |
| 3 | 199,990400 ms | 12,000576 |

Chaque canal reçoit ce nombre de fronts. Ce débit n'est pas une mesure de machine assemblée. Le banc sans ROM ni ressource native est supprimé après consignation et l'assembly régénérée. Le circuit WAIT/READY et son raccordement System 5 restent distincts de ces résultats de puce.
