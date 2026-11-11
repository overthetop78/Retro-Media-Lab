# Intel 8271 / 8271-6

## Périmètre et architecture

Passage du 7 novembre 2026 : les 158 tests propres au 8271 réussissent en Debug, zéro échec ni ignoré, 566 ms. Ils couvrent notamment le relâchement simultané WR/CS, les phases de registre, l'avancement groupé comparé aux clocks unitaires, les fronts de positionnement et les transferts FM. Aucun résultat de machine complète n'est déduit de ce seul groupe. Avertissement préexistant xUnit2031 dans `Shared/ArchitectureTests.cs`, hors du composant testé.

Le passage intégré suivant donne 136 réussites Debug, zéro échec ni ignoré, 5 s : groupe `Emulation.Intel8271` et `AcornEmuDiskControllerTests`. Il réexerce les pistes physiques, interruptions d'écriture, formatages, deux lecteurs/faces, cycles fractionnaires et raccordements existants. Les avertissements de compilation CA1416 d'autres modules, CS8604 Sony et CS0649 des infrastructures de test sont conservés ; aucune erreur de compilation.

### Lecture réelle sur le Model A — 7 novembre 2026

Diagnostic sans RML, carte complète Debug non optimisée, sans transport réseau ni sortie audio Windows. MOS `os12.rom`, BASIC `basic2.rom`, caractères `saa5050`, DFS `dfs0.9h.rom` en banque 14, contrôleur 8271, entrées/sorties du Model A conservées (`expandedIo: false`). Deux montages : RAM 16 Kio native puis 32 Kio avec extension. Aucun réglage utilisateur modifié. Le SSD `Model A Invaders (1982)(IJK)[INVA].ssd` du corpus TOSEC 2024 est ouvert en lecture seule par le SDK, détaché dans `finally` puis libéré. Saisie par le clavier émulé, sans injection du logiciel en RAM ni modification du PC.

`*DISC`, `*CAT` lisent 512 octets et affichent `model a`, Drive 0, `INV/A`, puis rendent l'invite. `CHAIN "INV/A"` lit 9 728 octets au total et atteint le menu des quatre options en 16 comme en 32 Kio. En 32 Kio, l'option 4 et Espace atteignent l'écran de partie avec les rangées d'envahisseurs, le score et trois bases ; capture inspectée `20261107-012447-32k-jeu-carte-sans-interface.bmp`. Cela prouve le chargement et l'entrée dans ce programme ; aucune écoute du son ni validation exhaustive de son rendu n'est déduite de cette image.

Mesures sans limite de vitesse : 120 trames de chauffe au démarrage, puis 10 de chauffe et 100 mesurées par scénario, `RunFrame` et `DrainAudio` inclus. Les passages répétés ci-dessous servent à distinguer la variabilité de mesure et la correction de saisie du banc ; aucune optimisation du contrôleur n'a été ajoutée pendant ce passage et aucun gain avant/après n'est revendiqué.

| RAM / scénario | ms/image observées | FPS calculés | MHz effectifs / vitesse réelle |
| --- | --- | --- | --- |
| 16 Kio / repos, deux premiers passages | 18,733 / 18,594 | 53,38 / 53,78 | 2,132 / 106,6 % ; 2,148 / 107,4 % |
| 16 Kio / menu chargé, deux premiers passages | 18,603 / 19,096 | 53,76 / 52,37 | 2,147 / 107,3 % ; 2,091 / 104,6 % |
| 32 Kio / repos, trois passages | 18,974 / 19,210 / 19,204 | 52,70 / 52,06 / 52,07 | 2,105 / 105,2 % ; 2,079 / 103,9 % ; 2,080 / 104,0 % |
| 32 Kio / menu chargé, trois passages | 18,631 / 18,955 / 18,663 | 53,68 / 52,76 / 53,58 | 2,144 / 107,2 % ; 2,107 / 105,3 % ; 2,140 / 107,0 % |
| 32 Kio / après démarrage option 4, deux passages | 19,395 / 19,723 | 51,56 / 50,70 | 2,059 / 103,0 % ; 2,025 / 101,2 % |

Le budget matériel reste environ 19,968 ms/image à 50,0801 Hz, 3 993 599 à 3 993 603 cycles par lot de 100 images selon la frontière d'instruction. Ces nombres décrivent le calcul hors interface ; ils ne sont pas des FPS présentés dans RML. Les scénarios ci-dessus restent sous ce budget sur ces passages. La mesure après sélection en 16 Kio ne prouve pas une partie : le menu est resté visible lors du premier essai, ce point demeure distinct de la lecture disque réussie.

Captures brutes du cœur, sans retouche, copiées dans `artifacts/TestsChips/Intel8271/BBC Micro Model A/` et `artifacts/TestsMachines/Acorn/BBC Micro Model A/` : `20261107-012314-16k-catalogue-carte-sans-interface.bmp`, `20261107-012319-16k-programme-carte-sans-interface.bmp`, `20261107-012329-32k-catalogue-carte-sans-interface.bmp`, `20261107-012334-32k-programme-carte-sans-interface.bmp`, puis les étapes `012421/012424` en 16 Kio et `012443/012447` en 32 Kio, et le contrôle de saisie `012519/012525/012529/012532` en 32 Kio. Ce sont des trames hors interface, pas des captures de fenêtre RML.

Le premier montage du diagnostic passait directement la ROM DFS de 8 Kio à une banque de 16 Kio et a été refusé par `SidewaysRoms`. Le banc utilise désormais `BbcMicroFirmwareFunctions.GetRomBytes`, comme le chargement normal ; aucune modification de production n'était nécessaire. Le premier générateur de saisie utilisait la touche A pour les chiffres avec le caractère hôte : la confirmation suivante emploie D4 avec son caractère, sans changer le clavier émulé. Les assemblies de ce banc sont reconstruites avec leur source préexistante après suppression du diagnostic ; les captures restent hors Git.

Confirmation 16 Kio avec D4 physique : le menu reste affiché après l'option 4 et Espace, y compris après les 100 images suivantes. Les captures `20261107-012615-16k-programme-carte-sans-interface.bmp`, `012619-16k-option4`, `012622-16k-jeu` et `012624-16k-jeu-apres-mesure` conservent cette limite ; la cadence mesurée de 18,436 ms/image concerne donc toujours le menu, pas une partie active. Repos : 18,964 ms/image, 52,73 FPS, 2,106 MHz (105,3 %) ; menu avant sélection : 19,184 ms/image, 52,13 FPS, 2,082 MHz (104,1 %). Aucune cause matérielle n'est établie par cette différence avec la RAM 32 Kio. Le nom du fichier ne suffit pas à valider sa compatibilité en 16 Kio.

Le composant numérique est dans `RetroMediaLab.Chips/Controllers/Intel/FDC/8271`. Il reçoit l’horloge du propriétaire, les cycles de bus et les niveaux des broches. Il ne connaît ni `IDiskImage`, ni format de fichier, ni piste sectorielle synthétique, ni rotation de lecteur. Les anciens fichiers Transfers/SectorAdapter/SectorWrites/SectorFormatting sont supprimés.

`IIntel8271` expose les registres, CS/RD/WR/DACK, données de bus à haute impédance, DRQ/NMI, Reset, Ready, protection, Fault, Track Zero, Index, Count, Unseparated Data, Data Window, PLO/SS, InSync et les fronts Write Data. `DriveControl` expose les sorties du lecteur ; leur utilisation physique appartient à la carte. Le bit Optional Output ne sélectionne aucune face intrinsèquement.

Le câblage `Intel8271Drive` est dans les services d’AcornEmu et est utilisé par BBC Micro, Atom et Acorn System. Il fournit les impulsions depuis les pistes reçues du SDK, avance la mécanique et reçoit les sorties Step/Direction/Write Enable/Write Data. Le BBC fournit sa fonction de sélection de face ; Atom/System conservent la face zéro. Le temps fractionnaire est conservé aux horloges propriétaires de 1, 2 et 3 MHz. Les images restent empruntées au propriétaire de la machine.

MediaEngine construit les pistes avec les encodeurs existants, conserve l’ordre physique, les doublons, C/H/R/N, marques et défauts d’intégrité, puis fusionne les fenêtres réellement écrites. Une piste trop longue n’obtient pas une rotation plus lente. Une fin silencieuse ne fabrique pas de transition. Les dommages d’écriture interrompue restent sur la piste ; un conteneur ne pouvant les représenter refuse la publication au lieu de les effacer. Le SDK relaie ces opérations sans interpréter les registres du contrôleur. Aucun nouveau Common n’est ajouté.

## Références examinées

- Intel, *8271/8271-6 Programmable Floppy Disk Controller*, AFN-00223B, pages 8-115 à 8-143 : https://www.theoddys.com/acorn/semiconductor_datasheets/D8271.pdf. Les tableaux de broches, commandes, registres, résultats et chronogrammes constituent la référence principale.
- JSBeeb, `src/intel-fdc.js` : https://github.com/mattgodbolt/jsbeeb/blob/main/src/intel-fdc.js ; commandes, microcode et raccordement BBC.
- B-Em, `src/i8271.c` : https://github.com/stardot/b-em/blob/master/src/i8271.c ; commandes et carte BBC.
- MAME, `src/devices/machine/i8271.cpp` : https://github.com/mamedev/mame/blob/master/src/devices/machine/i8271.cpp ; séquenceur, Scan, flux et DMA.

