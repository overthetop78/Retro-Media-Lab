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

### Profil du 9 novembre — réseau avec récepteurs 12/2 MHz

Profil EventPipe dotnet-sampled-thread-time (échantillonnage des piles managées, pas compteur matériel de cycles CPU), fenêtre de huit secondes pendant 200 millions de périodes source. Répartition exclusive attribuée au dernier cadre réel de la puce, sans additionner parents et descendants : PublishOutputs du générateur 14,49 %, PublishOutputs du compteur 14,12 %, NextCount 10,83 %, Ic4Outputs 10,04 %, CaptureRisingClock 8,71 %, RisingClock 7,73 %, ResolveClock 6,40 %, Ic2Outputs 6,19 %, Carry 5,82 %, FallingClock 5,22 %. Ce profil oriente les recherches ; son attribution statistique ne prouve pas un gain d'une réécriture. Les fenêtres non instrumentées restent la preuve de débit.

Une tentative de conservation des trois niveaux CLR/ENP/ENT dans le câblage et d'accès direct aux niveaux publiés donne 277,4958 / 279,1492 / 275,7130 ms pour 2,4 millions de périodes, contre 273,1476 / 272,6236 / 280,0417 ms en référence. Aucun gain établi ; tentative retirée. Les variantes du compteur sont consignées dans Sn74S163.md, également retirées. Chaque fenêtre conserve exactement 1,2 million de fronts dot et 200 000 fronts caractère. Le réseau reste à optimiser ; ne pas présenter ces essais comme une résolution de la régression.

### Décodage des bits et destinataires observés — 9 novembre 2026

Le placement des trois sorties de chaque compteur dans le mot de niveaux utilise maintenant masques et décalages, sans ternaires par broche. Les mêmes bits QA/QB/QC sont transmis ; la capture de tous les compteurs puis leur résolution et publication ne changent pas. Une seule table conserve les sept destinataires. La publication parcourt les bits changés ayant un destinataire, en ordre ascendant comme auparavant. Après chaque callback, le masque d'abonnements est réévalué pour les seules sorties encore à parcourir : un destinataire ajouté plus tard dans l'ordre reçoit le front courant ; une sortie antérieure ne le rejoue pas. Aucun sondage du récepteur, agenda commun ou suppression de clock.

Fenêtres Debug normales sans RML ni compilation concurrente : 24 000 périodes de chauffe, puis trois fenêtres de 24 000 000 périodes par scénario. Chaque fenêtre vérifie le compte source et les notifications montantes : zéro sans observateur, 14 000 000 avec 12/2 MHz, 36 000 000 avec sept sorties. Scénarios exécutés successivement.

| Chemin | Aucun observateur, ms | 12/2 MHz, ms | Sept sorties, ms |
| --- | --- | --- | --- |
| Référence | 2410,2742 / 2368,6196 / 2352,1996 | 2749,1349 / 2795,0904 / 2765,9783 | 3110,9686 / 3097,4868 / 3105,5163 |
| Placement direct des bits seul | 2170,9333 / 2241,2307 / 2149,4298 | 2570,2054 / 2592,6212 / 2564,5558 | 2884,7232 / 2898,9812 / 2919,1915 |
| Placement direct + publication ciblée retenus | 2168,5047 / 2115,4204 / 2087,5744 | 2471,8279 / 2484,7555 / 2449,0333 | 2979,2748 / 2990,7101 / 3011,2597 |

Par rapport à la référence, diminution moyenne du temps d'environ 10,6 % sans observateur, 10,9 % avec les clocks 12/2 MHz du montage et 3,6 % avec sept sorties. La publication ciblée seule est moins favorable que le parcours fixe pour sept destinataires (environ +3,2 % par rapport à l'étape intermédiaire), mais l'ensemble reste plus rapide que la référence dans les trois scénarios. Aucun gain machine n'en est déduit.

Contrôles permanents : ajout d'un cas de branchement des sorties antérieures depuis une publication ultérieure, sans rejeu du front. Groupe compteur/générateur/trajet VDU80 : **34 réussites, zéro échec/ignoré, 59 ms en Debug**. Les autres cas conservent les sept clocks, leur phase, les fronts descendants, l'état résolu simultané, le branchement/débranchement pendant publication, le duty cycle 8 MHz et les seeds hors phase. Banc temporaire supprimé ; assembly régénérée sans lui, zéro erreur (avertissement xUnit2031 préexistant). Le débit 12/2 MHz isolé reste vers 9,66–9,80 millions de périodes source/s pour 24 MHz requis : optimisation du montage toujours ouverte, pas de clôture de System 5.

