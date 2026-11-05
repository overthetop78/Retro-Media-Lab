# Acorn BBC Micro A, B et B+ — état de finalisation

## Périmètre demandé le 4 novembre 2026

Terminer les circuits et les raccordements des BBC Micro A, B, B+ 64 Kio et B+ 128 Kio, puis vérifier les machines complètes avec les équipements proposés. Les familles Master, System, Atom, Electron, Archimedes, Risc PC et A7000/A7000+ suivent dans cet ordre et ne doivent pas détourner ce bloc. Le travail préparatoire de la RTC Master est conservé pour le bloc suivant.

## Circuits réellement utilisés

La carte est construite par `BbcMicroMachine` et ses propriétaires de périphériques sous `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/BbcMicro/`. Cette même classe possède aussi des branches Master : leur présence dans le fichier ne signifie pas que les BBC Micro instancient une RTC, une EEPROM Compact ou une carte 80186.

| Fonction BBC | Composant actuel | État et preuve de puce |
|---|---|---|
| CPU 6502A / 6512A du B+ | Moteur NMOS Cpu6502, cmos faux | Reprise NMOS dans `docs/chips/CPU/Mos6502.md` ; les variantes CMOS ne sont pas nécessaires à ces modèles |
| VIA système / VIA utilisateur lorsqu’installé | Via6522 | `docs/chips/IO/Mos6522.md`, clôturé |
| Son | Sn76489 dans Audio | `docs/chips/Audio/Sn76489.md`, clôturé |
| ACIA | Acia6850 | `docs/chips/IO/Mc6850.md`, clôturé ; raccordement aux horloges de l’ULA série validé avec les quatre cartes, voir ci-dessous |
| ULA série | BbcSerialUla et raccordement Serial | Reprise, horloges et broches dans Chips ; 99 contrôles propres et preuves invitées RX/TX cassette A/B/B+ ; voir `docs/chips/IO/BbcSerialUla.md` pour les résultats et les limites analogiques nominales |
| CRTC, ULA vidéo, télétexte | Crtc6845, BbcVideoUla, TeletextSaa5050 | Fiches `docs/chips/Video/Crtc6845.md`, `BbcVideoUla.md`, `Saa5050.md`, clôturées |
| Disquettes 8271 / WD1770 / WD1772 | Contrôleurs séparés et propriétaires de lecteurs | `docs/chips/Controllers/Intel8271.md`, `Wd1770.md`, clôturés ; vérifier les configurations proposées par chaque machine lors du passage natif final |
| ADC lorsque présent | Adc7002 | `docs/chips/IO/Upd7002.md`, clôturé |
| Econet lorsque présent | Mc6854 dans Econet | `docs/chips/Network/Mc6854.md`, clôturé ; station, porte NMI et transport restent dans Acorn |
| Winchester | Interface hôte Acorn, cible Adaptec ACB-4000, propriétaire de média dans Acorn | Reprise clôturée dans `e692844bc` ; fiches `docs/chips/Controllers/AcornScsi.md` et `AdaptecAcb4000.md`, preuves natives DAT/DSC et EMU sur BBC B ; les combinaisons complètes des machines restent distinctes de cette preuve de composant |
| RAM/ROM et extensions proposées | SidewaysRoms, MemoryExtension, ModelBPlusMemory | Sélection/adressage dans la carte ; vérifier les combinaisons, collisions d’occupation et RAM vidéo/B+ avec les contrôleurs réellement utilisés |
| Clavier, IC32, bus lent | Keyboard et logique de BbcMicroMachine/VIA | Vérifier l’intégration complète, les strobes et les entrées/sorties ; ne pas déduire sa conformité de la seule suite des puces |

La synthèse vocale TMS5220/TMS6100 apparaît dans les documents Acorn et dans les catégories de firmware, mais aucune implémentation de ces circuits n’est actuellement instanciée par le cœur BBC examiné. Les coprocesseurs Tube du Master ne sont pas automatiquement des équipements du BBC Micro. Ces absences doivent rester explicites ; cette fiche ne prétend pas que toutes les extensions historiques sont déjà proposées ou validées.

## Reprise du raccordement série

Le constat initial était une sélection des débits ignorée, une ACIA cadencée à 19 200 fixes et un CTS déduit directement du choix RS423. La reprise applique maintenant les huit sélections documentées, les deux horloges indépendantes et les broches RXD/TXD/RTS/CTS/DCD. L’ACIA reçoit uniquement les fronts externes ; le magnétophone ne lui impose plus de caractères ou de centres de bits.