L’accord de plusieurs émulateurs ne suffit pas à établir un comportement matériel : ils peuvent partager une erreur. Par exemple, la conservation des quatre octets CHRN de Format, dont N indépendant de la longueur DATA commandée, est fondée sur Intel 8-133. L’arbre b2 examiné ne fournissait pas d’implémentation 8271 exploitable ; aucune affirmation de fidélité ne repose sur b2.

## Fonctions implémentées et vérifications

| Domaine | Comportement et contrôles effectués |
|---|---|
| Bus CPU | Toutes les combinaisons CS/RD/WR/DACK ; haute impédance hors sélection ; lecture unique par strobe ; écriture sur relâchement ; DACK indépendant de CS/adresse ; écriture maintenue et perte de sélection ; buffers CF/PF distincts et écrasement PF documenté ; acquittements distincts du résultat et des données. |
| Registres et Reset | Mode C0 initial et bits fixes ; Read/Write Special ; Specify timing et mauvais tracks ; Reset maintenu bloque les commandes/DMA et libère les sorties ; niveaux externes conservés. Les commandes busy ne remplacent pas la commande acceptée. |
| Signaux de lecteur | Deux sélections natives ; Ready mémorisé jusqu’à Read Drive Status, sans acquittement par Read Input Port ; protection/Fault pendant commande ; Track Zero distinct du registre de piste ; index et Count consommés sur fronts ; choix de face externe. Les quatre couples lecteur/face BBC sont exercés. |
| Positionnement | Direction avant Step ; impulsion Step de 10 µs ; pas/stabilisation/chargement standard et mini ; délai mécanique accélérable sans accélérer le transfert ; Step Rate zéro attend Count ; mauvais tracks successifs ; recalibration limitée à 255 pas ; Low Current à 43 ; single/double actuator ; déchargement par nombre d’index, zéro immédiat et quinze sans déchargement automatique. |
| Lecture FM | Synchronisation sur deux octets zéro ; marques C7 et D7 ; ID et DATA issus des impulsions ; clocks manquantes dans les octets ordinaires ; CRC ID et DATA réellement reçus ; recherche bornée par index ; ordre physique et doublons ; deux reprises de piste après mauvais C ; secteurs consécutifs ; résultat et secteur fautif conservés. |
| Tailles | Tous les codes N de 0 à 7, de 128 à 16384 octets, sont exercés sur les broches à 2 et 4 MHz, avec index pendant DATA. Une piste média surchargée reste tronquée à sa durée physique ; le test sur fil ne prétend pas augmenter sa capacité. |
| Read/Verify/Read ID | Read Data ignore les marques deleted en les comptant dans la commande ; Read Including Deleted transfère et conserve le résultat deleted ; Verify contrôle CRC sans transfert CPU ; Read ID attend l’index et rend C/H/R/N, ordre et doublons. Un autre ID ne peut prêter ses données à un ID précédemment reconnu. |
| Write | Marque FB ou F8 explicite ; sync, clocks, payload, CRC et postambule réellement émis ; acquittement CPU distinct de l’émission physique ; porte activée avant le premier front et maintenue après le dernier ; interruption par Reset, Fault, protection ou Late DMA ; aucune sauvegarde ou destruction d’image par la puce. |
| Format | Attend Index ; GAP5 et marque d’index D7 en standard, mode mini sans GAP5 ; gaps et IDs fournis par CPU, dont N ; DATA E5 selon la longueur commandée ; CRC réels ; un index ne coupe pas un champ long ; GAP4 attend l’index final. Les interruptions dans ID/DATA et les anciens champs non atteints sont conservés sur la piste. |
| Scan | EQ/GEQ/LEQ, joker FF, clés fixes, blocs et pas de secteur ; résultats SME/SNM/SNME et compteurs ; poursuite au secteur suivant ; contrôle du CRC final même après correspondance. |
| DMA/non-DMA | DRQ avec DACK en DMA ; demande et interruption par octet uniquement en non-DMA ; interruption de fin dans les deux modes ; limites du buffer et cadence selon l’horloge ; échéance DMA exacte de 124 clocks contrôleur en lecture et écriture, acquittement juste avant la limite et erreur Late DMA à la limite. |
| Single-shot/PLO | PLO/SS distinct ; Data Window échantillonné au front d’assertion ; single-shot distingue demi-cellule et cellule entière ; PLO utilise la phase Clock/Data ; acquisition des deux polarités, premier front reçu sur DATA et reprise de phase entre ID/DATA ; InSync émis et relâché aux transitions du séquenceur. |

Les tests propres aux broches sont dans `tests/RetroMediaLab.Chips.Tests/Controllers/Intel/FDC/8271/`. Les contrôles de pistes et du montage du module sont dans `tests/RetroMediaLab.LocalEmulationTests/Emulation/Intel8271/`, avec des médias autonomes en mémoire réutilisant les codecs MediaEngine.

## Timings et limites du modèle

- Standard : horloge 4 MHz ; mini : 2 MHz. Un bit FM utilise 16 clocks et un octet 128 clocks. Les horloges propriétaire et contrôleur sont distinctes.
- Intel 8-118 fixe l’échéance DMA à 31 µs en standard, soit 124 clocks. Le fonctionnement mini est obtenu par la même séquence cadencée deux fois moins vite ; les tests contrôlent le comptage, sans présenter le texte Intel comme une mesure de latence analogique.
- Write Enable utilise un délai nominal de 2 µs, dans l’intervalle 1–3 µs de la figure 10 ; la largeur des impulsions Write Data est une clock contrôleur. Les délais et la séquence sont vérifiés aux deux horloges.
- La figure 6, page 8-120, donne 2,85 µs pour le single-shot standard et 5,7 µs en mini. Le câblage emploie ces valeurs, remplaçant le seuil médian provisoire. Le séparateur et le PLO analogiques sont externes à la puce ; leur bruit, tolérances électriques et verrouillage analogique ne sont pas simulés. Le protocole numérique PLO/SS et les deux polarités sont testés.
- La fiche ne publie pas la latence complète du microcontrôleur interne entre consommation des buffers CF/PF. Leur consommation est modélisée à une clock nominale, avec buffers observables et strobages séparés. Ce choix n’est pas présenté comme une mesure du silicium.
- Les exigences électriques et de setup/hold supposent des niveaux fournis correctement par le câblage. La garantie constructeur exige un Reset actif pendant au moins onze clocks ; aucun comportement de silicium non spécifié pour une impulsion trop courte n’est inventé.

Le raccordement d’une écriture ne force pas la nouvelle phase sur les anciens octets du conteneur. Les contrôles de coupure vérifient chaque impulsion effectivement émise et l’identité des transitions hors porte. Ils ne déduisent pas abusivement un octet partiel sans tenir compte de cette phase. La borne finale d’un morceau continu appartient au morceau suivant ; un instant déjà traité avant fermeture/Reset est inclus explicitement, sans ajout de temps fictif.

Un défaut réel de relecture a été corrigé : exiger un intervalle exactement égal à la cellule entière classait mal des transitions légèrement déphasées au raccordement. Le single-shot utilise maintenant sa fenêtre documentée. La relecture d’une écriture privée de données conserve son préfixe, l’ancienne fin et le CRC fautif, puis une écriture complète rétablit une piste publiable.

### Reprise de la revue après MC6847 — 11 novembre2026

L'utilisateur confirme de conserver et consigner les limites actuelles : consommation CF/PF à une clock nominale faute de latence interne publiée, délais de sortie nominaux et séparateur/PLO analogique externe non simulé. Ce choix ne constitue pas une mesure matérielle de ces points. Les références Intel AFN-00223B déjà consignées restent la référence fonctionnelle ; l'archive PDF est conservée dans `artifacts/.tmp-archives/chips/Controllers/Intel/FDC/8271/`. Son extraction texte locale ne contient que des séparateurs de pages : ne pas la présenter comme un texte constructeur exploitable ; réutiliser la fiche et les scans.

Relecture de Clock/SerialRead/SerialWrite/Bus/Registers et des suites correspondantes : les compteurs et phases restent dans le composant, les entrées sont mémorisées aux méthodes de broches, les notifications sont publiées depuis leurs causes. AdvanceTo regroupe repos/reset/délais et préfixes de lecture ; chaque frontière reste dans TickCore. L'écriture demeure unitaire après l'essai mesuré et retiré du10 novembre ; cet essai n'est pas répété. Les contrôles actuels couvrent également l'horloge propriétaire différente du contrôleur, les strobages CF/PF/DACK et les délais DMA, sans fixer les latences non publiées depuis un émulateur.

Premier banc de cette reprise :165 contrôles Debug hors états réussissent, zéro échec/ignoré. Comparaison isolée du transport FM128octets et des avances existantes, soixante fenêtres Tick/AdvanceTo identiques sur données, résultats, compteurs et notifications datées. Read groupe les préfixes ; l'invocation AdvanceTo pour une seule période écrite montre un surcoût. La sélection canonique d'un seul Tick doit être examinée avant de changer le composant. Le premier lancement n'a pas enregistré de rapport, car le dossier de sortie AcornAtom manquait ; invocation corrigée en créant ce dossier, sans modification de production. Les2048octets par fenêtre de64transferts sont les tableaux params du banc, pas attribués à la puce ; les séparer avant le relevé final. Rapports `artifacts/TestsChips/Intel8271/AcornAtom/20261111-review-first-*` ; aucune clôture à ce stade.

### Résultat retenu de la revue Atom — 11 novembre2026