### Profil actuel de la machine complète — 9 novembre 2026

Profil EventPipe de huit secondes du moniteur NOS80 sur la machine complète après les optimisations de publication série et de ROM. Même RAM 2000–DFFF, VDU80 et 8271 sans média, rendu et drainage audio inclus ; 40 champs de chauffe et 240 champs exécutés. Compteurs vérifiés : 9 584 640 cycles de référence, 115 015 680 périodes source (ratio 12), empreinte framebuffer E9E458B2454C6AAD008F6EA0A02D78D406A9EBC80DA45D7057462F60FA99E76B. Le profil est instrumenté : aucune nouvelle vitesse nominale n'est déduite de sa durée.

Piles evented intégrées par intervalles ; attribution au dernier appelant réel de Chips ou du module, appels runtime inclus, sans additionner les coûts propres et inclusifs. Total attribué 8024,85 ms. Parts propres : SN74S163.PublishOutputs 9,52 %, SystemClockGenerator.PublishOutputs 8,71 %, VDU80.SetDotClock 6,69 %, SN74S163.NextCount 6,45 %, SystemVideo.SetDotClock 4,21 %, générateur.RisingClock 3,89 %, compteur.CaptureRisingClock 3,82 %, SN74LS165.ResolveGatedClock 3,74 %, compteur.ResolveClock 3,04 %, générateur.Ic2Outputs 2,58 %, compteur.get_Carry 2,28 %, générateur.Ic4Outputs 2,14 %. CPU ClockNmos 1,28 %, ExecuteNmosPhase 1,25 %, 8271Drive.Tick 1,17 %, TickReferenceClock 0,85 %. Les coûts faibles ne prouvent pas l'absence de défaut ; ils orientent l'optimisation.

Le coût principal reste donc le réseau des trois compteurs et de ses publications, pas le CPU. La publication du compteur calcule le RCO par un accesseur et compare deux champs séparés de dernière publication ; une représentation directe des broches publiées sera évaluée dans son suivi, en gardant le compte et la phase privés de chaque compteur. Le framebuffer représente 4,21 % dans ce scénario : il n'explique pas seul la lenteur. Aucun mode matériel ni front n'a été supprimé, aucun dynarec ajouté.

### Parcours critique compteurs et routage — 9 novembre 2026

Le compteur conserve son ENT sous forme de masque canonique, à la place du booléen. RCO est exactement ENT ET (Q=15), projeté par (compte+1)&masqueENT. La dernière publication tient dans un seul mot compte/RCO ; le calcul capturé et la publication résolue évitent les sous-appels NextCount, get_Carry et PublishOutputs à chaque front. Le destinataire est invoqué depuis son champ canonique, avec le même accesseur public. Compte, capture, horloge et compte de périodes restent propres à chaque SN74S163. Les 4096 combinaisons indépendantes et les cas de feedback/capture partagée sont conservés.

Le générateur mémorise uniquement les indices des prochains destinataires observés, recalculés aux changements d'abonnements. La publication parcourt ces indices dans l'ordre et consulte l'indice suivant après le callback : branchements et retraits au même front restent effectifs, sans rejouer une sortie antérieure. Ce cache de routage n'est pas un état matériel ni un ordonnanceur. Les fronts, captures et résolutions des trois compteurs restent inchangés.

Debug normal, chauffe 24 000 périodes puis trois fenêtres de 24 millions (1 s interne), aucun test/compilation concurrent.

| Variante | Réseau, récepteurs 12/2 MHz (ms) | Réseau + VDU synthétique (ms) |
| --- | --- | --- |
| Référence avant ce bloc | 2837,0274 / 2762,8523 / 2784,6698 | 4165,5247 / 4180,8090 / 4181,4692 |
| Compteur : broches et résolution intégrées | 2646,3759 / 2680,2011 / 2696,6161 | 3937,0670 / 3856,9520 / 4090,6708 |
| Essai de réception des deux fronts en une opération | 2669,2376 / 2629,2168 / 2646,5488 | 3937,8506 / 3954,3991 / 4025,5690 |
| Compteur avec destinataire direct + indices de routage | 2480,6366 / 2508,2260 / 2435,8936 | 3795,6925 / 3861,7357 / 3713,0588 |
| Variante table immuable de décodage des masques | 2542,3598 / 2543,1030 / 2545,3548 | 3785,0632 / 3758,8685 / 3808,0601 |