La validation série du 4 novembre comporte 2 463 tests Chips et 36 tests d’intégration dans chacune des configurations Debug et Release. Les quatre cartes A/B/B+ 64/128 ont reçu 64 octets à 300 et 1200 bauds ; chacune a également enregistré un signal par son CPU puis l’a relu et comparé depuis un autre exemplaire de la même carte. Les captures natives sont sous `artifacts/TestsChips/BbcSerialUla/<Machine>/`. Ces preuves de composant ne clôturent pas les autres équipements ou médias de la machine ; le passage final ci-dessous reste à faire.

## SSD réel : catalogue et chargement par les ROM des quatre cartes

Le 5 novembre 2026, quatre essais Release réussis (25,63 secondes pour le passage) ont assemblé les cartes par `ModelCatalog`, sans modifier la configuration utilisateur. Le média source, ouvert en lecture seule par `EmulationMediaProcessingService`, est :

`F:\Retro\Image Disquette\Acorn BBC - Applications - [SSD] (TOSEC-v2013-10-16)\Algebraic Manipulation Package (1982)(Acornsoft)\Algebraic Manipulation Package (1982)(Acornsoft).ssd`.

| Carte | RAM principale / totale B+ | ROM système / disque | Résultat natif |
|---|---|---|---|
| Model A | 16 Kio | MOS 1.20, BASIC II, DFS 0.9H / 8271 | Catalogue lu et `$.LOADER` chargé |
| Model B | 32 Kio | MOS 1.20, BASIC II, DFS 0.9H / 8271 | Catalogue lu et `$.LOADER` chargé |
| Model B+ | 64 Kio | MOS 2.00 + BASIC II, DFS 2.26 / WD1770 | Catalogue lu et `$.LOADER` chargé |
| Model B+ | 128 Kio | MOS 2.00 + BASIC II, DFS 2.26 / WD1770 | Catalogue lu et `$.LOADER` chargé |

Les commandes `*DISC`, `*CAT`, puis `*LOAD $.LOADER 2000` sont saisies par les entrées clavier du BBC. Le compteur du lecteur confirme au moins les 512 octets du catalogue ; son texte apparaît dans la mémoire écran. Après LOAD, les 48 octets de RAM à `&2000` sont comparés intégralement aux octets du fichier dans le SSD, dont le catalogue est décodé indépendamment par le banc. Le processeur n’est ni remplacé ni déplacé directement à une adresse de test. Le message final `LOAD OK 48 BYTES` est ensuite affiché par une commande BASIC, après cette comparaison réussie.

Captures réellement produites et inspectées, conservées hors Git :

- `artifacts/TestsChips/Intel8271/BbcMicroModelA/real-ssd-catalogue-load.png`
- `artifacts/TestsChips/Intel8271/BbcMicroModelB/real-ssd-catalogue-load.png`
- `artifacts/TestsChips/Wd1770/BbcMicroModelBPlus64/real-ssd-catalogue-load.png`
- `artifacts/TestsChips/Wd1770/BbcMicroModelBPlus128/real-ssd-catalogue-load.png`

Chaque essai détache son image avant de la libérer et réinitialise sa carte dans `finally`. Aucun fichier média ni réglage existant n’a été écrit. Ce passage prouve le démarrage et la lecture effective de ce SSD sur les quatre configurations ; il ne prouve pas encore l’exécution de l’application, les écritures, les deux lecteurs, les autres géométries et conteneurs, les cassettes réelles, les extensions mémoire ni le réseau. Ces vérifications restent ouvertes dans le suivi.

## Cassette UEF réelle : démodulation et chargement par le MOS

Le passage Release du 5 novembre 2026 comporte quatre essais réussis en 34,43 secondes. Le média, uniquement lu, est :

`F:\Retro\Image Disquette\Acorn BBC - Games - [UEF] (TOSEC-v2023-08-29)\Draughts v2.1 (19xx)(Acornsoft)\Draughts v2.1 (19xx)(Acornsoft).uef`.