`Intel8271.Clock.cs` utilise maintenant le Tick canonique pour une avance d'une seule période ; l'échéance série d'écriture, déjà obligatoirement à la période suivante, est retournée directement. Le Tick et ses publications restent uniques, avec leurs compteurs et fractions dans le composant. La lecture garde ses préfixes et l'écriture ses clocks ordinaires ; aucune extension du regroupement écriture retiré le10 novembre n'est réintroduite. Cette simplification réutilise les parcours canoniques et les qualifications déjà établies dans le tableau de la procédure.

**165 contrôles Debug et165 Release hors états réussissent, zéro échec/ignoré**, avec les suites existantes Bus/Register/DriveSignal/Clock/SerialRead/SerialWrite. Les cas actuels couvrent notamment avances d'une période, buffers, reset, échéances, IRQ/DMA, toutes tailles N, transferts complets, phases et notifications datées. Aucun nouveau test redondant. Les comparaisons diagnostiques d'états déjà présentes sont conservées sans modification des API de sauvegarde/chargement. Rapports `20261111-review-final-{debug,release}.trx` sous TestsChips/Intel8271/AcornAtom.

Banc isolé Debug .NET10 aux paramètres ordinaires : même transport FM issu des tests de broches, horloge d'entrée2MHz, contrôleur2/4MHz, DMA/non-DMA. Une fenêtre de chauffe puis cinq chronométrées, trois séries rapprochées référence Tick / actuel Tick / référence AdvanceTo / actuel AdvanceTo, sans test/build concurrent. Repos et recherche sans flux :1M périodes par fenêtre. Lecture/écriture :64 commandes FM réellement menées à terme, ID/CRC,128octets, acquittements, résultat0 et notifications NMI/Drive/WriteData inclus. Les commandes du banc utilisent Write direct ; ce débit n'inclut ni CPU, ni mécanique, ni strobages CF/PF, ni média, ni rendu. La vérification des demandes fait partie du coût du banc. Tableaux de paramètres et chronomètre préparés hors du compteur d'allocations.

**540 fenêtres identiques** entre les parcours comparés sur données et résultats, temps d'entrée, états publiés, compteurs, comptes de notifications et empreinte de leur date/valeur. Toutes les boucles finales mesurées allouent **0octet**. Les soixante fenêtres de la première paire étaient déjà identiques, mais les2048octets des tableaux params du banc y étaient inclus ; cette invocation n'est pas assimilée à une allocation du composant.

Durées ci-dessous en **ms par transfert128octets** : médiane des trois médianes de fenêtres, divisée par64. Réductions calculées par paire ; elles ne sont pas le quotient des deux médianes affichées.

| Charge | Entrée | Avant | Après | Réduction appariée |
|---|---|---:|---:|---:|
| Lecture 2MHz DMA |Tick|1,6854|1,7223|0,65 %|
| Lecture 2MHz non-DMA |Tick|1,6440|1,7154|-2,32 %|
| Lecture 4MHz DMA |Tick|0,9338|0,9742|-2,90 %|
| Lecture 4MHz non-DMA |Tick|0,9665|1,0178|-3,51 %|
| Écriture 2MHz DMA |Tick|2,0564|1,5568|24,37 %|
| Écriture 2MHz non-DMA |Tick|2,0614|1,5246|25,64 %|
| Écriture 4MHz DMA |Tick|1,1565|0,9031|23,50 %|
| Écriture 4MHz non-DMA |Tick|1,1422|0,9039|22,53 %|
| Lecture 2MHz DMA |AdvanceTo|0,5598|0,5830|-3,55 %|
| Lecture 2MHz non-DMA |AdvanceTo|0,5606|0,5711|-1,87 %|
| Lecture 4MHz DMA |AdvanceTo|0,5714|0,5796|1,83 %|
| Lecture 4MHz non-DMA |AdvanceTo|0,6117|0,5717|8,28 %|
| Écriture 2MHz DMA |AdvanceTo|2,4377|1,5487|37,00 %|
| Écriture 2MHz non-DMA |AdvanceTo|2,4536|1,5739|35,86 %|
| Écriture 4MHz DMA |AdvanceTo|1,4447|0,9471|34,44 %|
| Écriture 4MHz non-DMA |AdvanceTo|1,4651|0,9699|33,02 %|

Sur AdvanceTo en lecture, les réductions appariées sont−3,55/−1,87/1,83/8,28 % selon les quatre charges, avec dispersion comprenant des écarts défavorables : aucun gain général de lecture attribué à ce changement. En écriture, AdvanceTo gagne33,02–37,00 % et Tick22,53–25,64 %. Le parcours courant AdvanceTo reste0,22–7,30 % plus lent que Tick en écriture selon cette invocation, alors que la lecture groupée est40,51–65,94 % plus rapide que ses Tick : conserver cette distinction au raccordement et ne pas généraliser le gain d'un parcours. Ces résultats sont ceux du contrôleur avec son transport de banc, pas des performances Atom/System5.

Au repos ou en recherche sans impulsion ni DMA pendant, AdvanceTo représente1M périodes en un appel inférieur à0,001ms sur ces fenêtres ; ne pas convertir cette mesure proche de la résolution en une fréquence physique ou un pourcentage de gain significatif. Les Tick font toujours leurs opérations et observations du banc à chaque période. Les résultats bruts, périodes, allocations et dispersions sont conservés dans les douze JSON `20261111-review-<0..2>-<reference|current>-<tick|grouped>.json`, `20261111-review-summary.json` et les empreintes DLL `20261111-review-assemblies.json`. Le profil est séparé des chronométrages ; aucun nouveau débit Release revendiqué.

La capture historique `artifacts/TestsChips/Intel8271/BbcMicroModelB/guest-disk-commands.png` a été réinspectée : six opérations PASS et le verdict global lisibles. Elle est réutilisée sans réexécuter ce programme graphique et sans la présenter comme une nouvelle preuve de silicium. Le banc temporaire et ses produits sont supprimés après terminaison des processus ; rapports et image restent hors Git.

**Revue8271 vérifiée dans le périmètre accepté**, avec les limites nominales explicitement conservées sur décision utilisateur. Cela ne certifie pas les latences non publiées ni le séparateur analogique. Le prochain bloc est le câblage Atom et ses validations : utilisation effective des parcours qualifiés, phases VIA, horloge vidéo et interactions disque ; aucun gain de machine complète acquis par cette mesure isolée.

## Résultats finaux des tests automatisés

| Passage | Réussites | Échecs | Durée |
|---|---:|---:|---:|
| Chips.Tests complet, Debug | 1508 | 0 | 725 ms |
| Chips.Tests complet, Release | 1508 | 0 | 667 ms |
| Atom/System/BBC et intégrations 8271, Debug | 143 | 0 | 6 s |
| Atom/System/BBC et intégrations 8271, Release | 143 | 0 | 5 s |
| Encodeurs ISO et fenêtres de flux, Release | 40 | 0 | 542 ms |

Le groupe propre au 8271 contient 140 cas de broches ; il ne faut pas confondre les 1508 tests de toute la bibliothèque avec une couverture de 1508 fonctions du 8271. Les 143 intégrations comprennent 76 contrôles déplacés d’images/pistes et les contrôles natifs Atom/System/BBC existants. Les avertissements préexistants xUnit2031, CS8604 et CS0649 ne sont pas des erreurs de puce.

Les essais temporaires sur de vrais fichiers SSD ont aussi vérifié la chaîne carte BBC → broches → SDK → runtime MediaEngine : écriture de 256 octets puis sauvegarde/rechargement ; privation après dix octets CPU, résultat 0A et préfixe réellement émis conservé ; refus de publication SSD du champ endommagé ; réparation complète et sauvegarde redevenue possible ; effacement de piste et restauration, invalidation du cache après écriture sectorielle, média en lecture seule et bornes de piste. Leurs fichiers, outils et sorties temporaires sont retirés après consignation.

## Preuve invitée BBC Model B

Capture locale hors Git : `artifacts/TestsChips/Intel8271/BbcMicroModelB/guest-disk-commands.png`, 640 × 256. Elle a été inspectée : six lignes PASS et un verdict global sont lisibles. Les pixels proviennent de la vidéo de la machine ; aucun texte de verdict n’est dessiné par l’hôte.

La carte BBC Model B 32 Kio utilise les fichiers existants MOS 1.20, BASIC II et SAA5050. Après initialisation MOS, le programme 6502 est chargé à 1900 et le PC positionné à cette adresse comme lors d’un chargement par débogueur. Il commande lui-même FE80/FE81/FE84, acquitte les transferts et affiche par OSWRCH. Les NMI passent par le vecteur réel 0D00. Le média de test en mémoire traverse MediaEngine et le câblage Acorn.

Le programme compare les 128 octets initiaux, écrit puis relit et compare un motif XOR A5, compare les quatre octets Read ID, déplace la tête à 1 et vérifie son registre avant retour à 0, exécute Verify, puis formate et compare les 128 octets E5 du nouveau secteur 9. Son verdict global en RAM ne devient PASS qu’après ces comparaisons. L’hôte contrôle également au moins 388 octets réellement lus et 132 octets CPU écrits. Diagnostic réussi, zéro échec, 6 s.

Cette capture prouve ces six opérations sur BBC Model B ; elle ne prétend pas prouver les modes PLO, les dommages interrompus ou toutes les autres cartes. Ces fonctions disposent de leurs contrôles de broches/intégration distincts. Le diagnostic temporaire est supprimé et son assembly reconstruit ; les médias/cartes sont détachés et réinitialisés dans finally, les objets graphiques libérés dans using/finally.