Variante retenue : compteur avec destinataire direct et indices de routage, environ 11,4 % de durée réseau en moins et 9,2 % de durée du montage en moins face à la référence. Ces gains concernent le bloc combiné ; ne pas les attribuer entièrement au seul routage. Le regroupement des wrappers de fronts n'établit pas de gain utile : retiré. La table immuable est moins favorable sur le réseau à deux destinataires : retirée. Aucune variante n'a supprimé de front ou de composant.

Empreintes de tous les dots identiques pour les cinq variantes : 19586B6F325B9EA6 / 0CC139C34DCAABA6 / B8853FE145B5B8A6. Chaque fenêtre conserve 12 millions de fronts montants dot et 2 millions de fronts montants caractère.

Contrôle des autres branchements, même compteur optimisé, trois fenêtres de 2,4 millions : routage initial sans observateur 209,7413 / 212,3085 / 211,2226 ms ; indices retenus 210,2018 / 212,8954 / 208,5326 ms. Sept sorties : initial 308,5136 / 334,6820 / 299,7805 ms ; indices 321,9640 / 307,9789 / 302,8348 ms. Aucun gain significatif ni régression nette dans ces deux scénarios ; 3,6 millions de fronts montants vérifiés par fenêtre avec sept sorties. Le gain utile est établi pour le montage à peu de destinataires de System 5.

Après suppression du banc et régénération, 75 contrôles compteur/générateur/VDU/RC/ROM/registre passent (99 ms), dont niveaux explicites contre périodes et branchements modifiés pendant publication. Mesure complète de la machine à réaliser après ce bloc ; aucune nouvelle cadence native n'est déduite du banc.

Vérification native après le bloc de coûts : System 5 NOS80, 101,742130–102,206670 ms/champ et 19,536885–19,626088 % en Debug. Durée moyenne réduite d'environ 11,6 % face au passage précédent ; compteurs et trois empreintes vidéo identiques. Ce gain porte sur le bloc combiné, pas sur une seule modification. Capture réelle inspectée, banc supprimé, 131 contrôles System/audio (4 s) et 75 contrôles de composants (99 ms) réussis après nettoyage. Conditions et tableau dans la validation System 5 ; machine toujours en cours.

### Raccordement des intervalles stables vérifié — 9 novembre 2026

Le trajet complet utilise les périodes stables sans dépasser les échéances locales et fronts observables. System 5 entier Debug : 74,691297 / 76,980154 / 76,304280 ms/champ ; référence 102,139359 / 102,206670 / 101,742130. Durée moyenne réduite de 25,5 % pour le bloc combiné, pas pour ce seul composant. Vitesse 25,94–26,73 % du nominal, source/CPU = 12, empreintes des trois champs identiques. Conditions et résultats complets : `docs/tasks/machines/AcornSystem/system5-validation.md`, passage « Intervalles stables raccordés ».

Après nettoyage des bancs et régénération : 96 contrôles de composants (414 ms), 135 contrôles System/audio (3 s) réussis. La comparaison des champs couvre curseur, inversion, entrelacement et caractères variables, avec mêmes niveaux et instants de notifications. Capture réelle inspectée, conservée dans `artifacts/TestsChips/SystemClock/Acorn System 5/20261109-stable-intervals-monitor-debug.bmp`, SHA256 162FD4E97CB8FF4D3D07BE20A79393C087D912C1559AEFE60DB9F951DFB9DA12. Aucun gain propre supplémentaire déduit des seules mesures machine ; optimisation globale encore ouverte.

