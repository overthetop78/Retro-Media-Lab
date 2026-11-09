# Générateur partagé Acorn 400,005

## Référence physique

Acorn System 5 Handbook, carte CPU 6502A 400,005, §2.3, figure 9, pages imprimées 7–8 (PDF 48–49). [Manuel constructeur archivé](https://chrisacorns.computinghistory.org.uk/docs/Acorn/Manuals/Acorn_System5Handbook.pdf) sous `artifacts/.tmp-archives/machines/Acorn/Acorn System 5/`. Chronogramme inspecté : `System5-clock-timing-49.png`. Nomenclature : IC2, IC3 et IC4 sont trois 74S163 ; l'oscillateur X1/IC1 fournit 24 MHz. La fiche TI et les règles du compteur sont dans [SN74S163](../Logic/Sn74S163.md).

| Liaison réelle | Effet |
| --- | --- |
| CLK 24 MHz commun aux trois compteurs | Capture des trois ensembles d'entrées avant résolution des sorties du même front. |
| IC2/QB inversé vers CLR d'IC2 | Division par trois ; QA fournit 8 MHz avec un état haut d'une période source et un état bas de deux périodes, pas une onde artificielle à 50 %. |
| IC2/QA vers ENP et ENT d'IC3 | Un comptage sur trois clocks ; QA/QB/QC d'IC3 fournissent 4/2/1 MHz. |
| IC3/RCO inversé vers CLR d'IC4 | Synchronisation toutes les 2 µs, avec la validation ENT réelle de RCO. |
| QA/QB/QC d'IC4 | Sorties 12/6/3 MHz. |

Les liens LK2/LK3/LK1 sélectionnent ensuite respectivement 1/2/3 MHz pour le CPU. Le circuit WAIT/READY utilise IC5, un 74LS74 distinct ; les signaux décrits ici sont **avant ces circuits**. Le générateur n'est pas un ordonnanceur commun des puces de la machine.

## Réalisation du 9 novembre 2026

`RetroMediaLab.Chips/Controllers/Acorn/SystemClock/Services/SystemClockGenerator.cs` raccorde les trois instances de SN74S163. Chaque instance conserve son CLK, son compte, ses entrées et sa capture interne. Le générateur conserve le niveau de la source 24 MHz, son nombre de fronts et les niveaux du bus d'horloges. Les sorties des compteurs modifient leurs destinataires par notifications ; aucune lecture répétée d'une autre puce n'est utilisée pour rechercher des changements.

À chaque front source, toutes les captures sont réalisées avant résolution d'IC2, IC3 et IC4. Les sorties externes sont publiées lorsque les niveaux raccordés de ce front sont résolus ; les mutations de calcul ne deviennent pas des impulsions RCO fictives. SetOscillator reçoit le niveau physique de CLK ; Clock avance une période complète ; AdvanceClocks conserve actuellement chaque front unitaire. Aucune optimisation arithmétique nouvelle, aucun thread ni délai systématique d'un tick ajouté. Les sept sorties ont des notifications distinctes, sans destinataire imposé pour une sortie non raccordée.

La graine déterministe IC2=2, IC3=0, IC4=0 définit l'origine de phase du modèle ; TI ne garantit pas ces valeurs au power-on. Le constructeur interne utilisé par les contrôles permet d'exercer une phase IC4 initialement différente, puis sa remise en phase par la vraie liaison CLR. Aucun reset CPU n'est raccordé aux compteurs libres. Le modèle numérique ne prétend pas caractériser les délais analogiques de l'oscillateur ou les violations de setup/hold.

## Résultats actuels

**21 contrôles Debug réussis, zéro échec/ignoré, 58 ms** : 12 cas propres au SN74S163 et neuf cas du générateur. Les contrôles parcourent 96 fronts contre les équations du chronogramme, comparent les trois comptes et leurs clocks reçus, vérifient les sept sorties, leurs nombres de fronts et leurs phases, le rapport cyclique 8 MHz, la resynchronisation d'IC4 initialement décalé et la visibilité simultanée des niveaux finaux chez les récepteurs. Le maintien d'un niveau CLK n'ajoute pas de front. Les avances groupées sont comparées aux unitaires, avec toutes les notifications et leurs horodatages.

Ces durées sont celles des tests fonctionnels, pas une mesure du débit de la carte ou du générateur. Le composant n'est pas encore raccordé dans AcornSystemMachine : la suppression du calcul de phase VIA dans la carte et les essais actuels System 5 sont à réaliser après les circuits WAIT/READY. Ne pas présenter les anciennes mesures System 5 comme issues de ce générateur.

### Débit actuel après réalisation — 9 novembre 2026

Mesure isolée Debug net10.0, hors RML, sans compilation/test concurrent : 20 000 périodes de chauffe puis trois avances de 2 400 000 périodes source par `AdvanceClocks`. Les trois compteurs et leurs liaisons internes fonctionnent ; aucune sortie externe n'est raccordée à la machine dans ce scénario.

| Passage | Durée hôte | Millions de périodes source par seconde |
| --- | --- | --- |
| 1 | 503,583900 ms | 4,765839 |
| 2 | 507,499500 ms | 4,729069 |
| 3 | 538,184900 ms | 4,459434 |

Le chemin actuel traite chaque période source : il atteint seulement 18,58–19,86 % des 24 MHz requis, avant les récepteurs. Ce n'est pas le pourcentage de la machine entière. Le banc exerce les captures/résolutions unitaires des trois compteurs à chaque front ; le coût est établi pour ce scénario sans le décomposer davantage. Étudier une avance équivalente avant validation de la cadence System 5, en conservant phases, fronts observables et retours synchrones. Aucun gain annoncé. Le banc temporaire commun avec SN74LS74A est supprimé après consignation.

### Suppression des résolutions sans effet au front descendant

Le front descendant est toujours transmis aux trois entrées CLK. Comme il ne déclenche aucune capture de ce réseau à fronts montants, les trois appels de résolution sans capture et la publication sans mutation sont retirés de ce seul front. Le front montant conserve l'ordre de capture/résolution partagé. Un contrôle permanent parcourt 96 périodes et vérifie CLK bas, comptes inchangés et absence de notification supplémentaire au front descendant ; les 12 tests compteur et les dix tests générateur passent (22 cas), plus les deux cas du banc temporaire.

Comparaison sur le même banc Debug, 24 000 périodes de chauffe puis trois passages de 2 400 000 périodes, sans autres tests pendant les mesures finales :

| Récepteurs | Avant, ms par passage | Après, ms par passage | Débit actuel, millions de périodes/s |
| --- | --- | --- | --- |
| Aucun externe | 511,376800 / 502,048400 / 515,595700 | 456,575200 / 446,563300 / 464,932300 | 5,256527 / 5,374378 / 5,162042 |
| Sorties 2 et 1 MHz observées | 519,482600 / 576,829200 / 543,743600 | 456,676300 / 465,708000 / 526,997200 | 5,255364 / 5,153444 / 4,554104 |

Les compteurs des trois puces sont contrôlés à chaque passage ; les deux sorties raccordées produisent exactement 600 000 notifications par passage. Les moyennes hôtes baissent de 10,5 % sans récepteurs et de 11,7 % avec ces récepteurs. La dispersion est conservée ; aucun de ces résultats n'atteint les 24 MHz requis. Cette réduction locale est réalisée, la performance de la machine complète reste à établir après raccordement. Le banc temporaire est supprimé et l'assembly régénérée sans lui.

### Récepteurs System 5 raccordés et première mesure assemblée

La sortie libre 2 MHz rejoint le circuit WAIT/READY et les horloges vidéo/disque ; le VIA 6522A reçoit les phases du CPU 6502A, conformément au fil PH2 de la figure 21, sans diviseur 1 MHz de carte. Le générateur reste libre pendant attente et reset. 32 tests du câblage/RAM/E/S System réussissent. Le parcours natif NOS80 + BASIC chargé et calcul 46 réussit ; trois fenêtres complètes de 40 champs comptent chacune 19 169 280 clocks source et 1 597 440 cycles CPU (ratio 12), à 16,070638–16,430838 % Debug. Valeurs et capture réelles dans la fiche System 5. Ce montage sert de référence aux réductions des accès C# internes ; il ne tient pas encore sa cadence nominale.

### Champs internes et destinataires mémorisés — 9 novembre

CLK source et son total de fronts sont conservés dans des champs canoniques, lus directement par les méthodes internes et par les propriétés publiques, sans copie supplémentaire. Les propriétés de branchement mémorisent le masque des sorties observées. Chaque front et chaque compteur restent traités ; les sept niveaux internes et leur historique sont toujours actualisés, mais le parcours de notification est évité lorsqu'aucune sortie raccordée ne change. Le masque complet des changements est conservé pendant la publication pour qu'un premier callback puisse raccorder un récepteur d'une autre sortie de ce même front, comme avant. Les changements de branchement ne fabriquent aucune notification de niveau.

**23 contrôles compteur/générateur réussis, zéro échec/ignoré, 58 ms**, dont le nouveau cas de branchement/débranchement pendant la publication puis entre fronts. Sur le System 5 complet, après la seule optimisation précédente du compteur (référence 97,755810 / 96,275580 / 99,340815 ms/champ), ce bloc donne 84,661892 / 84,751965 / 86,018600 ms/champ : durée moyenne réduite d'environ 12,9 %. Le débit actuel est 11,811690 / 11,799136 / 11,625393 champs/s libres, 0,471712 / 0,471210 / 0,464272 MHz CPU effectifs, 23,585582 / 23,560516 / 23,213584 % réels. Conditions identiques : Debug, NOS80/VDU80, BASIC exact et PRINT 12+34=46, trois fenêtres de 40 champs avec vidéo/audio.

Chaque fenêtre conserve 19 169 280 clocks source, 1 597 440 cycles CPU et 50,080128 Hz matériels. La capture source-fields est identique aux deux captures précédentes, SHA256 53D97862D9313589B6DDF4D6F149F094038FE64A8FF6C3C8F17F0D5485613A89. Aucune fonctionnalité, phase ou clock matérielle n'est supprimée. Le gain cumulé de ces deux blocs d'accesseurs/parcours est environ 30,6 % de durée hôte depuis la première mesure assemblée ; le montage ne tient toujours pas 100 % en Debug.


### Débit isolé remesuré sur le code actuel — 9 novembre 2026

Code de production 2480c530d, Debug normal, hors RML. 100 000 périodes de chauffe, puis trois fenêtres de 2 000 000 périodes complètes appelées par un délégué Action ; ce coût d'appel est inclus. Entrées conservées pendant la mesure, aucun récepteur externe de notification ; les opérations internes et leurs raccordements restent actifs. Ces mesures ne valent ni pour un composant sans activité interne ni pour tous les récepteurs d'une carte assemblée. Pas de mesure Release dans ce passage.

Générateur avec ses trois compteurs et ses connexions internes, sans destinataire externe. Débits : **8,278180 / 7,909803 / 7,878014 millions de périodes source/s**, soit **34,492416 / 32,957512 / 32,825057 %** de 24 MHz. Le coût du réseau est donc limitant avant même les récepteurs CPU/VIA/vidéo/disque. System 5 complet au moniteur sur le même code : 89,943281 / 87,672650 / 88,595791 ms par présentation, **22,200658 / 22,775632 / 22,538317 %**. Ce rapprochement situe un coût certain ; il n'attribue pas toute la durée machine à ce seul composant et ne remplace pas un profil d'appels. Les factorisations des opérations de fronts déjà inscrites au suivi restent à vérifier et mesurer avant rétention.

Diagnostic temporaire réussi et supprimé après consignation ; assembly de tests régénérée sans ce banc utilisant les ROM externes pour son autre partie. Aucun changement de production pendant ces mesures.

### Réutilisation des opérations de fronts du compteur — 9 novembre 2026

Le générateur et ses périodes complètes réutilisent RisingClock/FallingClock. Ces opérations transmettent chaque vrai front aux opérations canoniques locales de chacun des trois SN74S163. Le contrôle public par niveaux garde la suppression des niveaux répétés et les erreurs de période commencée à haut. Les validations d'entrée déjà garanties par une période complète ne sont plus répétées à chaque sous-appel. Capture des trois compteurs, résolution des trois, puis publication : ordre inchangé. Chaque compteur conserve intégralement sa phase, son total d'horloge, ses entrées et sa capture locale. Aucun état n'est calculé à sa place dans le générateur.

Le contrôle existant des avancements groupés compare désormais aussi le chemin public SetOscillator montant/descendant ; notifications et niveaux concordent pour 0, 1, 17, 48 et 1001 périodes. Avec compteur et WAIT/READY : **34 contrôles fonctionnels réussis, zéro échec/ignoré, 60 ms**. Le banc de débit est exécuté séparément, Debug normal, 24 000 périodes de chauffe puis trois fenêtres de 2 400 000 périodes, appel Action inclus, sans compilation ni autre test concurrents.

| Scénario | Avant les deux factorisations, ms | Après compteur et générateur, ms | Débit final, millions de périodes/s |
| --- | --- | --- | --- |
| Aucun récepteur externe | 280,025000 / 273,705300 / 276,083400 | 242,020600 / 250,261900 / 247,420700 | 9,916511 / 9,589954 / 9,700078 |
| Récepteurs 1 et 2 MHz | 299,152400 / 286,921300 / 283,033900 | 254,896300 / 259,536000 / 259,558900 | 9,415594 / 9,247272 / 9,246456 |

Baisse de durée hôte moyenne : **10,9 %** dans chacun de ces scénarios depuis la référence avant factorisation ; l'accroissement transitoire du coût du chemin général après le seul compteur est résorbé. Exactement 2 400 000 fronts source et 600 000 notifications pour les sorties observées dans chaque fenêtre. Aucun gain n'est déduit du seul meilleur passage.

System 5 complet au vrai NOS80, sans média ni commande, 40 présentations de chauffe puis trois fenêtres de 80 avec vidéo/audio : **76,259704 / 75,102638 / 76,012375 ms par présentation**, **26,184209 / 26,587615 / 26,269407 %** réels. Référence actuelle précédente identique au moniteur : 89,943281 / 87,672650 / 88,595791 ms, soit environ **14,6 %** de durée hôte en moins. Chaque fenêtre conserve 38 338 560 clocks source et 3 194 880 cycles CPU, ratio 12, vidéo 50,080128 Hz. La cadence Debug nominale n'est pas atteinte ; le coût de chaque front du réseau 24 MHz reste limitant.

Après mesure, le même montage charge BASIC depuis le disque existant via le SDK normal, 4 608 octets lus, SHA1 du programme chargé 6571FCA3E86E6DC99C2B56481979FBC24615BFF2 ; PRINT 12+34 affiche 46. Capture native 480×250 progressive inspectée : `artifacts/TestsMachines/Acorn/Acorn System 5/20261109-system5-canonical-edges.bmp`, copie TestsChips/Mos6502/Acorn System 5/20261109-canonical-edges.bmp. SHA256 identique aux passages précédents : 53D97862D9313589B6DDF4D6F149F094038FE64A8FF6C3C8F17F0D5485613A89. Les 177 contrôles System/disque/audio passent, zéro échec/ignoré, cinq secondes. L'import Interfaces absent du diagnostic a été corrigé avant son exécution, sans changement de production. Disque possédé détaché et libéré dans finally. Aucun fichier image disque modifié. Les deux sources de diagnostic sont retirées et leurs assemblies régénérées après consignation ; seules les preuves natives prévues sont conservées hors Git. Pas de livraison RML ni de mesure Release dans ce bloc.