## Revue Atom — 11 novembre 2026

Production inchangée : **165 cas 8271 hors états passent en Debug et 165 en Release**, zéro échec/ignoré. Rapports `artifacts/TestsChips/AcornAtomReview/20261111-chips-{debug,release}.trx`. Ils couvrent les fonctions et comparaisons de chronologie décrites dans cette fiche ; aucune latence interne non publiée n'est certifiée par un autre émulateur.

Banc actuel isolé, propriétaire 1 MHz, contrôleur mini 2 MHz, au repos : 1 million de Tick de chauffe, cinq fenêtres de 1 million, médiane et boucle comprises. **Debug 22,3860 ns/Tick propriétaire ; Release 4,3742 ns/Tick.** Tick courant met aussi à jour le temps et la publication d'échéance : ce chemin n'est pas celui du banc historique de la première réécriture. Aucun gain/régression de production n'est déduit de leur différence. Pas de flux média, mécanique, CPU ou rendu inclus. Les vitesses actives et AdvanceTo ne sont pas mesurées par ce cas au repos. JSON `artifacts/TestsChips/AcornAtomReview/20261111-speed-{Debug,Release}.json`.

Le regroupement lecture et les échéances locales sont réellement présents ; l'écriture reste scalaire après le retrait mesuré de l'essai du 10 novembre. La latence CF/PF nominale et le PLO analogique externe restent les limites décrites plus haut. Le raccordement Atom doit encore être examiné après traitement successif des puces. [Bilan de revue](../../tasks/machines/AcornAtom/chip-review.md).

## Performances hôte

Même banc initial/courant, sans limitation à la vitesse réelle : médiane de cinq passages, huit millions de Tick au repos par passage après cent mille de chauffe. Le banc de flux exécute deux millions de ticks propriétaire par passage, avec impulsions FM, index, bus, acquittements DMA et relance des commandes ; 640 octets sont réellement lus au dernier passage.

| Configuration | Initial au repos | Courant au repos | Flux courant, broches et acquittements inclus |
|---|---:|---:|---:|
| Debug | 6,671 ns/tick | 4,128 ns/tick | 48,055 ns/tick propriétaire |
| Release | 2,582 ns/tick | 0,683 ns/tick | 20,303 ns/tick propriétaire |

Le chemin au repos retourne après les buffers sans passer par un appel de prédicat série ; le chemin actif lit son état directement. Aucun cycle actif, signal, donnée, CRC ou option de compilation Debug n’est supprimé. Le code initial et le code courant ne couvrent pas les mêmes fonctionnalités ; seule la charge au repos est comparée directement. Le banc de flux n’est pas une mesure du framerate de l’application ou du coût complet de la machine.

## Livraison et nettoyage

### Régression du 5 novembre : recherche après une jonction d’écriture

La validation complète par les ROM DFS 0.9H a rouvert un défaut absent du premier banc CPU : après SAVE du catalogue, les Model A/B renvoyaient 18h en recherchant le secteur 2. Les dix champs CHRN de la piste restaient valides pour le décodeur média ; aucune exception du relais n’était levée. Le banc synthétique de piste uniforme passait et ne suffisait donc pas à reproduire ce cas.

Le scénario autonome ajouté envoie un champ de données ignoré rempli de zéro pendant une recherche d’ID, décale la phase, puis fournit l’ID et ses données. Avant correction, quatre des huit cas échouaient : retards 9/12 ticks à 2 MHz et 5/7 ticks à 4 MHz. Le séparateur, déjà calé sur le champ ignoré, ne se recalait pas sur la nouvelle synchronisation.

En recherche seulement, le modèle réacquiert désormais le calage au franchissement du seuil existant de seize clocks sans données. Ce recalage intervient une seule fois par plage continue ; il ne redémarre pas le compteur à chaque impulsion, ne touche pas aux transferts déjà synchronisés et ne change ni la cadence des cellules ni les délais matériels. La correction reste à coût constant par front et sans allocation.

Les **148 contrôles 8271 passent en Release (293 ms) et Debug (390 ms)**, ainsi que les **80 contrôles d’intégration 8271 en Release (3 s)**. Ces durées sont des temps de suites, pas un nouveau benchmark du débit de la puce. Les nouveaux tests autonomes conservent les huit décalages et la piste vierge après écriture du catalogue.

Sur les vrais MOS/DFS, A 16 Kio et B 32 Kio réussissent maintenant SAVE puis LOAD de quatre motifs distincts de 512 octets : unités DFS 0/1/2/3, soit deux lecteurs et deux faces. Après détachement, Flush, fermeture et réouverture des deux DSD, les quatre LOAD restituent encore tous les octets exacts. Passage A/B réussi en 52,956 secondes hôte. Captures inspectées sous `artifacts/TestsChips/Intel8271/<Machine>/two-drives-two-sides-save-load.png` ; elles montrent le retour des commandes et le verdict BASIC après les comparaisons, sans exécuter le CPU à une adresse imposée par le banc.

Après ce passage, le diagnostic externe est retiré et l’assembly Release reconstruit sans lui. Le build Debug `--modules=A` réussit ; les 24 modules et l’exécutable sont présents, et la DLL Chips dans `lib` correspond par SHA-256 à la compilation testée. Les captures et médias temporaires ne font pas partie du commit.

`scripts/local-building.cmd --building=debug --modules=A` a terminé avec succès. Exécutable livré : `build/Debug/Retro Media Lab/retromedialab.exe`. La DLL `lib/retromedialab.chips.dll` et chacune des 24 assemblies de modules ont été comparées par SHA-256 aux sorties compilées : elles correspondent.

Le dossier temporaire `artifacts/.tmp-i8271/`, ses références, bancs, journaux et sorties compilées sont supprimés. Les tests autonomes restent dans leurs projets ; la capture locale demeure hors Git. Le commit de clôture exclut artifacts et les images de test. Les limites non publiées ci-dessus restent explicites ; le document ne revendique aucune caractérisation analogique ou latence interne mesurée qui n’existe pas.

## Optimisation des observations de raccordement — 5 novembre 2026

DriveSignalsChanged est une notification interne à l'émulation des changements des états déjà exposés (commande lecteur, sélection, lecture/écriture), pas une nouvelle fonction matérielle. Les mutations sont centralisées sans changer les valeurs ni l'ordre des commandes, fronts, échéances ou Tick. Le raccordement conserve chaque cycle du contrôleur, chaque avancement fractionnaire et la rotation, mais ne relit plus quatre états deux fois par cycle s'ils sont inchangés. Les remplacements de média, resets et fins de fenêtre d'écriture conservent leurs chemins explicites.

149 tests autonomes de composant et 135 tests de transferts/raccordement/configuration passent en Debug et Release. Le nouveau contrôle parcourt les 256 valeurs de commande et de sortie manuelle et vérifie les notifications après chaque opération observée ; les tests de flux, DMA, interruptions d'écriture et mécanique préexistants restent exécutés. Sur 500 tranches au repos BBC, le raccordement disquette mesure 1,3277 ms contre 2,5410 ms dans la référence précédente, sans allocation ; le cœur complet reste à 33,16 FPS / 1,32 MHz. Ce résultat sans interface ne prouve ni 50 FPS dans l'application ni l'achèvement des autres puces.

## Sortie NMI sur mutation du statut — 6 novembre 2026

Les écritures du statut passent par SetStatus : le rappel NmiChanged ne s'exécute que si le bit d'interruption change. Demande de données, résultat, acquittement et reset passent par ce chemin. Aucun sondage supplémentaire n'est ajouté à Tick. Le câblage BBC reçoit le niveau et le conserve dans sa contribution NMI. Correction écrite ; tests, build et comparaison temporelle restent à effectuer.

## Sélection au front WR et horloge propre — 6 novembre 2026

La fiche Intel AFN-00223B, page 8-141 (table WRITE CYCLE), donne tCA, maintien de sélection après WR, à 0 ns minimum. Le chronogramme WRITE page 8-142 sépare la sélection, WR et les données. Le modèle rejetait à tort le relâchement simultané CS/WR en utilisant la sélection après le front. SetBus retient désormais CS, DACK, adresse et données du cycle d'écriture engagé. Il utilise cette sélection retenue au relâchement de WR. Une perte de sélection dans un appel antérieur alors que WR reste actif annule toujours le cycle ; elle n'est pas réinterprétée comme une écriture ultérieure. Le maintien DACK constructeur reste de 25 ns : cette correction de CS n'établit pas une garantie sur des cycles DMA qui violent ce maintien.

Le fichier Intel8271.Clock.cs appartient au contrôleur. Il contient son compteur temporel, ses fréquences d'entrée/contrôleur et sa prochaine échéance. AdvanceTo conserve les ticks série actifs et la phase fractionnaire des buffers. Au repos ou sous reset, il avance directement le compteur sans exécuter TickCore. Pendant un délai de positionnement sans buffer ni activité série, il soustrait les périodes sans effet et exécute le traitement au même cycle d'expiration. ExternalSeek sans délai attend l'entrée Count.

Le raccordement Intel8271Drive conserve seulement sa référence temporelle et l'échéance reçue. Il n'appelle plus Tick du contrôleur au repos. Les accès bus BBC, Atom et System synchronisent d'abord le contrôleur ; index et impulsions le synchronisent avant de modifier leurs entrées, puis le tick courant garde son ordre d'origine. Les niveaux index et la fin d'impulsion ne sont envoyés que lorsqu'ils changent. Les échéances de série restent conservatrices, à une période d'entrée : aucune suppression de ces cycles n'est revendiquée. Aucun ordonnanceur global ni thread supplémentaire n'est introduit.