Le regroupement ne commence qu'après la vraie resynchronisation IC4 avec IC2=2. Les profils d'entrée IC2 (/3), IC3 (ENP/ENT issus de QA) et IC4 (CLR issu du RCO) sont alors périodiques. Chaque compteur calcule son intervalle depuis son propre total CLK, en gardant les phases et priorités TI. Intervalle borné au prochain front 2 MHz (au plus six périodes source), uniquement si le récepteur 12 MHz accepte des périodes complètes sans changer sa sortie. Initialisations, refus, observateur 12 MHz supplémentaire ou autre sortie observée rétablissent les fronts unitaires. Les captures partagées précèdent les résolutions, puis publication de la frontière et front descendant. Le composant ne contient aucun agenda global. Les transitions internes représentées par les profils ne sont pas notifiées à un consommateur absent ; toutes celles qui sont demandées restent sur le parcours explicite. Les graines et branchements dynamiques sont comparés au parcours explicite dans les tests permanents.
### Contrôle simplifié des périodes unitaires — 9 novembre 2026

Après une période explicite terminant CLK12 haut, livrer directement la période source suivante si le budget le permet : aucun intervalle complet low-to-low ne peut commencer dans cet état. Les deux appels ClockPeriod et tous leurs callbacks restent exécutés ; seul le retour dans la boucle de qualification est évité. Les récepteurs peuvent toujours changer entre les deux fronts. Les 21 contrôles du générateur passent, dont les sept graines et abonnements dynamiques ; groupe complet 96 réussites, System/audio 135.

Mesure du bloc raccordé avec compteur et vidéo : durée moyenne 75,991910 → 73,052154 ms/champ, soit 3,9 % en moins, sans gain propre attribué à cette seule simplification. Détails, conditions et limites : [validation System 5](../../tasks/machines/AcornSystem/system5-validation.md), passage « Captures compactes et contrôle des intervalles ». Capture réelle inspectée sous TestsChips/SystemClock/Acorn System 5/20261109-packed-clock-monitor-debug.bmp, SHA256 162FD4E97CB8FF4D3D07BE20A79393C087D912C1559AEFE60DB9F951DFB9DA12. Bancs et traces supprimés, assemblies régénérées ; machine toujours en cours.

### Sortie 4 MHz et récepteur vidéo stable — 9 novembre 2026

La carte400,004 exige la vraie sortie4 MHz. Son abonnement désactivait tout le regroupement12 MHz, parce que StableReceiverMask n'autorisait que12/2 MHz. Le masque autorise maintenant4 MHz ; la frontière est le prochain changement de QA d'IC3 lorsqu'un destinataire y est raccordé, sinon le prochain changement2 MHz comme auparavant. La phase est dérivée des broches mémorisées d'IC2, sans compteur de division ajouté. Chaque compteur conserve ses captures et sa résolution. Le front de frontière reste publié par le chemin canonique ; aucun front4 MHz n'est supprimé ou retardé. Un observateur12 MHz supplémentaire ou une autre sortie non prise en charge continue de désactiver le regroupement. Sans destinataire4 MHz, ses transitions ne déclenchent pas un recalcul supplémentaire de frontière.

29 contrôles du générateur réussis,82 ms, dont huit nouveaux cas : sept phases initiales avec4/2 MHz comparées aux fronts explicites, et branchements/retraits4 MHz pendant une publication2 MHz. Chaque comparaison vérifie les dates, niveaux, ordre de notification, compteurs locaux, temps et fronts reçus du récepteur12 MHz ; un destinataire4 MHz invalide puis réactive aussi le regroupement.

Banc isolé Debug normal :24 000 périodes de chauffe puis trois fenêtres de24 millions, aucune compilation concurrente. Récepteur12 MHz synthétique acceptant les périodes stables, notifications4/2 MHz avec empreinte. Il ne simule ni la VDU complète ni les lecteurs. Comparaison entre le même code en parcours explicite et groupé, pas entre deux versions de machine.

| Passage | Fenêtre | Explicite (ms) | Groupé (ms) |
| --- | --- | --- | --- |
| Qualification4 MHz initiale | 1 | 2626,3703 | 2423,5354 |
| Qualification4 MHz initiale | 2 | 2605,9826 | 2439,2610 |
| Qualification4 MHz initiale | 3 | 2607,7468 | 2387,3707 |
| Version finale, recalcul borné aux sorties raccordées | 1 | 2765,2065 | 2470,2433 |
| Version finale, recalcul borné aux sorties raccordées | 2 | 2686,4794 | 2427,5167 |
| Version finale, recalcul borné aux sorties raccordées | 3 | 2719,2494 | 2443,1918 |

