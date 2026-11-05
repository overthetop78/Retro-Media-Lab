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