BusTests couvre désormais le relâchement simultané CS/WR, l'adresse retenue et l'absence de seconde écriture. ClockTests ajoute les comparaisons d'avancement groupé/unitaire et de transitions de positionnement. Ces nouveaux contrôles sont écrits mais non exécutés pendant ce bloc ; aucune performance, absence de régression ou livraison compilée n'est encore annoncée pour ces modifications.

## Validation groupée des raccordements — 6 novembre 2026

Les cas de ce composant et les autres cas du bloc horloges/signaux réussissent dans le passage Chips Debug complet. Le passage ciblé des sept composants en Release donne 723 réussites, zéro échec ni ignoré, 710 ms. Les contrôles d'intégration sélectionnés BBC Model B, tape/série, 8271 et WD1770 donnent 166 réussites en Release, 10 s. En Debug, les 160 cas extérieurs au câblage série ont réussi au premier passage ; les six cas de câblage série ont réussi après adaptation de leur relais et de leurs assertions de câblage, 848 ms. Ces comptes sont ceux des groupes, pas un nombre de fonctions propres à cette puce.

Le passage Chips Debug global reste non vert : deux contrôles des contrats publics, notamment sur des classes ACB-4000 déjà présentes et non modifiées, ainsi qu'un contrôle d'allocation SCSI échoué une fois puis réussi isolément. Les exigences correspondantes ne sont pas supprimées. Le détail est consigné dans docs/tasks/chips-validation.md. Ces résultats ne remplacent ni la mesure du BBC complet ni les captures de validation des machines ; aucun nouveau framerate n'est revendiqué.

## Essai retiré : temps du relais moteur arrêté — 6 novembre 2026

Essai dans Intel8271Drive, sans modification du contrôleur : accumuler les périodes sans moteur, impulsion, écriture, changement de sortie ou échéance, puis consolider exactement nanosecondes et résidu avant toute observation. Les 89 contrôles de raccordement/transfert réussissent en Debug (4 s) et Release (3 s). Les comparaisons temporelles couvraient aussi des fréquences non divisibles et des réveils média/reset.

Comparaison du BBC complet : 80 trames de chauffe puis 200 mesurées, trois processus par variante dans chaque compilation/scénario, ordre référence/candidat/candidat/référence/référence/candidat. RunFrame, drainage audio et empreinte PCM inclus. Configuration copiée en mémoire ; aucune fenêtre, sortie audio Windows ni transport UDP. Les DLL réellement chargées sont contrôlées pour conserver Debug sans optimisation JIT et Release optimisé.

| Scénario | Debug avant → après, ms/image | Release avant → après, ms/image |
| --- | --- | --- |
| Prompt sans média | 21,584752 → 20,066061 | 3,469745 → 3,412416 |
| *DISC / *CAT, DSD en lecture seule | 23,864427 → 23,449641 | 4,770247 → 4,806078 |

Médianes de trois passages. Dans chaque scénario, les six passages de chaque compilation ont exactement les mêmes cycles, PC, activité lecteur, octets lus et empreintes vidéo/PCM. Catalogue : 7 987 199 cycles, PC DEF2, moteur actif sur 130 trames, 512 octets lus. Vidéo SHA256 `12E388C611BDFFCD77A6961533431F748C8ECF9CD2E0F549E621F67B784C69F7`, PCM `3C7700D5B155CCCB980C220182E82858A332451FB727344329DB2B62C4DBB68E`. Au repos : 7 987 198 cycles, PC E464, aucune activité ; empreintes identiques à celles du bloc SERPROC consignées dans Mos6502.md.

Le gain au repos ne suffit pas à retenir cet essai : la médiane Release du catalogue augmente de 0,75 %, avec des plages de mesures qui se chevauchent. Cela ne démontre pas une régression certaine, mais ne confirme pas le bénéfice sur le scénario actif. L'essai et ses seules adaptations de tests sont retirés. Les raccordements et notifications antérieurs sont conservés. Acorn est reconstruit après restauration en Debug et Release, zéro avertissement/erreur. Aucun nouveau build livré ni résultat natif n'est revendiqué. Le budget de 19,97 ms reste ouvert.

## Piste actualisée sur changement des raccordements — 6 novembre 2026

Le relais Intel8271Drive ne recherche plus image/piste/face à chacun des ticks moteur. Une notification DriveSignalsChanged invalide la sélection retenue ; les chemins de changement de média, reset, pas mécanique et publication d'écriture invalident également la piste. Le chargement reste effectué dans le même créneau du prochain Tick moteur avant index et lecture des impulsions. Une piste stable reste directement en mémoire. Les cycles du contrôleur, rotations, fronts d'index, fenêtres d'écriture et monostable ne sont pas modifiés.

Les propriétaires actuels sont vérifiés : BBC décode sa face depuis la sortie optionnelle du 8271, Atom et System utilisent la face zéro fixe. Aucun de ces raccordements ne dépend d'un sélecteur extérieur changeant sans notification. Ce constat concerne ces trois propriétaires existants, pas une hypothétique autre carte. Le contrôle de rotation stable vérifie l'absence de consultations répétées du fournisseur d'image et du décodeur de face, puis leur réveil après changement de face, média et reset. Les 87 cas Intel8271 passent en Debug (4 s) et Release (3 s), dont les transferts, écritures, formatages et deux lecteurs/faces déjà couverts par ce groupe.

Mesures du BBC complet : configuration utilisateur copiée en mémoire, mêmes ROM et options, sans interface, transport UDP ni sortie audio Windows. 80 trames de chauffe puis 200 mesurées, RunFrame/DrainAudio et empreinte PCM inclus. Trois processus par variante, ordre référence/candidat/candidat/référence/référence/candidat. Debug conserve son JIT non optimisé ; ce mode est contrôlé sur les DLL réellement chargées.

| Scénario | Debug médian avant → après, ms/image | Release médian avant → après, ms/image |
| --- | --- | --- |
| Prompt sans média | 21,235326 → 21,394956 | 3,443555 → 3,440623 |
| *DISC puis *CAT, DSD en lecture seule | 23,513850 → 23,123186 | 4,702718 → 4,128318 |

En lecture : coût médian réduit de 1,66 % en Debug et 12,21 % en Release. Au repos : aucun gain établi ; les plages Debug se chevauchent (référence 21,046909–21,342828, candidat 21,023735–21,437429), et ce chemin n'exécute pas la recherche de piste ciblée. Cette variation est conservée dans le résultat, pas remplacée par une valeur choisie.

Dans chaque scénario, les douze passages Debug/Release conservent les mêmes cycles, PC, activité et empreintes. Catalogue : 7 987 199 cycles, PC DEF2, moteur actif sur 130 trames, 512 octets lus ; vidéo SHA256 `12E388C611BDFFCD77A6961533431F748C8ECF9CD2E0F549E621F67B784C69F7`, PCM `3C7700D5B155CCCB980C220182E82858A332451FB727344329DB2B62C4DBB68E`. Prompt : 7 987 198 cycles, PC E464, aucune activité, empreintes déjà consignées dans Mos6502.md.

La première saisie du banc utilisait une touche A pour toutes les lettres : la capture montrait des commandes incomplètes. Ses mesures catalogue sont invalidées. Une copie trop large des DLL restaurait également l'ancien exécutable du banc ; cette invocation est corrigée pour copier seulement les six dépendances nommées. La série catalogue retenue utilise les touches physiques des lettres et vérifie obligatoirement l'empreinte de l'écran attendu, en plus des octets lus. Les résultats au repos ne dépendent pas de cette saisie et sont conservés.

Preuve Debug du catalogue : `artifacts/TestsChips/Intel8271/BbcMicroModelB/catalogue-track-signals.png`, 640 × 512, issue de la trame du cœur sans retouche. Elle montre *DISC, *CAT, Drive 0, Directory :0.$ et le retour au prompt pour `F:\Retro\Vierge_BBC_Model_A.dsd`, catalogue vide. Elle prouve ce scénario, pas un test visuel d'écriture/formatage, ni une cadence native. Le média est ouvert en lecture seule, détaché en finally puis libéré ; la configuration utilisateur n'est jamais écrite. Le tampon brut est supprimé après conversion.

Le bloc est conservé pour son gain en lecture dans les deux configurations et l'identité des sorties contrôlées. Le budget Debug de 19,97 ms reste non atteint ; aucune puce ni machine entière n'est déclarée finalisée sur cette seule optimisation.

### Build Debug livré

`scripts/local-building.cmd --building=debug --modules=A` terminé avec code 0. Exécutable présent : `build/Debug/Retro Media Lab/retromedialab.exe`. Les 24 manifestes sont livrés et chaque entryAssembly existe : acorn, amstrad, apple, atari, bandai, coleco, commodore, epoch, example, fairchild, mattel, microsoft, nec, nintendo, panasonic, philips, sega, sharp, sinclair, snk, sony, thomson, watara, wenquxing. La DLL Acorn livrée est identique à sa sortie win-x64 Debug. Chips est présent dans lib et identique à sa sortie Debug : SHA256 `B30B88C613990BE4D39ABE0BF14FFF4D54599F7CD4B8DEFDE4E0686BE1FAFC93`. Les avertissements CA1416 observés concernent d'autres modules et ne sont pas masqués. Cette livraison n'est pas une nouvelle mesure des FPS de la fenêtre native. Aucun commit effectué dans ce bloc.