Première série :2613,366567→2416,722367 ms en moyenne,-7,5 %. Série finale :2723,6451→2446,983933 ms,-10,2 %. La variation du parcours explicite est conservée ; elle ne prouve pas un gain entre les deux séries. Chaque fenêtre, chauffe comprise :12 012 000 fronts4/2 MHz,12 012 000 montées dot et empreinte E1CBBCBF17370BB0 identiques ;8 007 984 périodes stables acceptées. Le premier essai sans sortie détaillée a réussi mais n'a pas fourni ses mesures : aucune valeur inventée pour cet essai. Le débit du System5 complet n'est pas actualisé par ces résultats.

Après suppression du banc temporaire et reconstruction de l'assembly :45 contrôles compteur/générateur réussis, zéro échec/ignoré,210 ms ; seul avertissement xUnit2031 préexistant. Preuve conservée : `artifacts/TestsChips/SystemClock/Acorn System 5/20261109-four-mhz-stable-results.md`. Aucun artefact temporaire créé par ce banc, aucun processus possédé laissé actif.

### Sortie4 MHz raccordée, 9 novembre
System5 branche désormais la sortie4 MHz existante sur les IC8/IC14 de400,004. Le générateur conserve son réseau physique, ses captures et les limites de regroupement lorsque cette sortie est observée. La carte reçoit les positions rationnelles de la source24 MHz ; aucune accumulation de périodes arrondies41 ns. La performance de la machine avec la carte complète reste7,4 % Debug ; aucun gain de générateur déduit de l'ancien montage sans ces circuits.

### Périodes du récepteur haut→haut — 9 novembre2026
La surcharge AdvanceStablePeriods(maximumPeriods,startHigh) refuse par défaut la phase haute pour les récepteurs historiques. Le générateur offre maintenant les périodes complètes depuis la phase reçue, bornées par la prochaine sortie2/4 MHz ; il ne force plus un retour au niveau bas entre offres. Les trois74S163 capturent toujours avant résolution et possèdent leurs propres états et clocks.
48 contrôles générateur/74S163 passent en206 ms : récepteurs historiques, récepteur haut→haut, plusieurs graines, abonnements2/4 MHz, niveaux et tous les comptes comparés au parcours source unitaire. Aucun gain annoncé avant raccordement au VDU80 réel et mesure native.

Résultat du raccordement complet :98 contrôles composants et135 contrôles System/carte/audio passent après suppression du diagnostic. Comparaisons de parcours natives Debug :7,32 % puis12,76 % de temps en moins pour la phase haute avec capture du front final ; les conditions, la variation hôte et le relevé sans adaptateur183,9065 ms/champ sont conservés dans `artifacts/TestsChips/SystemClock/Acorn System 5/20261109-high-phase-results.md` et `docs/tasks/machines/AcornSystem/system5-validation.md`. La machine reste très loin du nominal ; aucune livraison ni commit de ce passage.

### Captures163 : essai de représentation retiré —10 novembre2026
Trois représentations des broches/comptes163 ont été évaluées, avec48 contrôles de composant/générateur réussis pour chaque variante. Le comparatif direct final sur deux versions de Chips dans le même processus donne163,1101→163,16845 ms/champ, champs identiques : aucun gain machine. Le générateur lui-même n'a pas été modifié par cette piste. Toutes les sources163 initiales sont restaurées, les modèles et diagnostics nouveaux supprimés ;96 contrôles163/générateur/93/393 et21 contrôles carte/horloges passent après reconstruction. Conditions, variantes et limites dans `docs/chips/Logic/Sn74S163.md` et sa preuve20261110. Aucun gain ni nouvelle vitesse du code livré revendiqués.
### Regroupement jusqu'à la prochaine interaction2 MHz —10 novembre2026, premier passage
Le chemin4 MHz du System5 alimente la carte disque ; son retour au CPU passe par NMI. `Cpu6502.Interrupts.cs:SetNmi` ne fait que mémoriser le niveau et le front, sans exécuter de transfert. Les entrées caractère/VRAM et les transferts CPU sont déclenchés par `SetFastReferenceClock` à2 MHz. Ce câblage permet au récepteur vidéo stable de représenter ses périodes jusqu'à ce front, tout en conservant chaque frontière4 MHz et les états des trois163 à sa date.

