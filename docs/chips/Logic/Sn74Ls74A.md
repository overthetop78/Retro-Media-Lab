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

### Représentation compacte des sorties — 9 novembre 2026

Profil natif actuel System 5 : CaptureClock 2,81 %, StoreOutputs 1,61 %, SetClock 1,41 %, Get 1,26 % du temps attribué ; attribution propre, pas une somme de temps inclusifs. Les deux nullable conservés dans chaque canal sont remplacés par un mot de niveaux et connaissance, sans copie supplémentaire de l'état. Il représente exactement les quatre états accessibles : inconnus tous deux, Q bas et /Q haut, Q haut et /Q bas, PRE=CLR actif avec Q et /Q hauts. La libération simultanée interdite restaure l'inconnue. La capture compare ce mot directement ; les lectures et notifications seules construisent les valeurs du contrat public. Clock réutilise aussi le canal déjà validé pour ses deux fronts. Capture partagée, PRE/CLR asynchrones, compteurs et fréquences distincts, feedback et paramètres invalides restent vérifiés.

Les 11 contrôles de la bascule et les 9 du contrôleur WAIT/READY réussissent (20 au total, 70 ms). Aucun nouveau test miroir ajouté ; les contrôles fiables couvrent déjà ces états et modes.

Banc autonome Debug : 24 000 périodes de chauffe, trois fenêtres de 2,4 millions, deux canaux avec SetClock haut/bas comme le raccordement WAIT/READY. Les callbacks comptent et hachent les changements, avec leur compteur de clock. Aucun autre test ou compilation concurrente.

| Scénario | Avant, ms | Mot compact, ms | Résultat |
| --- | --- | --- | --- |
| D établis, sorties inchangées | 224,1390 / 230,7249 / 228,1847 | 129,6916 / 132,2843 / 132,5889 | durée moyenne réduite de 42,2 % |
| D alternés, sorties notifiées | 432,9096 / 403,0312 / 404,5279 | 411,7918 / 413,3656 / 430,9163 | pas de gain établi, environ 1,3 % de durée en plus |

Comptes identiques par canal : 2 424 000 / 4 824 000 / 7 224 000. Scénario établi : zéro notification par fenêtre, empreinte CBF29CE484222325. Scénario alterné : 4 800 000 notifications par fenêtre, empreintes identiques avant/après 0FE5C7A5C0B0E325 / BFC32926807C0325 / 4D3B67E9C22B6325. Le gain isolé n'est pas une mesure de machine ; la rétention finale attend la mesure System 5 complète. Le compromis lorsque toutes les sorties changent est conservé explicitement, sans annoncer de gain universel.

Rétention après comparaison native : le premier passage compact (61,183502 ms/champ) n'améliorait pas le précédent (60,067840). Référence rétablie dans les conditions actuelles : 61,101054 ; compact rétabli : 59,137233, soit un gain rapproché modeste de 3,2 %. Cette variation est explicitement conservée ; ne pas annoncer un gain natif de 42,2 %. Dernière vitesse 33,62–33,85 % Debug, images et comptes identiques. Tableau complet, scénarios et limites dans la [validation System 5](../../tasks/machines/AcornSystem/system5-validation.md), passage « Double bascule WAIT/READY compacte ».

Après nettoyage et régénération : 118 contrôles de composants réussis (458 ms), dont les 20 contrôles fiables bascule/WAIT/READY, et 135 contrôles System/audio (3 s). Aucun test permanent supplémentaire ajouté pour refléter le nouveau stockage privé. Capture réelle du moniteur inspectée et copiée sous TestsChips/Sn74Ls74A/Acorn System 5/20261109-wait-clock-monitor-debug.bmp ; SHA256 162FD4E97CB8FF4D3D07BE20A79393C087D912C1559AEFE60DB9F951DFB9DA12, même fichier pour la référence rétablie conservée séparément. Preuves de transitions/comptes du banc dans 20261109-output-representation-results.md ; native dans 20261109-wait-clock-native-results.md. Bancs et traces supprimés, aucun build de livraison ni clôture machine.

Validation courante avec le stockage compact conservé (29dedacdd) : sur System 5, CAT, chargement exact des 4096 octets BASIC et calcul 46, puis SAVE/LOAD d'une copie avec réouverture dans une machine neuve ; 768 octets écrits et 768 relus physiquement, motif exact, source intacte et copie supprimée. Aucune modification supplémentaire de la bascule requise par ce contrôle. Détails et limites dans la [validation System 5](../../tasks/machines/AcornSystem/system5-validation.md), passage « Validation logicielle et persistance sur le montage optimisé ». Preuves de cette exécution copiées sous TestsChips/Sn74Ls74A/Acorn System 5/20261109-current-software-results.md ; captures réelles du calcul et de la nouvelle lecture inspectées. Banc temporaire supprimé, assembly régénérée.