## Capture et réinjection à l'instant T

`Intel8271State` copie les 90 champs scalaires, les 15 tableaux et la valeur présentée sur le bus : registres, paramètres et commandes tamponnés, séquenceurs FM, CRC partiels, séparateur, données en vol, délais mécaniques, broches et échéances déjà publiées. Les deux horloges configurées sont contrôlées avant restauration. Aucun accès lecteur, acquittement, front ou événement n'est généré par la réinjection ; les récepteurs restent raccordés.

Trois tests Debug réussis (573 ms) : douze reprises au milieu de lectures/écritures FM, puis transfert complet de 128 octets et comparaison des impulsions, DRQ/NMI, échéances et états finaux. Une commande/paramètre en attente sur le bus reprend également à l'identique ; horloge incompatible et tableau mal dimensionné sont refusés avant mutation. Les images et la mécanique du lecteur appartiennent au module et restent distinctes de cet état de puce.

## Logiciel invité Model B, 7 novembre 2026

Le BBC B 32 Kio charge réellement Boulder Dash par MOS 1.20/BASIC II/DFS 0.9H et le 8271 : 28672 octets lus depuis le SSD TOSEC ouvert via le SDK en lecture seule, aucun changement de puce. Après entrée au clavier, les captures inspectées montrent le niveau et le compteur passant de 149 à 146 : `artifacts/TestsChips/Intel8271/BBC Micro Model B/20261107-092836-model-b-boulder-start.bmp` et `20261107-092841-model-b-boulder-after-240-frames.bmp`. Carte complète Debug sur 240 images après chargement : 19,540560 ms/image, 51,1756 images/s, 2,043749 MHz, 102,19 %, zéro octet disque supplémentaire pendant cette fenêtre. Ces chiffres ne chronomètrent donc pas le canal de lecture actif. Source inchangée, image détachée puis libérée dans finally ; diagnostic temporaire supprimé après consignation. [Conditions et limites](../../project/emulations/Acorn/BBC%20Micro/Model%20B/board-validation.md).

## 8 novembre 2026 — cartes Acorn System 3/4/5

Réutilisation du 8271 existant, sans modification de ses cadences. Le défaut du bootstrap 6809 (sept octets lus) venait de CWAI compté deux fois dans le CPU ; le même contrôleur lit ensuite 512 octets et le programme chargé s'exécute. Sur 6502/System 3/4 et 6502A/System 5 : catalogue, BASIC chargé intact et exécuté, SAVE PROBE (768 octets écrits), effacement RAM puis LOAD correct. CAT1 vérifie le second port sur System 4/5. Le défaut System 5 était le plan DRAM, corrigé dans la fabrique ; ne pas le compenser dans le FDC. Captures sous artifacts/TestsChips/Intel8271/Acorn System 5/ et TestsMachines/Acorn/Acorn System 3, 4, 5. Mesures de cartes complètes, non de puce isolée : System 3/6502 premier repos 15,65 ms/champ, System 4 15,52 ms/champ, System 5 13,39 ms/champ, tous Debug. Régression commune 46 tests System, 61 tests MC6809.

## 8 novembre 2026 — revue statique des échéances

Défaut constaté : `Tick()` et `AdvanceTo()` ne publiaient pas leur échéance finale. Le raccordement compensait cette omission en relisant `NextClockEvent` après chaque synchronisation. Les deux entrées d'avancement publient maintenant l'échéance finale uniquement si elle diffère de la précédente ; le passage au repos publie également l'absence d'échéance. Un avancement au même instant retourne directement. Le lecteur initialise son échéance une fois puis conserve les notifications reçues ; la relecture récurrente est supprimée.

`AcknowledgeData` ne modifie ni phase ni délai : aucune publication supplémentaire n'y est ajoutée. Les entrées Ready, protection, défaut, index, Count, reset et écritures publiaient déjà leurs mutations. Les cycles série et délais de positionnement sont conservés. Revue du code uniquement conformément à la demande : aucun test, build ou gain chronométré nouveau ; la validation fonctionnelle de cette correction reste à effectuer.


## Revue du raccordement System 3/6502 — 9 novembre 2026

Le 8271 conserve dans `Intel8271.Clock.cs` son compteur, ses fréquences et ses échéances. System 3 fournit une référence propriétaire de 1 MHz et configure le contrôleur mini 2 MHz par le constructeur existant ; les conversions de délais et les phases série restent dans la puce. `Intel8271Drive` possède seulement la conversion du temps du lecteur, sa rotation et les transitions des broches physiques. Il retient les échéances notifiées, synchronise le 8271 avant les transactions et changements de broches, et ne recherche plus le support ou la face à chaque période moteur stable. Le bus System synchronise aussi tous les accès et miroirs de données avant lecture/écriture. NMI est initialisée puis raccordée directement à l'entrée mémorisée du CPU. Aucun ordonnanceur commun, nouvelle boucle de sondage ni modification de production ajoutés pendant cette revue.

Contrôles existants actuels : **155 tests de puce Debug réussis, zéro échec/ignoré, 611 ms**, sauvegardes d'état exclues ; **87 tests de raccordement Intel8271 Debug réussis, zéro échec/ignoré, environ 4 s**. Ils couvrent les registres/bus, signaux, lecture/écriture série, échéances groupées/unitaires, transferts de champs, changement de support et fenêtres physiques, dont écritures interrompues. Ces durées sont celles des tests fonctionnels et ne constituent pas un relevé de débit. L'essai du logiciel invité et la cadence du System 3 actuel sont consignés séparément après leur exécution.


Confirmation invitée actuelle System 3/6502 : CAT et LOAD BASIC par la vraie image SSD en lecture seule, 4608 octets lus avant lancement ; empreinte des 4096 octets BASIC à C000 exacte (`6571FCA3E86E6DC99C2B56481979FBC24615BFF2`), puis PRINT 12+34 produit 46. Débit carte complète au prompt BASIC après chargement : 10,566098 / 10,932135 / 10,904064 ms par champ, soit 182,947–189,285 % du profil 1 MHz en Debug. Trois fenêtres de 160 champs, rendu et drainage audio inclus. Cette valeur concerne la carte complète au prompt, pas un débit isolé du contrôleur ni une lecture continue. Les ajustements du diagnostic, le montage, le tableau et son nettoyage sont détaillés dans la fiche System 3 ; aucun changement de production du 8271 nécessaire.


## Bootstrap actuel System 3/6809 — 9 novembre 2026

Deux secteurs FM de 256 octets, descripteur FF42 du moniteur et programme 6809, transmis depuis MemoryTrackDisk par l'encodeur MediaEngine et les broches normales du contrôleur. La commande invitée D lit exactement 512 octets ; les 256 octets du programme chargé sont contrôlés, puis son exécution écrit 2010=5A et BOOT OK en RAM vidéo. Passage final réussi, aucune modification de production du 8271. Capture actuelle inspectée et portée du média documentées dans les fiches MC6809/System 3 ; ne pas confondre ce diagnostic en mémoire avec une distribution de système d'exploitation. Le diagnostic temporaire est supprimé après consignation.


## Deux ports System 4 actuels — 9 novembre 2026

Contrôle par la fabrique normale SystemFunctions.Create : CAT et LOAD BASIC sur le port 0 (4608 octets lus, empreinte des 4096 octets chargés exacte), exécution BASIC et calcul 46, puis déplacement du média en lecture seule vers le port 1 sans reset et *CAT1 (512 octets, drive 1 affiché). Aucun changement de production du contrôleur. Capture actuelle inspectée : `artifacts/TestsChips/Intel8271/Acorn System 4/20261109-current.bmp`, copie de la même exécution conservée sous TestsMachines. Cadence carte complète Debug après lecture : 10,562988 / 10,471156 / 11,107172 ms par champ, soit 180,064–191,001 % du profil 1 MHz. Ces valeurs ne sont ni le débit isolé du 8271 ni une mesure de lecture continue. Montage, tableau et nettoyage détaillés dans la fiche System 4.

## System 5 optimisé : logiciel et persistance actuels — 9 novembre 2026

Version de production 29dedacdd : CAT puis BASIC depuis le SSD de référence en lecture seule, 4608 octets lus, transfert C000 exact et PRINT 12+34=46. Sur une copie nouvellement créée : SAVE PROBE écrit 768 octets ; après Flush, fermeture puis ouverture dans une machine neuve, LOAD PROBE relit physiquement 768 octets et restitue 5A00FF0102807FA5. CAT affiche PROBE. Image source inchangée ; copie et compagnons possédés supprimés, zéro résidu. Le banc attend la fin de commande avant détachement ; la politique de retrait en cours de commande reste un audit matériel distinct, pas une validation déduite de ce passage.

Résultats, conditions et corrections du banc dans la [validation System 5](../../tasks/machines/AcornSystem/system5-validation.md), passage « Validation logicielle et persistance sur le montage optimisé ». Captures inspectées et preuves de commandes sous TestsChips/Intel8271/Acorn System 5/20261109-current-software-results.md et deux BMP datés 20261109-133020-0699-current-software-*. Banc supprimé, assembly régénérée ; aucune modification de production du 8271 pendant cette vérification.