Qualification ajoutée à SystemClock, conservative par défaut ; les destinataires inconnus gardent la borne4 MHz. Pendant un préfixe déjà représenté, protéger les mutations de raccordement ; à sa frontière elles redeviennent possibles. Les fronts12 MHz du préfixe sont déjà transmis par l'API de périodes stables ; seul le front supplémentaire est livré par le chemin de publication. Aucun oscillateur, agenda commun ou compteur physique dupliqué ; la limite de préavancement est une validation de contrat logiciel.

42 contrôles de générateur passent, dont10 nouveaux cas ;135 contrôles System/carte/audio passent. Comparatif des DLL complètes précédant le bloc et de la première candidate, dans le même processus et deux contextes collectables :20 champs de chauffe chacun,6 fenêtres alternées de10 champs natifs NOS80/VDU80 sans média, audio drainé. Référence163,3134/160,8647/159,7191/165,5379/164,3039/160,2478 ; candidate154,3595/151,3191/146,3400/153,5451/159,8027/147,4326 ms/champ. Moyennes162,3311→152,1332 (-6,28%), six fenêtres favorables. Tous les champs et comptes Quartz/FDC/référence correspondent. Les deux DLL effectivement chargées sont contrôlées ; fermeture et collecte des contextes vérifiées. Résultat intermédiaire, pas de clôture de la cadence System5 ; la validation avec média et le nettoyage restent à réaliser.
### Publication finale et logiciel disque —10 novembre2026
Le code retenu conserve la publication scalaire ordinaire. Seuls les blocs12 MHz déjà représentés ajustent la métadonnée privée de dernière publication avant celle-ci : les broches physiques ne sont jamais inversées, les fronts représentés ne sont pas rejoués et le seul front supplémentaire réel est livré une fois. Aucun gain propre à cette retouche isolée n'est revendiqué.

Comparatif final de versions complètes dans un même processus,20 champs chauffe par instance,6 fenêtres alternées de10 champs, même NOS80/VDU80/RAM2000–DFFF/400,004 sans média et drainage audio : référence157,3608/158,0561/158,0934/156,7040/158,4189/160,3742 ; candidate149,1968/149,7304/148,8807/150,6966/160,0418/148,8083 ms/champ. Moyennes **158,1679→151,22577 ms/champ,4,389% de temps en moins**,5 fenêtres favorables sur6. La première série donnait6,28% ; toutes les fenêtres, y compris défavorables, sont conservées. Chemins chargés et attributs Debug DisableOptimizations des quatre DLL contrôlés. Champs, clocks Quartz/FDC/référence identiques ; fermeture et collecte des deux contextes vérifiées.

Validation du code final avec SSD original ouvert en lecture seule via le SDK : CAT, BASIC, PRINT12+34 par les entrées hôtes,4608 octets lus ;4096 octets àC000 avec SHA1 exact6571FCA3E86E6DC99C2B56481979FBC24615BFF2, calcul46 et prompt visibles. Capture native réellement inspectée480×250 : `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-independent-clock-basic.bmp`. Image source inchangée après fermeture, SHA25669E6E3B3D5C83E4025BAC73161DA74C4B569EDA935E764C992769D84A27209DF. Pas d'injection d'état CPU.

42 contrôles de générateur dont10 nouveaux ;135 contrôles System/carte/audio sur le premier raccordement ;77 contrôles163/générateur/VDU/curseur sur la publication finale. Après ce bloc :156 contrôles composants passent (599 ms),135 contrôles System/carte/audio passent. Finale native6,613 champs/s pour50,080 Hz matériels,264,08 kcycles/s effectifs sur2 MHz, **13,204% du nominal**. Cette mesure hors interface ne représente pas les FPS affichés par RML. System5 reste en cours pour performance ; aucun nouveau build utilisateur ni commit.

