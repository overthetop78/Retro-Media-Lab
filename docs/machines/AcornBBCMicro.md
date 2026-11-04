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
| ACIA | Acia6850 | `docs/chips/IO/Mc6850.md`, clôturé ; son horloge de carte reste à raccorder correctement à l’ULA série |
| ULA série | BbcSerialUla et raccordement Serial | Reprise, horloges et broches dans Chips ; 99 contrôles propres et preuves invitées RX/TX cassette A/B/B+ ; voir `docs/chips/IO/BbcSerialUla.md` pour les résultats et les limites analogiques nominales |
| CRTC, ULA vidéo, télétexte | Crtc6845, BbcVideoUla, TeletextSaa5050 | Fiches `docs/chips/Video/Crtc6845.md`, `BbcVideoUla.md`, `Saa5050.md`, clôturées |
| Disquettes 8271 / WD1770 / WD1772 | Contrôleurs séparés et propriétaires de lecteurs | `docs/chips/Controllers/Intel8271.md`, `Wd1770.md`, clôturés ; vérifier les configurations proposées par chaque machine lors du passage natif final |
| ADC lorsque présent | Adc7002 | `docs/chips/IO/Upd7002.md`, clôturé |
| Econet lorsque présent | Mc6854 dans Econet | `docs/chips/Network/Mc6854.md`, clôturé ; station, porte NMI et transport restent dans Acorn |
| Winchester | AcornScsi dans Winchester | **Reprise non terminée** : le composant possède directement IDiskImage, commandes, tampons et mécanique ; distinguer l’interface hôte, la cible SCSI/SASI et le média avant de corriger |
| RAM/ROM et extensions proposées | SidewaysRoms, MemoryExtension, ModelBPlusMemory | Sélection/adressage dans la carte ; vérifier les combinaisons, collisions d’occupation et RAM vidéo/B+ avec les contrôleurs réellement utilisés |
| Clavier, IC32, bus lent | Keyboard et logique de BbcMicroMachine/VIA | Vérifier l’intégration complète, les strobes et les entrées/sorties ; ne pas déduire sa conformité de la seule suite des puces |

La synthèse vocale TMS5220/TMS6100 apparaît dans les documents Acorn et dans les catégories de firmware, mais aucune implémentation de ces circuits n’est actuellement instanciée par le cœur BBC examiné. Les coprocesseurs Tube du Master ne sont pas automatiquement des équipements du BBC Micro. Ces absences doivent rester explicites ; cette fiche ne prétend pas que toutes les extensions historiques sont déjà proposées ou validées.

## Reprise du raccordement série

Le constat initial était une sélection des débits ignorée, une ACIA cadencée à 19 200 fixes et un CTS déduit directement du choix RS423. La reprise applique maintenant les huit sélections documentées, les deux horloges indépendantes et les broches RXD/TXD/RTS/CTS/DCD. L’ACIA reçoit uniquement les fronts externes ; le magnétophone ne lui impose plus de caractères ou de centres de bits.

La validation série du 4 novembre comporte 2 463 tests Chips et 36 tests d’intégration dans chacune des configurations Debug et Release. Les quatre cartes A/B/B+ 64/128 ont reçu 64 octets à 300 et 1200 bauds ; chacune a également enregistré un signal par son CPU puis l’a relu et comparé depuis un autre exemplaire de la même carte. Les captures natives sont sous `artifacts/TestsChips/BbcSerialUla/<Machine>/`. Ces preuves de composant ne clôturent pas les autres équipements ou médias de la machine ; Winchester et le passage final ci-dessous restent à faire.

## Vérifications de machine restant à réaliser après les composants

- A d’origine et avec ses équipements autorisés ; B avec ses équipements de base et extensions proposées ; B+ 64 et 128 avec leurs mémoires distinctes.
- MOS/BASIC/DFS et choix de ROM réellement compatibles ; démarrage, commandes clavier, vidéo, son et interruptions.
- RAM principale, shadow/private/sideways suivant modèle et extensions choisies, changement de banque et vidéo préservée.
- Cassette : moteur, 300/1200 bauds, lecture/enregistrement réels, positions et éjection ; RS423 avec débits RX/TX indépendants.
- Deux lecteurs de disquettes : géométries autorisées, boot, lecture/écriture/formatage, supports protégés et éjection ; HDD selon interface installée.
- Econet entre machines compatibles et fonctionnement sans interface installée.
- Formats des conteneurs, création formatée/non formatée, secteurs graphiques et explorateur dans MediaEngine/MediaFileSystems/MediaAnalysis ; ne pas déplacer ces responsabilités dans Chips.

Aucune de ces machines n’est déclarée entièrement finalisée par cette fiche initiale. Les résultats natifs, captures, médias empruntés à `F:\Retro`, builds et commits seront ajoutés après les passages correspondants.