Le relais SDK utilise l’identifiant de décodeur cassette `acorn-bbc`, comme le propriétaire BBC existant. Le premier fichier `DRAUGHTS` comprend sept blocs valides et 1 774 octets ; le signal complet dure 95,275 secondes. Le banc configure explicitement la lecture RealTime, insère le signal et saisit `*TAPE` puis `*LOAD "DRAUGHTS" 2000` par les entrées clavier. Les 1 774 octets lus dans la RAM à `&2000` correspondent intégralement aux données décodées indépendamment par MediaEngine. Le mode direct n’est pas utilisé.

| Carte | Images jusqu’à la comparaison intégrale | Calcul hôte, démarrage et saisie compris | Capture inspectée |
|---|---:|---:|---|
| A 16 Kio | 975 | 7,38 s | `artifacts/TestsChips/BbcSerialUla/BbcMicroModelA/real-uef-load.png` |
| B 32 Kio | 975 | 8,49 s | `artifacts/TestsChips/BbcSerialUla/BbcMicroModelB/real-uef-load.png` |
| B+ 64 Kio | 975 | 8,10 s | `artifacts/TestsChips/BbcSerialUla/BbcMicroModelBPlus64/real-uef-load.png` |
| B+ 128 Kio | 975 | 7,95 s | `artifacts/TestsChips/BbcSerialUla/BbcMicroModelBPlus128/real-uef-load.png` |

Les 975 images représentent environ 19,5 secondes BBC après la saisie de LOAD ; les durées hôte ci-dessus ne sont pas des mesures isolées du débit CPU. Chaque capture montre `DRAUGHTS 06 06EE`, le retour de LOAD et l’exécution de `PRINT "UEF OK 1774 BYTES"`. Le banc ne lance pas le jeu : le chargement à une autre adresse sert à comparer son contenu sans exécuter un logiciel potentiellement prévu pour un autre équipement.

Le premier média envisagé, `Neanderthal Man (1983)(Carlos, D.).uef`, contient un premier fichier `CAVE` de 15 635 octets ; il a été écarté de ce scénario commun car la zone `&2000`–`&3BFF` du A ne suffit pas. Ce refus concernait la taille du banc, pas un échec d’émulation. Un premier appel de préparation avec le nom du modèle au lieu de l’identifiant `acorn-bbc` ne demandait aucun décodeur compatible ; l’invocation du banc a été corrigée avant les essais natifs. Aucune correction du logiciel n’a été nécessaire pour le passage réussi.

Le signal est éjecté et chaque carte réinitialisée en `finally`. Les captures restent hors Git ; le fichier de diagnostic temporaire est retiré après usage. Les enregistrements WAV/UEF/CSW/RAW sont validés dans le passage suivant ; les autres fonctions de machine restent ouvertes.

## Vérifications de machine restant à réaliser après les composants

L’essai initial d’enregistrement MOS du 5 novembre reconnaissait le nom puis affichait `Data?`. Le maintien de porteuse ULA a été corrigé dans `81ed4158f`, puis la phase du diviseur de transmission MC6850 dans `6b9ee07bb`. La seconde correction permet les 16 relectures physiques ; elle ne corrige pas l’indexeur PCM, traité séparément dans MediaEngine.

Le passage final Release réussit les quatre cartes en 2,1229 minutes : chaque carte exécute `*SAVE RML 2000 2200`, attend le retour du MOS et enregistre ses 512 octets. Le signal est écrit puis rouvert par le relais SDK dans chacun des quatre conteneurs. Après RESET et effacement de la destination, `*LOAD "RML" 2000` restitue les 512 octets exacts en lecture physique. L’indexeur fournit aussi des blocs tous valides dont les données concaténées sont identiques à la source.

| Carte | WAV | UEF | CSW | RAW |
|---|---|---|---|---|
| A 16 Kio | Index + LOAD exacts | Index + LOAD exacts | Index + LOAD exacts | Index + LOAD exacts |
| B 32 Kio | Index + LOAD exacts | Index + LOAD exacts | Index + LOAD exacts | Index + LOAD exacts |
| B+ 64 Kio | Index + LOAD exacts | Index + LOAD exacts | Index + LOAD exacts | Index + LOAD exacts |
| B+ 128 Kio | Index + LOAD exacts | Index + LOAD exacts | Index + LOAD exacts | Index + LOAD exacts |