### Raccordement400,004 dans System5, 9 novembre
Le module rapide reçoit les vrais fronts QA du393 après propagation et appelle AdvanceTo sur la nouvelle position du compteur d'entrée ; cette API existante évite TickCore lorsque le composant est inactif ou sous reset. Aucun cycle série actif supprimé. La carte conserve les niveaux physiques READY/index et la fenêtre des compteurs/NAND. Le montage est vérifié en lecture-écriture-relecture de128 octets et dans NOS80/CAT/BASIC ; résultats et cadence Debug insuffisante dans docs/tasks/machines/AcornSystem/system5-validation.md. Aucun changement des timings internes du8271 dans ce passage.

### Raccordement System5, 9 novembre2026
Aucun changement interne du8271 dans ce passage. La carte400,004 reçoit les échéances du lecteur par notification et évite les répétitions de niveaux d'entrée du bus ; elle conserve chaque transaction et les clocks2 MHz du contrôleur.17 contrôles de carte passent : lecture/écriture/relecture128 octets et comparaison des flux DATA WINDOW/NMI datés, READY, données et comptes du8271 pour les deux lecteurs. Détails et performances de la machine complète : docs/tasks/machines/AcornSystem/system5-validation.md ; ne pas attribuer ces mesures au seul contrôleur.
### Formatage invité sur400,004, 10 novembre2026
Contrôle d’intégration sur le code actuel : FORM80 officiel chargé depuis un SSD d’utilitaires en lecture seule sur System5, puis formatage et vérification des80 pistes du lecteur1, sans injection de commandes internes ou de données CPU/RAM. Formatted,00–4F,Verified et catalogue vide inspectés sur les captures natives. Image fermée :800 secteurs dont798 secteurs de données tousE5, alors qu’ils étaientA5 avant opération ; original utilitaire inchangé.3712 octets hôtes reçus en écriture,237 pas ; vérification interne sans DMA de lecture, donc compteur hôte lu à0. Ce résultat valide ce parcours intégré après les corrections du relais ; aucune modification interne du8271 ni nouvelle mesure isolée de performance. Sources des captures, empreintes et conditions dans [le suivi System5](../../tasks/machines/AcornSystem/system5-validation.md), rubrique FORM80 du10 novembre.

### FAULT/OP0 et surfaces System5 — 10 novembre2026
Le manuel System5 p8/PDF103 précise que la sortie FAULT/OP0, après le7438, sélectionne ici les surfaces. Le relais de carte ignorait cette ligne et imposait tête0 ; le contrôleur8271 publiait déjà correctement OP0, aucune modification interne du8271 nécessaire. Correction dans SystemFunctions.Create, AcornSystemMachine et SystemFloppyDrive : capacité du lecteur depuis le modèle configuré, surface reçue depuis la ligne, lectures/écritures/mécaniques sur cette tête et fermeture de fenêtre avant mutation.159 contrôles System/carte/audio passent,41 carte ; les cas double face échouaient avant correction.
DOS natif par la vraie fabrique : fichiers distincts sur volumes1/3 d’un DSD, puis fermeture et nouvel invité,1536 octets écrits et1536 relus, motifs exacts et catalogues indépendants. Pas d’injection RAM/CPU. Captures et conditions dans [le suivi System5](../../tasks/machines/AcornSystem/system5-validation.md) et la fiche400,004 ; aucune performance du seul8271 ni des autres cartes déduite.

