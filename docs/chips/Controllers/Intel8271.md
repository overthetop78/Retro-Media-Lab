# Intel 8271 / 8271-6

## Périmètre et architecture

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