MediaEngine se recale désormais sur chaque début de caractère UART et estime la période depuis une porteuse stable, au lieu d’appliquer une grille fixe sur toute la bande. Les 72 tests FSK/Acorn/séquentiels et les 28 tests d’intégration cassette passent en Release. Les cas synthétiques couvrent les débits d’échantillonnage 22 050/44 100/48 000 Hz, une dérive de vitesse de ±2 %, la phase continue et les pauses entre caractères ; ils ne remplacent pas une validation de tous les enregistrements analogiques existants.

Les captures `artifacts/TestsChips/BbcSerialUla/<Machine>/record-replay-<format>.png` montrent le retour de LOAD et le message BASIC après comparaison. La capture B+ 128/RAW a également été inspectée dans ce passage. Les médias de travail sont supprimés en `finally`, sans écriture de configuration ni de média utilisateur. La validation reste limitée à ce scénario d’enregistrement de 512 octets pour ces quatre formats.

- A d’origine et avec ses équipements autorisés ; B avec ses équipements de base et extensions proposées ; B+ 64 et 128 avec leurs mémoires distinctes.
- MOS/BASIC/DFS et choix de ROM réellement compatibles ; démarrage, commandes clavier, vidéo, son et interruptions.
- RAM principale, shadow/private/sideways suivant modèle et extensions choisies, changement de banque et vidéo préservée.
- Cassette : moteur, 300/1200 bauds, lecture/enregistrement réels, positions et éjection ; RS423 avec débits RX/TX indépendants.
- Deux lecteurs de disquettes : géométries autorisées, boot, lecture/écriture/formatage, supports protégés et éjection ; HDD selon interface installée.
- Econet entre machines compatibles et fonctionnement sans interface installée.
- Formats des conteneurs, création formatée/non formatée, secteurs graphiques et explorateur dans MediaEngine/MediaFileSystems/MediaAnalysis ; ne pas déplacer ces responsabilités dans Chips.

Aucune de ces machines n’est déclarée entièrement finalisée par cette fiche initiale. Les résultats natifs, captures, médias empruntés à `F:\Retro`, builds et commits seront ajoutés après les passages correspondants.

## Winchester : composant raccordé et preuves obtenues

Le 5 novembre 2026, le modèle numérique ACB-4000 documenté et la carte hôte Acorn sont raccordés aux BBC avec interface Winchester installée. Le modèle expose les deux ports matériels ; les conteneurs DAT/DSC et EMU passent par le relais Emulation vers MediaEngine. Chips ne possède pas les fichiers et le module Acorn ne référence pas directement MediaEngine.

Les captures du BBC B sous `artifacts/TestsChips/Acb4000/BbcMicroModelB/Release/` montrent les essais CPU de sauvegarde/chargement ADFS et réouverture, FORMAT de 33 secteurs, lecture du remplissage, écriture/relecture de 256 octets et diagnostic natif 61h de 259 octets. Les contrôles exacts et limites de chaque passage sont consignés dans [AcornWinchester.md](AcornWinchester.md) et la fiche du contrôleur. Les captures sont hors Git ; aucune configuration ni image originale de l’utilisateur n’a été modifiée par ces diagnostics.

La validation du composant courant réussit 861 cas puces/hôte en Debug et Release, 58 cas d’intégration Acorn et 124 cas des formats médias en Release. Elle ne remplace pas les vérifications complètes ci-dessus sur chacun des A, B, B+ 64 et B+ 128 ni les essais ultérieurs sur des médias existants dans `F:\Retro`.

## Build après les corrections cassette du 5 novembre

`scripts/local-building.cmd --building=debug --modules=A` termine avec le code zéro. L’exécutable est `build/Debug/Retro Media Lab/retromedialab.exe`, Chips est présent dans `lib/retromedialab.chips.dll` et chacun des 24 manifestes de module possède sa DLL d’entrée. Le banc natif temporaire d’enregistrement a été supprimé puis l’assembly LocalEmulationTests reconstruit en Release sans ce banc. Les deux régressions autonomes utiles sont conservées.

## Deux lecteurs de disquettes : SAVE, LOAD et persistance sur deux faces

### Formatage natif DFS sur B+