Données complètes : `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-independent-clock-results.md`. La qualification vient du câblage réel : NMI est mémorisé sans transfert CPU immédiat, caractère/VRAM/CPU restent bornés par2 MHz. Aucun agenda commun ni compteur physique dupliqué ; la limite de préavancement valide seulement le contrat logiciel. Les diagnostics et copies de DLL sont retirés après consignation.
Nettoyage final du bloc10 novembre : diagnostic et quatre snapshots DLL supprimés après consignation, assembly de tests reconstruit sans la fabrique de diagnostic.156 contrôles composants et135 contrôles System/carte/audio réussis sur le code retenu ; reconstruction sans diagnostic0 erreur,4 avertissements de tests déjà présents. Capture et rapport de mesures conservés hors Git. Aucun nouveau commit ni livraison utilisateur ; performance globale toujours ouverte.
### Récepteur indépendant à pixels variables —10 novembre2026
Le contrat `IIndependentClockPeriodReceiver` complète `IStableClockPeriodReceiver` : consommation exacte de périodes, même lorsque les sorties du récepteur changent, uniquement si ces sorties ne consultent ni n’affectent la source ou les autres domaines avant la frontière annoncée. La source conserve ses trois163 et borne l’intervalle aux fronts4/2 MHz selon les dépendances déclarées. Le récepteur vidéo conserve chaque front local et chaque pixel ; seules ses plages déjà stables utilisent le chemin arithmétique existant. Aucune période CPU, aucun délai ou mode matériel changé ; aucun agenda commun ni nouveau réglage utilisateur.
Le générateur essaie d’abord le préfixe stable existant, puis confie le reste au récepteur indépendant. Les liaisons restent protégées pendant cet intervalle. Les autres récepteurs conservent le parcours historique. Les notifications vidéo sont horodatées par la puce, jamais par l’avance temporaire de la source. Un futur raccordement dépendant des pixels doit respecter ou réduire cette frontière avant de bénéficier de ce contrat.
Validation du code retenu :45 contrôles générateur et53 contrôles vidéo/carte réussis. Trois graines supplémentaires comparent les fronts12/4/2 MHz, tous les comptes et les changements de liaison interdits. Les contrôles vidéo préexistants comparent les transitions datées et huit champs successifs, avec inversion et entrelacement. La comparaison native dans un même processus utilise deux cœurs NOS80/VDU80, RAM2000–DFFF, carte400,004 sans média,20 champs de chauffe puis3×20, ordre des mesures alterné. Le témoin désactive seulement le cache de capacité logicielle ; aucune RAM ni horloge matérielle modifiée. Vidéo/audio et durées natives identiques pour chaque fenêtre, mêmes trois SHA256 que la référence.
Mesures appariées ms/champ :144,80774→143,53322 ;144,98950→142,58179 ;144,73069→141,96234. Moyennes144,84264→142,69245, soit1,4845 % de temps en moins. Le premier candidat ajoutant un appel pour toutes les périodes donnait168,82105 ms/champ sur des fenêtres non appariées ; le préfixe stable est donc conservé directement dans la source pour limiter cette surcharge. Les premières fenêtres entre processus variaient fortement et ne servent pas à attribuer le gain.
Le chemin est conservé pour son gain apparié et son regroupement exact des sorties variables, mais ce résultat reste minime et ne clôture pas System5 :7,0081 champs calculés/s,13,9937 % nominal hors interface, champ matériel19,968 ms. Le retour utilisateur6 FPS reste distinct. Aucun gain significatif ajouté au tableau Application aux composants, aucun build applicatif ni commit de ce seul bloc. Rapport : artifacts/TestsMachines/Acorn/Acorn System 5/20261110-independent-periods-paired.md. Diagnostic et copie source supprimés après consignation ; projet local reconstruit sans eux : zéro erreur, quatre avertissements préexistants.


### Transport de la sortie périodique4 MHz —10 novembre2026
Après la qualification existante de la séquence /3 et de la synchronisation physique du réseau, IC3/QA annonce au raccordement son prochain front descendant, une période de6 fronts24 MHz et une durée haute de3. Les compteurs restent canoniques dans les163 ; la description n'arrête aucune horloge et ne supprime aucun observateur. Le propriétaire d'un fil peut retirer son seul transport scalaire après transmission de la phase à ses récepteurs. Aucun motif n'est annoncé pendant les états de démarrage non encore qualifiés. Sources physiques : Handbook figure9 et chronogramme déjà archivés ci-dessus.
Contrôles actuels :48 cas SystemClock,18 cas93 et47 cas393, soit113 réussites, zéro échec/ignoré,465 ms. Trois phases initiales du réseau sont comparées à tous les fronts explicites après l'annonce, publication unique et phase de démarrage conservées. Aucun gain machine encore annoncé.