### Lecture série groupée — 10 novembre2026, vérification autonome
NextClockEvent annonce désormais le prochain bit ou timeout DMA pendant la lecture ; avant cette frontière, AdvanceTo calcule directement la fraction série, l'âge de la dernière impulsion et le compteur de délai. La saturation de l'âge conserve son arrondi au cycle d'origine. Le front de bit et l'expiration passent encore par TickCore ; les buffers de bus et l'écriture conservent leur parcours unitaire. Les impulsions Unseparated Data et acquittements publient leur nouvelle échéance. Aucune fréquence ni broche modifiée, aucune gestion temporelle déplacée dans la carte. Le témoin interne GroupSerialReadClocks=false conserve les réveils précédents pour comparaison.
Contrôle autonome Debug :165 cas réussis en1 s, dont10 nouveaux cas. Ils comparent tous les états internes entre Tick et AdvanceTo (hors indication d'échéance publiée), avec rapports d'horloges2/2,2/3,3/2,4/2 MHz, impulsions variables et reset ; puis lecture FM complète DMA/non-DMA à2/4 MHz, échéances et transitions NMI datées, demandes d'octets et expiration Late DMA. Les avances logicielles du banc groupé sont inférieures à la moitié des avances unitaires. Aucun gain machine encore revendiqué. TRX : `artifacts/TestsChips/Intel8271/Acorn System 5/20261110-serial-event-chip.trx`. Les tests d'états sauvegardés restent hors du périmètre demandé.

### Mesure sur System5 — 10 novembre2026
Même DLL Debug, seul GroupSerialReadClocks faux/vrai :80 champs de chauffe puis3paires de80, ordre alterné, CPU/ROM synthétiques et dix secteurs FM128. RunFrame/DrainAudio inclus, vérifications hors chronomètre. Les5 comparaisons fonctionnelles passent : CPU/RAM/vidéo/audio, flux et dates NMI inchangés. Trois moyennes ancien→nouveau :62,91877→56,26639 ;64,56140→57,36313 ;65,40166→56,83182 ms/champ. Total64,29394→56,82045 (-11,624 %) ; lecture113,53530→85,86952 (-24,368 %), écriture avec recherche d'en-tête128,85933→112,31824 (-12,837 %). Réannonces d'échéance par scénario80champs :1 096 850→305 656 (-72,13 %). La sérialisation écrite reste unitaire ; aucun gain intrinsèque de ce chemin n'est déduit du gain de la phase écriture complète. Pas de résultat FPS RML ni de débit du seul8271 revendiqué. Rapports et phases détaillés dans la validation System5 et `20261110-serial-clock-native.{json,md}` ; gain utile retenu, cadence machine encore insuffisante. Banc temporaire supprimé après consignation.
Contrôles finaux sans banc temporaire :200 cas System/carte/relai8271 réussis en7 s, en plus des5 comparaisons de transferts et165 contrôles autonomes. Assemblies locales reconstruites après retrait du banc :0 erreur,4 avertissements préexistants. TRX `20261110-serial-event-integration.trx`. Aucun processus/cœur possédé ni capture WPR en cours. Le bloc d'optimisation est vérifié ; System5 n'est pas déclaré terminé.

### Écriture aux frontières utiles — 10 novembre2026, validation autonome
Même principe que le regroupement lecture : prochaine retombée WriteData, demi-cellule, fermeture de porte ou expiration DMA. Les cycles précédents calculent les fractions, l'âge de l'impulsion et les compteurs ; TickSerialWrite conserve chaque frontière et ses fronts multiples si l'entrée temporelle est plus lente que le contrôleur. Le témoin interne GroupSerialWriteClocks=false garde le parcours du commit3d7726b8f, regroupement lecture inchangé.
173 contrôles8271 hors sauvegardes réussis en979 ms, dont8 cas nouveaux : écriture DMA/non-DMA et sous-alimentation2/4 MHz, formatage deux secteurs avec index intermédiaire et final, états internes/WriteData/Drive/NMI datés aux rapports2/3,3/2 et4/2 MHz. Chaque impulsion, porte, demande et mot FM correspond au Tick de référence ; la différence d'échéance logicielle publiée seule est exclue des comparaisons d'états. Pas de gain machine revendiqué avant mesure. TRX `artifacts/TestsChips/Intel8271/Acorn System 5/20261110-write-event-chip-final.trx`.

### Essai écriture aux échéances non retenu — 10 novembre2026
Extension écriture du regroupement8271 testée :173 cas autonomes passent (dont8 nouveaux : formatage et fronts datés à rapports de clocks différents) et2comparaisons CPU un/dix secteurs passent en17 s. Même RAM/CPU/vidéo/audio, données et dates NMI. Les retombées WriteData, demi-cellules et fermeture de porte ont été conservées dans cet essai ; aucune modification de fréquence ou de MediaEngine.
Mesure Debug isolée sur la même DLL : lecture groupée active dans les deux modes, seul GroupSerialWriteClocks faux/vrai.80 champs de chauffe puis3paires neuves de80, ordre alterné par champ, RunFrame/DrainAudio inclus. Moyennes ancien→nouveau par paire en ms/champ :55,18552→54,25101 ;55,51799→54,06299 ;56,23364→54,26136. Total55,64572→54,19178 (-2,613 %). Phase écriture, recherche d'en-tête incluse :111,31394→102,30537 (-8,093 %) ; lecture86,27532→85,05311 (-1,417 %, pas de changement fonctionnel dans ce trajet) ; contrôleur inactif avec attente READY37,17069→37,02714 (-0,386 %). Réannonces d'échéance par scénario305 656→167 436 (-45,22 %).
Décision : gain global trop faible pour conserver cette complexité. Intel8271.Clock.cs, ClockTests.cs, SerialWriteTests.cs et AcornEmuSystemDiskTransferTests.cs rétablis exactement depuis3d7726b8f ; banc temporaire et ses huit tests spécifiques retirés. Le regroupement lecture déjà committé reste conservé. Aucun nouveau conseil d'optimisation ajouté aux directives. Rapports persistants `artifacts/TestsMachines/Acorn/Acorn System 5/20261110-write-clock-native.{json,md}`, TRX `20261110-write-clock-native.trx` et `20261110-write-event-transfers.trx` ; preuve puce dans TestsChips/Intel8271/Acorn System5/20261110-write-event-chip-final.trx. Ces mesures ne sont pas des FPS RML ni un débit NOS80. Reconstruction sans essai à terminer ; System5 reste ouvert.
Retrait terminé et vérifié : aucune différence src/tests avec3d7726b8f ; Chips et Acorn reconstruits sans erreur/avertissement, tests locaux reconstruits sans erreur avec4 avertissements préexistants.200 contrôles System/carte passent de nouveau en7 s sur ces assemblies restaurées (`20261110-write-trial-restored.trx`). Aucun banc temporaire ni processus de test conservé. Aucune modification de production retenue, donc aucun petit commit des seuls documents ; ces résultats restent avec le suivi pour le prochain bloc utile.

### Réutilisation sans changement du8271 — 10 novembre2026
La carte400004 reçoit la qualification d'inactivité depuis les notifications existantes DriveSignalsChanged et conserve les demandes d'échéance ; le composant reste intégralement inchangé depuis3d7726b8f. La coordination du CPU peut traverser les événements lecteur pendant l'inactivité prouvée, sans retirer leur exécution. Refus vérifié pour formatage actif en attente d'index sans échéance interne et pour NMI en propagation ; transferts et notifications datées comparés. NOS80/BASIC entier :70,15062→56,14132 ms/champ moteur actif (-19,970 %), bilan360champs43,68878→39,29389 (-10,060 %). Ces gains concernent le raccordement, pas la vitesse isolée du8271. Protocole complet et preuves dans la validation System5 ; pas de gain annoncé moteur arrêté ni de modification des autres machines.

## Bilan System5 Debug/Release —10 novembre2026
Production identique àc1a362111 ; aucune modification de ce composant par cet audit. Tests autonomes actuels et scénario isolé, distincts de la vitesse assemblée. Les comptes portent sur la famille/variante des tests, pas sur autant de logiciels invités.
| Scénario | Tests Debug réussis/échoués | Tests Release réussis/échoués | Debug ms/1M unités | Release ms/1M unités |
|---|---:|---:|---:|---:|
|cycle : Tick au repos, aucune synthèse mécanique|168/0|168/0|23,50|3,91|
|cycle : recherche série sans index, NRDATA500 ns/4 us et DataWindow|mêmes168/0|mêmes168/0|91,56|18,19|
Médiane de cinq fenêtres réellement chronométrées de1M unités après10k+3M unités de chauffe non chronométrées, observateurs et boucle inclus ; aucun coût CPU propre déduit. Pour le8271, la ligne active utilise des entrées série et reste BUSY, sans index : ne pas l’assimiler à une lecture native complète. Les profils et délais physiques restent ceux déjà documentés, sans fréquence modifiée.
Le [bilan détaillé](../../tasks/machines/AcornSystem/system5-performance-audit.md) contient min/max, horloges réelles, unités exactes, limites de fidélité, captures TRX/JSON, avant/après historiques comparables et résultats de la machine entière. Bancs temporaires supprimés et assemblies reconstruites sans diagnostic ; aucune extrapolation de gain isolé à la machine.

Profil de la reprise Atom : temps propres/inclusifs échantillonnés séparés, sans addition des parents/enfants. AdvanceTo736,16/3331,44ms, SerialReadClocksToEvent770,73/860,85ms, NextClockEvent659,76/1120,30ms et PublishClockEvent480,59/1608,90ms. La feuille CPU_TIME est attribuée au dernier cadre réel, pas à une fonction fictive. Le parcours AdvanceTo d'une seule période refait les admissions/branches de la boucle avant TickCore ; l'écriture y redemande une admission série qui retourne systématiquement1. Suppression de calculs C# redondants à vérifier par le Tick canonique et retour direct de l'échéance d'écriture ; aucun comportement matériel à retirer. Trace/profil sous20261111-review-reference.*, résuméJSON ; aucune mesure de débit déduite du profilage.

### Avance locale du raccordement lecteur — 11 novembre 2026

`Intel8271Drive.Clock.cs`, dans le module Acorn, conserve le temps, la fraction et la rotation dans leur propriétaire existant. Son échéance est le minimum reçu du8271 et des prochaines entrées physiques représentées : flux, retombée de donnée, index/retour de rotation et publication d'une révolution écrite. Les changements de commande/support et le chargement d'une piste imposent le parcours ordinaire. Pour une distance d nanosecondes, le premier numéro de période est calculé par plafond((d×fréquence−fraction)/1e9), conformément à l'accumulation entière du Tick existant ; seul le préfixe strictement antérieur est calculé directement. Les cadences où une période atteint une révolution conservent le parcours ordinaire.

Les frontières gardent l'ordre existant : temps du lecteur, signaux de commande, retombée de donnée, flux/index et rotation, incrément du compte source puis avancement du contrôleur. Le contrat exige le rattrapage avant toute nouvelle entrée/transaction et la livraison des effets à leur date source. Aucun ordonnanceur commun ni modification de la puce8271, du séparateur nominal accepté ou des états sauvegardés. La carte Atom reste sur Tick à ce stade ; aucun gain machine annoncé.

Qualification retenue après mesure : lorsque du flux est chargé ou qu'une écriture est en cours, AdvanceTo garde la boucle TickCore canonique. Les calculs de préfixe n'y amortissent pas leur coût. Ils restent utilisés au repos et pour la mécanique sans flux, strictement avant ses frontières. Le parcours conserve la fraction générale ; les cadences intégrales évitent seulement le calcul d'une fraction connue nulle. La frontière calculée avant un préfixe est réutilisée directement, sans seconde recherche.

Premier passage abandonné :5 échecs du nouveau cache de notification (null non publié au premier abonnement),3 réussites puis arrêt explicite du processus de test devenu très lent à cause de comparaisons répétées de tables immuables. Aucun succès global déduit du TRX `20261111-drive-advance-debug.trx`. Initialisation du cache corrigée au premier abonnement. Le banc comparait aussi le mauvais bit de fin de commande : correction vers ResultFull/Busy, sans changement matériel. La table de flux est ensuite comparée à chaque remplacement de référence dans l'un ou l'autre parcours, ses index et les états variables à chaque observation.14 nouveaux cas corrigés passent en4s (`20261111-drive-advance-corrected-debug.trx`). Deux contrôles supplémentaires indépendants fixent les frontières index2ms/200ms à1/3MHz. Passage suivant :302 contrôles Debug réussis,0échec/ignoré en1min29s, comprenant les16 cas nouveaux, les raccordements8271 existants et les suites Atom/System (`20261111-drive-advance-integration-debug.trx`). Contrôle ciblé final après qualification :23 cas réussis,0échec/ignoré en4s (`20261111-drive-qualified-final-debug.trx`), couvrant aussi les tests préexistants de fractions et d'absence de sondage des fournisseurs.

Mesure isolée Debug, source1MHz/contrôleur2MHz, un million de périodes par fenêtre :3échauffements puis5paires d'instances neuves, ordre alterné, aucune compilation/test concurrent pendant les fenêtres, création de piste et vérification des empreintes hors temps. Le témoin Tick est appelé un million de fois ; AdvanceTo reçoit la même cible en un appel, avec écouteur d'échéance. Cette mesure exclut le coût de la boucle source de la machine et ne mesure ni le débit disque ni des FPS RML.15paires finales ont les mêmes empreintes de tous les champs primitifs/tableaux vérifiés du transport et du contrôleur. Rapports sous `artifacts/TestsChips/Intel8271/Acorn Atom/` :

| Essai / scénario | Tick, ms par seconde émulée | AdvanceTo, mêmes unités | Décision |
| --- | ---: | ---: | --- |
| Préfixes généraux, flux actif (`20261111-drive-speed.json`) |69,22764|133,61758|Régression, rejet de ce parcours.|
| Arithmétique intégrale, flux (`…-integral.json`) |65,26728|87,22466|Encore défavorable.|
| Frontière réutilisée, flux (`…-frontier.json`) |66,43522|72,92806|Encore défavorable, qualification nécessaire.|
| Final, repos (`…-qualified.json`) |26,48306|0,00040|Calcul constant du préfixe ; durée proche de la résolution du chronomètre, aucun débit extrapolé.|
| Final, moteur sans flux |38,92386|0,00328|Rotation/index conservés, calcul direct utile.|
| Final, moteur avec flux |67,30730|65,78176|Clocks canoniques ; écarts appariés de−2,26 à+5,86%, aucun gain établi sur ce trajet.|

Les performances de carte complète et la réception effective des échéances restent à réaliser avant de retenir le raccordement Atom. Les références constructeur et les limites explicitement acceptées plus haut restent inchangées ; les comparaisons ne sont pas une nouvelle caractérisation du silicium.

Qualification complémentaire du raccordement : la première mesure de carte montre encore2–5% de surcoût avec flux, malgré la boucle canonique dans AdvanceTo. Le lecteur annonce maintenant `CanGroupClocks`/`ClockGroupingChanged` aux mutations effectives du moteur, du contenu de piste et de la porte d'écriture ; les autres sorties restent inchangées. La source peut donc conserver Tick et suspendre son écoute des échéances pendant le trajet dense, puis recevoir le retour aux intervalles utiles, sans sonder le lecteur à chaque période. Le Tick publie aussi l'échéance finale si un écouteur a été rétabli pendant son traitement. Ce contrat n'altère aucune fréquence ni activité ;24 contrôles lecteur Debug passent en4s, dont le nouveau contrôle des changements de qualification (`20261111-drive-qualification-contract-debug.trx`). La qualification est vérifiée et committée avant sa réception par la carte ; les mesures de la carte finale restent dans la validation Atom.