Les commandes constructeur `*ENABLE`, puis `*FORM 40 0 2` ou `*FORM 80 0 2`, formatent les deux faces du lecteur 0. La syntaxe est décrite dans l’[addendum Acorn DFS](https://chrisacorns.computinghistory.org.uk/docs/Acorn/Manuals/Acorn_DiscSystemUGI2.pdf), pages 3–4. Les essais utilisent les ROM MOS/BASIC B+ et DFS 2.26 existantes ; seuls des DSD temporaires sont modifiés.

| Configuration testée | Vérification de chaque secteur | SAVE/LOAD et réouverture |
|---|---|---|
| B+ 64 Kio, DSD 40 pistes | 800 secteurs, deux faces | Deux fichiers distincts de 512 octets exacts |
| B+ 128 Kio, DSD 80 pistes | 1 600 secteurs, deux faces | Deux fichiers distincts de 512 octets exacts |

Tous les secteurs sont initialement altérés avec A5h. Après formatage, chaque piste contient dix secteurs de 256 octets, avec intégrité valide, cylindre enregistré attendu et remplissage E5h hors catalogue. Les fichiers sont rechargés après effacement de leur RAM, puis après sauvegarde, fermeture et réouverture du DSD.

Le premier passage a révélé un défaut MediaEngine : DFS inscrit un ID de face zéro sur les deux faces physiques. Le remplacement comparait cet ID à la face physique et refusait la seconde face. Création et lecture DFS partagent désormais cette représentation ; le remplacement compare l’ID enregistré du secteur d’origine, sans déplacer son adresse physique. Le [constructeur SSD/DSD de JSBeeb](https://raw.githubusercontent.com/mattgodbolt/jsbeeb/main/src/disc.js) emploie également cet ID zéro. Aucun code WD1770 ni timing n’a changé pour cette correction.

Le passage Release final réussit sept cas (cinq autonomes et deux natifs) en 1,7408 minute. Les 18 contrôles de stockage Acorn et 98 contrôles d’intégration 8271/WD/métadonnées réussissent également. Captures hors Git : `artifacts/TestsChips/Wd1770/BbcMicroModelBPlus64/dfs-form40-format.png`, `dfs-form40-save-load.png`, et sous `BbcMicroModelBPlus128/`, `dfs-form80-format.png`, `dfs-form80-save-load.png`. La capture du formatage 80 pistes inspectée affiche la fin des deux faces et « FORM 80 OK 1600 SECTORS ».

Ces essais couvrent les deux combinaisons indiquées, pas toutes les permutations RAM/géométrie, tous les conteneurs, le formatage ADFS ou les différents pas mécaniques de lecteurs physiques. Les contrôles existants de stockage ne prouvent pas l’exploration graphique de ces nouveaux DSD.

Le diagnostic temporaire est supprimé, son assembly Release reconstruite et aucun DSD de travail ne subsiste. Les cinq contrôles autonomes réussissent également en Debug (365 ms). Le build Debug avec `--modules=A` termine avec code zéro : exécutable et Chips présents, 24 manifestes et DLL d’entrée présents ; l’empreinte SHA-256 de MediaEngine dans `lib` correspond à celle de l’assembly Debug compilée.

### Validation ADFS S/M/L avec la ROM Acorn 1.30

Le B+ 64 Kio démarre avec MOS/BASIC 2.00 et `Acorn ADFS v1.30 [variant 1].rom`, déjà présents dans le dossier Firmware. Les images ADS 160 Kio, ADM 320 Kio et ADL 640 Kio sont créées par le SDK puis montées par `*ADFS`, `*MOUNT 0` et `*CAT`. Pour chacune, la ROM sauvegarde un motif de 8 192 octets par `*SAVE $.RML 2000 4000` ; la RAM est vidée avant chaque LOAD et tous les octets sont comparés, également après fermeture et réouverture du conteneur. Les trois cas initiaux réussissent en 1,0572 minute hôte.

ADFS L numérote d’abord les 1 280 secteurs d’une face avant ceux de l’autre : ce premier petit fichier ne prouvait donc pas l’accès à la seconde face. Le cas renforcé réserve un fichier par `PTR#OPENOUT "$.FILL"=&4F500:CLOSE#0`, attend le retour effectif de BASIC puis sauvegarde le motif. Il réussit en 2,4958 minutes : des octets du motif sont présents sur la face physique 1, le LOAD après réouverture est exact et l’explorateur MediaEngine extrait exactement les 8 192 octets du fichier RML. L’attente initiale fixe de ce banc était trop courte pendant l’allocation ; ce n’était pas une panne de l’émulateur.

Le média réel `F:/Retro/Image Disquette/Acorn BBC - Applications - [ADL] (TOSEC-v2013-10-16)/Scrabble Dictionary (19xx)(-)[ADFS]/Scrabble Dictionary (19xx)(-)[ADFS].adl` est ouvert en lecture seule. La ROM affiche son catalogue et charge `$.!BOOT` en RAM ; ses 16 octets correspondent au contenu extrait par l’explorateur. Ce cas réussit en neuf secondes, sans écrire l’original. Son catalogue contient aussi DICMAP, Info, MAPMAP, OLDDM, OLDMM, SCRABDIC et SDIC ; leur présence n’est pas une preuve de chargement de chacun.

Captures inspectées et conservées hors Git sous `artifacts/TestsChips/Wd1770/BbcMicroModelBPlus64/` : `adfs-adl-save-load.png` montre l’allocation, SAVE, les deux LOAD et « ADFS ADL 8192 BYTES REOPEN OK » ; `adfs-real-scrabble-save-load.png` montre le catalogue réel, LOAD de !BOOT et « F RETRO ADL LOAD OK ». Les passages ADS et ADM disposent également de leurs captures.

Le formateur `$.LIBRARY.Aform` du disque `MasterWelcome.adl` distribué dans les [sources BeebEm](https://github.com/stardot/beebem-windows/tree/master/UserData/DiscIms) refuse le B+ avec « This is the wrong type of machine ». Ce refus ne démontre pas un défaut du WD1770 : ce programme n’a pas servi à valider le formatage B+. Le passage suivant utilise ADT, compatible B/B+, pour réaliser ce formatage. Le [guide Acorn ADFS](https://chrisacorns.computinghistory.org.uk/docs/Acorn/Manuals/Acorn_ADFSUG.pdf), chapitre 9, distingue le programme utilitaire AFORM de la ROM et décrit les formats S/M/L ainsi que sa confirmation.

Les deux passages S/M complémentaires réussissent en 41 secondes : l’explorateur extrait exactement les fichiers créés par la ROM et `SectorImageFluxVisualizer` construit respectivement les 40 et 80 pistes attendues. C’est un contrôle du pipeline de visualisation, pas une capture de son interface graphique. Ces résultats ne finalisent pas la machine entière, les autres conteneurs, la création non formatée ou l’affichage interactif des secteurs. Aucun code de puce ni timing n’a été modifié pendant ces essais ADFS. Le diagnostic, les médias de travail et la capture de la tentative AFORM refusée sont supprimés ; l’assembly Release est reconstruite sans ce diagnostic.

### Formatage ADFS natif avec Advanced Disc Toolkit

La ROM `Advanced Disc Toolkit v1.78.rom`, déjà présente dans Firmware, est installée dans le support libre IC35 du B+ 64 Kio par la configuration habituelle du module. Acorn ADFS 1.30 reste dans son emplacement d’interface WD1770. Le [manuel ACP ADT MK2](https://8bs.com/othrdnld/manuals/applications/ADT_Manual_MK2.zip) décrit la syntaxe FORM ; les commandes exécutées sont `*FORM 40 0`, `*FORM 80 0 M` et `*FORM 80 0 L`, chacune confirmée par Y. Aucun programme historique n’a été modifié pour accepter le B+.

| Format | Pistes physiques vérifiées | Secteurs contrôlés | SAVE/LOAD avant et après réouverture |
|---|---:|---:|---|
| S, ADS 160 Kio | 40 × 1 face | 640 | 512 octets exacts |
| M, ADM 320 Kio | 80 × 1 face | 1 280 | 512 octets exacts |
| L, ADL 640 Kio | 80 × 2 faces | 2 560 | 512 octets exacts |

Avant l’essai, les secteurs de données sont altérés avec A5h, hors sept secteurs de structure nécessaires au montage initial. Après le formatage, toutes les pistes possèdent seize champs de 256 octets avec cylindre attendu, ID de face zéro et intégrité valide ; le remplissage précédent est absent des secteurs de données. Le catalogue créé est monté et relu après sauvegarde, fermeture et réouverture, puis le fichier RML est chargé exactement après effacement de sa RAM.

Le passage L initial a réellement terminé les 160 pistes dans l’émulation mais la projection du flux vers le conteneur refusait son ID H=0 sur la face physique 1. La régression autonome reproduisait ce refus de `SectorTrackFunctions.Replace`. La lecture et la création BBC ADFS S/M/L utilisent maintenant `AcornAdfLayout`, qui sépare ces deux informations comme le fait le formateur. Les formats Archimedes 800/1600 Kio et Amiga conservent leurs identifiants physiques propres. L’ordre des secteurs et leurs octets restent identiques ; ni WD1770 ni ses timings ne sont modifiés.

Validation finale Release : 15 réussites en 2,2785 minutes, comprenant 12 régressions autonomes et les trois formatages natifs suivis de SAVE/LOAD. Les 18 contrôles de stockage Acorn réussissent en 425 ms. Le premier essai S avait fourni un paramètre S non documenté et n’était pas retenu comme preuve ; le passage final utilise la syntaxe correcte et refuse toute sortie « Bad » ou « fault ».

Captures hors Git sous `artifacts/TestsChips/Wd1770/BbcMicroModelBPlus64/` : `adfs-form-s.png`, `adfs-form-m.png`, `adfs-form-l.png`. La capture L inspectée montre les commandes, la fin de piste 9F, le catalogue RML et « ADFS L FORM REOPEN OK 2560 SECTORS ». Ces preuves concernent ce B+ 64 et ce couple de ROM ; elles ne constituent pas la validation de toutes les autres machines, de tous les conteneurs ni de l’interface graphique d’exploration.

Le diagnostic natif est supprimé et son assembly Release reconstruite sans cette classe ; aucun média de travail ne subsiste. Les douze régressions autonomes réussissent en Debug en 802 ms. Le build `--building=debug --modules=A` termine avec code zéro : exécutable, Chips dans `lib`, 24 manifestes et DLL d’entrée présents ; le SHA-256 de MediaEngine livré correspond à l’assembly Debug compilée. La capture S inspectée montre également le catalogue RML et la réussite après réouverture des 640 secteurs.

### SAVE/LOAD DFS sur deux lecteurs

Le passage du 5 novembre crée deux DSD de 80 pistes par `EmulationSdk.MediaProcessing.CreateBlankDiskAsync`, avec catalogues DFS vierges. Chaque carte saisit par son clavier `*DISC`, puis SAVE et LOAD de quatre fichiers distincts de 512 octets sur les unités DFS 0, 1, 2 et 3. Les unités 0/2 correspondent aux deux faces du premier lecteur, 1/3 à celles du second. La RAM est effacée avant chaque LOAD et comparée intégralement au motif attendu.

| Carte | Contrôleur / ROM | Deux lecteurs et deux faces | Après Flush, fermeture et réouverture des DSD |
|---|---|---|---|
| A 16 Kio | 8271 / DFS 0.9H | Quatre fichiers exacts | Quatre LOAD exacts |
| B 32 Kio | 8271 / DFS 0.9H | Quatre fichiers exacts | Quatre LOAD exacts |
| B+ 64 Kio | WD1770 / DFS 2.26 | Quatre fichiers exacts | Quatre LOAD exacts |
| B+ 128 Kio | WD1770 / DFS 2.26 | Quatre fichiers exacts | Quatre LOAD exacts |

Les B+ ont réussi dès le premier passage (23 et 25 secondes hôte). A/B avaient révélé une perte de recalage du séparateur 8271 après la jonction de champs réécrits ; la fiche `docs/chips/Controllers/Intel8271.md` détaille la régression autonome et sa correction. Leur passage final réussit en 52,956 secondes pour les deux cartes. Les contrôles supplémentaires de piste uniforme n’avaient pas reproduit cette perte : leur réussite seule ne validait pas le scénario natif.

Captures : `artifacts/TestsChips/Intel8271/BbcMicroModelA/two-drives-two-sides-save-load.png`, même chemin pour `BbcMicroModelB`, et `artifacts/TestsChips/Wd1770/<BbcMicroModelBPlus64 ou BbcMicroModelBPlus128>/two-drives-two-sides-save-load.png`. Les images de B et B+ 128 ont été inspectées. Ces preuves couvrent les écritures DFS sur ces DSD ; elles ne prouvent pas encore le formatage par le CPU, tous les conteneurs, la double densité, la protection ou les autres fonctions ouvertes de la machine.

Le banc temporaire a été supprimé et son assembly Release reconstruit sans cette classe. Les médias de travail sont supprimés ; les captures demeurent hors Git. Après la correction 8271, le build Debug avec `--modules=A` termine avec code zéro : exécutable présent, 24 DLL d’entrée présentes et empreinte SHA-256 de la DLL Chips livrée identique à celle compilée en Debug.

## Conteneurs de flux SCP et HFE — 5 novembre 2026

Le service d’ouverture de MediaEngine accepte désormais sa représentation physique de flux. Il conserve les révolutions capturées, leur résolution et leur durée d’index ; il n’invente ni secteurs, ni capacité logique. Les champs SectorSize/SectorCount/SectorsPerTrack restent à zéro. Les lecteurs BBC acceptent SCP/HFE dans leurs limites de pistes et de faces ; le contrôleur continue de décoder les impulsions du média, sans conversion FM/MFM fondée sur l’extension. Aucun code de puce n’est changé dans ce bloc.

Les fenêtres d’écriture restent physiques jusqu’à publication par le writer du conteneur. HFE v1 utilise une grille commune exacte lorsqu’elle est représentable. Si cette grille dépasserait la taille de piste HFE v1, HFE v3 conserve les impulsions avec ses changements de débit et ses cellules partielles, sans les arrondir. Le décodeur v3 utilise maintenant la fréquence absolue des opcodes, indépendante du débit initial de l’en-tête, conformément à la [spécification HxC](https://www.hxc2001.com/floppy_drive_emulator/HFE-file-format.html) et au [lecteur FlashFloppy](https://github.com/keirf/FlashFloppy/blob/master/src/image/hfe.c). Les timings ou dispositions que le conteneur ne peut représenter restent refusés ; les régions faibles ne sont pas silencieusement figées par une sauvegarde et restent protégées en écriture.

Les quatre essais natifs utilisent un BBC Model B 32 Kio avec MOS 1.20/BASIC II : Intel 8271 + DFS 0.9H, puis WD1770 + DFS 2.26, chacun avec SCP et HFE. Le CPU exécute les commandes MOS `DISC`, `SAVE PROOF 3000 +200` et `LOAD PROOF 4000`. Les 512 octets chargés sont comparés au motif initial, la RAM destination est effacée, le média est détaché, sauvegardé, fermé puis rouvert, et le CPU recharge le même fichier exactement. Le signal de la piste 39 non écrite reste identique après réouverture. Quatre réussites Release en 8,30 s ; le HFE modifié reste réouvrable en écriture.

Captures natives inspectées hors Git : `artifacts/TestsChips/Intel8271/BbcMicroModelB/flux-scp-save-load.png`, `flux-hfe-save-load.png`, et mêmes noms sous `artifacts/TestsChips/Wd1770/BbcMicroModelB/`. La capture WD1770 montre Acorn 1770 DFS puis « HFE SAVE LOAD REOPEN OK ». Le premier banc omettait le générateur de caractères et produisait une capture noire : elle a été remplacée par la capture lisible, avec la ROM SAA5050 existante. L’échec intermédiaire HFE « Drive fault » provenait de la conversion prématurée vers sa grille v1 ; il n’est plus reproduit dans les quatre passages finaux.

Les 14 cas autonomes de flux couvrent résolutions/révolutions distinctes, silence final, timing variable, fenêtres traversant l’index, conservation des impulsions hors grille, grille HFE commune, limites de résolution, phases de 500/1 000/1 500 ns en HFE v3 et échappement des données ressemblant à ses opcodes. Le passage regroupé avec les fenêtres physiques, la lecture des secteurs et les limites des lecteurs BBC compte 63 réussites Release, environ une seconde d’exécution. Le diagnostic externe et les médias de travail sont supprimés ; son assembly Release est reconstruite sans cette classe.

Ces résultats ne constituent pas encore la validation de tous les conteneurs existants, des protections à régions faibles, des autres modèles BBC avec ces flux, de leur création dans l’interface ni de l’explorateur graphique de ces conteneurs. Ces vérifications restent ouvertes dans le suivi général des machines et médias.

Les 14 nouveaux contrôles autonomes réussissent aussi en Debug en 431 ms. Le cas de piste absente conserve deux révolutions lorsqu’elles sont présentes sur les autres pistes, afin que son écriture ne réduise pas les captures SCP intactes à une seule révolution.

Livraison finale : `scripts/local-building.cmd --building=debug --modules=A` termine avec code zéro. `build/Debug/Retro Media Lab/retromedialab.exe`, Chips dans `lib`, les 24 manifestes et leurs 24 DLL d’entrée sont présents. Le SHA-256 du MediaEngine livré est identique à celui compilé en Debug ; aucun diagnostic externe ne reste dans les sources.