### Point de reprise du transport périodique4 MHz
Le premier raccordement machine complet a48 contrôles carte/horloges réussis, y compris lecture/écriture/relecture avec les deux transports. La comparaison native Debug reste défavorable :166,03954→168,74964 ms/champ (+1,632 %), empreintes vidéo/audio égales. Ne pas considérer la simple description du signal4 MHz comme une accélération obtenue ; les réveils du récepteur aval restent à refondre. Aucun changement des fréquences ni des options Debug. Le commit complet demandé constitue un point de reprise, pas une clôture System5.

### Réseau qualifié résolu directement
49 contrôles générateur réussissent, zéro échec/ignoré,171 ms. Les trois163 résolvent chacun leur capture périodique dans leur propre état canonique ; leurs liaisons internes déjà décrites ne sont pas rejouées par callbacks, et les niveaux externes sont réunis seulement après résolution des trois états. Les fronts de sortie12/4/2 MHz et les descentes CLK restent représentés. Les comparaisons de phases/graines et d'ordres des fronts existantes passent ; un cas supplémentaire revient du réseau groupé aux sept sorties explicites après10001 périodes et compare1001 fronts supplémentaires, états, RCO et comptes identiques. Aucun gain isolé annoncé ; comparaison sur le bloc complet.

## Comparaison du bloc architectural complet —10 novembre2026
Le générateur résout directement les états canoniques des trois163 depuis les motifs d'entrée déjà établis, puis publie les seuls niveaux externes de la frontière ; aucun fil interne décrit n'est rejoué. Le393 projette localement le compte QA entre deux effets raccordés au lieu de matérialiser chaque ripple lors de la synchronisation8271. Le bus de carte retient ses pins et ne transmet plus les variations à8271 pendant une désélection déjà effective ; la sélection et sa libération restent transmises. Les trains IC14/QA→IC7/CKA et IC7/QA→CKB conservent leurs révocations/reset et repli explicite.
Contrôles :50 cas393 (894 ms),16 cas163 (432 ms),49 cas générateur (171 ms),49 cas carte/horloges (16 s), tous réussis sans échec/ignoré. Le contrôle393 couvre les projections non mutantes et leur refus aux frontières qualifiées ; les contrôles163 et générateur comparent aussi le retour au parcours explicite. Les contrôles de carte conservent les bus/mirroirs, NMI, resets, fenêtre et lectures/écritures/relectures128 octets.
Comparaison native appariée dans le même processus : deux contextes collectables isolent chacun leur couple Acorn/Chips. Référence : les cinq sources source-clock/carte du point63d77fc27 ; les autres sources (dont les nouvelles API inutilisées dans ce témoin) et la fabrique sont identiques. Aucun réglage matériel ni option Debug différent ; DisableOptimizations vérifié dans les quatre DLL.20 champs de chauffe puis3×20, ordre alterné, vidéo/audio inclus et empreintes hors chronométrage.

| Fenêtre | Référence ms/champ | Candidat ms/champ |
| --- | ---: | ---: |
|1 |177,58559 |115,42187 |
|2 |177,71615 |113,45575 |
|3 |178,55178 |114,99553 |
| **Moyenne** | **177,95117** | **114,62438** |

**Temps réduit de35,587 %, accélération1,5525×.** Les trois empreintes vidéo/audio et les durées émulées sont identiques ; les deux cœurs sont fermés et les deux contextes effectivement déchargés après la mesure. Champ matériel19,968 ms à50,08013 Hz ; candidat **8,72415 champs/s et17,42038 % nominal**, sans limite et hors RML. Cette refonte n'atteint ni la réduction50 % demandée ni100 % de cadence. La machine reste ouverte ; les avances entre interactions et les coûts globaux restants nécessitent une poursuite structurelle, pas une déclaration de clôture.
Rapport ignoré : `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-architecture-block-native.md`. Les cinq sources courantes ont été sauvegardées en octets avant la construction témoin et restaurées dans finally, puis le candidat a été reconstruit. Fabrique, diagnostics externes et copies DLL sont retirés après consignation ; le rapport reste conservé.
