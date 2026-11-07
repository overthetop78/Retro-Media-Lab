# AcornEmu — Acorn Atom

- [x] Réaliser le bloc Atom après le commit System 8224c1751.
  - [x] Composants et carte mère.
    - [x] Créer `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornAtom/Common/Constants/AtomPpiConstants.cs` : ports 8255, masque des directions, commande de configuration et commande bit set/reset, une constante par ligne.
    - [x] Créer `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornAtom/Common/Services/AtomPpi8255.cs` : ports du PPI dans le mode statique utilisé par Atom, lectures des entrées et des verrous de sortie, directions séparées des demi-ports C et commandes BSR ; aucune fenêtre ni ressource native.
    - [x] Créer `Constants/AtomConstants.cs` et `Constants/AtomVideoConstants.cs` dans le même composant : adresses, horloges, capacités RAM/ROM, dimensions vidéo et broches PPI suivant la carte réelle.
    - [x] Créer `Services/AtomVideo.cs` et `Dictionaries/AtomCharacterGenerator.cs` : générateur MC6847, modes texte, semi-graphiques et graphiques, synchronisation et caractères internes documentés ; distinguer cadence matérielle et présentation.
    - [x] Créer `Dictionaries/AtomKeyboardMap.cs` et `Services/AtomKeyboard.cs` : matrice réelle, SHIFT/CTRL/REPT et BREAK ; réutiliser les entrées communes.
    - [x] Déplacer `Machines/AcornSystem/Common/Services/SystemTape.cs` vers `Emulators/AcornEmu/Services/AnalogTape.cs` et ses constantes vers `Constants/AnalogTapeConstants.cs` ; créer `Enums/TapeSignalMode.cs`, séparer la broche analogique directe du modem FSK, modifier les références de `AcornSystemMachine.cs` et `AcornEmuSystemTests.cs` ; réutiliser ce transport pour Atom sans copier la gestion lecture/enregistrement.
    - [x] Modifier `Constants/AtomConstants.cs` : adresses 0A00/0A04 et miroir 0078 du Disc Pack, RAM 2000–27FF et 3C00–3FFF, ROM DOS E000–EFFF, niveau du buzzer et demi-période tape ; le bloc par défaut du bus Acorn est 0000.
    - [x] Créer `Services/AtomMachine.cs` et ses partiels Bus/Media : raccorder CPU6502 existant à 1 MHz, PPI, VIA existant, vidéo, RAM, ROM, tape, buzzer et interface disque 8271 ; conserver la propriété des médias dans le cœur.
    - [x] Modifier `Constants/AtomVideoConstants.cs` et `Services/AtomVideo.cs` : conserver les géométries MC6847 dans la définition matérielle nommée, sans nombres de résolution directement dans le service de rendu.
  - [x] Modèle, firmware et adaptation au module.
    - [x] Créer `Common/Machines/AcornAtom/Constants/ModelConstants.cs`, `Dictionaries/ModelCatalog.cs` et `Dictionaries/AcornAtomModel.cs` : modèle Atom, RAM réellement installable, emplacements ROM et compatibilités par rôle.
    - [x] Modifier les catégories existantes `Common/Machines/Common/Dictionaries/AcornFirmware.*.cs` ; ajouter BASIC/OS et flottants dans le dossier Firmware Acorn de l’utilisateur, vérifier leurs tailles et empreintes sans remplacer les fichiers existants.
    - [x] Modifier `Common/Machines/Common/Enums/MachineEnums.cs`, `Enums/HardwareModelEnums.cs`, `Dictionaries/HardwareModelCatalog.cs`, `Dictionaries/MachineCatalog.cs` et `EmulationGlobalUsings.cs` : déclarer Atom sans changer les identités existantes.
    - [x] Créer `Emulators/AcornEmu/Machines/AcornAtom/Common/Constants/AtomDriveConstants.cs` et `Dictionaries/AtomFloppyDriveModels.cs` ; modifier `Emulators/AcornEmu/Functions/StorageFunctions.cs` pour injecter le lecteur Atom et ses formats sans règles Atom dans MediaEngine.
    - [x] Modifier `src/RetroMediaLab.MediaEngine/Images/Creation/BlankSectorImageFactory.cs` : permettre la construction du format Atom DOS déjà lu et écrit par MediaEngine, en utilisant sa géométrie existante.
    - [x] Modifier `Common/Machines/Common/Contracts/ConfigurationContracts.cs` : déduire correctement les familles System et Atom au lieu de retourner BBC pour chaque modèle.
    - [x] Modifier `Emulators/AcornEmu/Services/EmulatorCore.cs` : extensions Atom, absence d’options vidéo BBC et bonne identité de machine lors de la lecture d’une tape Atom.
    - [x] Créer `Emulators/AcornEmu/Functions/AtomFunctions.cs` et ses partiels Firmware/Storage ainsi que `Machines/AcornAtom/Common/Dictionaries/AcornEmuAtomModelCatalog.cs` ; modifier les catalogues et la fabrique existante pour injecter les réglages, firmware, clavier et médias Atom.
    - [x] Modifier les ressources `Resources/00-Base` et toutes les langues concernées : invariants communs, traductions Argos pour les nouveaux libellés.
  - [x] Validation et livraison en fin de bloc.
    - [x] Créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuAtomTests.cs` : scénarios autonomes utiles PPI, bus, clavier et modes vidéo.
    - [x] Créer puis supprimer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornAtomBootDiagnostic.cs` : démarrer les ROM réelles, saisir un programme BASIC, exercer tape et disquette ; conserver les captures de revue dans `artifacts/acorn-atom`, supprimer les fichiers temporaires et libérer les objets possédés dans finally.
    - [x] Modifier les fichiers générés sous `build/Debug` avec `scripts/local-building.cmd --building=debug --modules=A` ; vérifier exécutable et 24 modules après les corrections regroupées.
    - [x] Modifier ce suivi avec les résultats et créer le commit du bloc Atom terminé avant Electron.

## Références matérielles retenues

- Source MAME : https://github.com/mamedev/mame/blob/master/src/mame/acorn/atom.cpp
- Carte mémoire : RAM CPU 0000–03FF ; cartes RAM texte 2800–3BFF ; vidéo 8000–97FF ; extension ROM A000–AFFF ; PPI B000–B003 avec miroirs ; VIA B800–B80F ; ROM C000–DFFF et F000–FFFF.
- `abasic.ic20` : 8192 octets, SHA-1 0072c83458a9690a3ea1f6094f0f38cf8e96a445, deux moitiés à C000 et F000. `afloat.ic21` : 4096 octets, SHA-1 ebcde5b36cb3a3344567cbba4c7b9fde015f4802, à D000.
- PPI A : sélection clavier bits 0–3 ; vidéo A/G et GM0–2 bits 4–7. PPI C : tape, buzzer et CSS en sortie basse ; horloge 2400 Hz, entrée tape, REPT et synchronisation vidéo en entrée haute.
- Le lecteur d’images Atom DOS existe dans MediaEngine (`AcornAtomDskReader`) : réutiliser ce format, ne pas recréer un catalogue matériel dans MediaEngine.
- Cadence MC6847, montage PAL et variantes matérielles à confronter aux schémas et à une seconde source avant de fixer le profil vidéo.

Référence MC6847 (ROM interne extraite du silicium) : https://github.com/mamedev/mame/blob/master/src/devices/video/mc6847.cpp ; 280 octets, SHA-1 5e9d68e55e73cae3d28adaff34fe115e00029009. Le décodeur devra utiliser ce fichier dans Firmware, comme le SAA5050 BBC, sans intégrer le binaire à l’assembly. Cadence du circuit de base : 3579545 / (228 × 262) Hz. Palette et organisation 5 × 7 relevées dans cette source ; carte PAL encore à confronter au manuel.

Carte Atom raccordée à la fabrique : CPU, bus, clavier, vidéo, buzzer, tape, Disc Pack, réglages et sélection des firmwares. Démarrage réel et programme BASIC 123+456=579 vérifiés ; sauvegarde/relecture par les broches tape et par le DOS sur disquette vérifiées.

Firmware installés et identifiés : Acorn Atom BASIC OS.bin (8192), Acorn Atom Floating Point.bin (4096), Acorn Atom DOS.bin (4096), MC6847.bin (280). SHA-1 conformes aux références MAME listées ci-dessus. Archives utilisées : https://github.com/Abdess/retrobios/tree/main/bios/Acorn/BBC%20Micro et https://colorcomputerarchive.com/repo/ROMs/MAME-MESS/mc6847.zip ; téléchargement et extraction en mémoire, aucun ZIP temporaire conservé.

## Corrections issues du diagnostic groupé

- [x] Corriger la création vierge et la cadence de saisie du diagnostic.
  - [x] Modifier src/RetroMediaLab.MediaEngine/Images/Creation/BlankSectorImageFactory.cs : renseigner les 400 secteurs du catalogue Atom vierge pour que le lecteur Atom existant le reconnaisse.
  - [x] Modifier tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornAtomBootDiagnostic.cs : respecter le temps de relâchement et de traitement de la ROM entre les touches, puis vérifier BASIC, modes graphiques, tape et catalogue ensemble.

- [x] Modifier tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornAtomBootDiagnostic.cs : sélectionner DOS avant CAT et confirmer SAVE/LOAD par une touche après le démarrage du transport, suivant les manuels Atom.

- [x] Modifier tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornAtomBootDiagnostic.cs : compléter le catalogue par SAVE, NEW, LOAD et RUN sur disquette afin de vérifier les écritures réelles du DOS, puis supprimer ce diagnostic après validation.

## Résultats

404 contrôles Acorn autonomes réussis ; trois diagnostics ROM réelles réussis (BASIC/graphismes, tape, disquette). La séquence disque sélectionne *DOS, lit CAT puis effectue SAVE/NEW/LOAD/RUN sur une image ouverte en écriture. Les séquences tape confirment RECORD TAPE et PLAY TAPE par RETURN, suivant Atomic Theory and Practice, chapitre 1.10.

Références : https://chrisacorns.computinghistory.org.uk/docs/Acorn/Tech/Acorn_Atom_Disc_Pack.pdf ; https://www.theoddys.com/acorn/acorn_system_computers/atom/Atomic%20Theory%20and%20Practice.pdf .

Debug All : script terminé avec code 0, 24 manifestes et DLL présents ; empreinte du module Acorn livré identique à celle de la compilation. Exécutable : build/Debug/Retro Media Lab/retromedialab.exe. Aucune modification de production depuis ce build ; seuls les diagnostics temporaires et le suivi ont évolué.

- [x] Nettoyer les diagnostics après validation.
  - [x] Supprimer tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornAtomBootDiagnostic.cs et reconstruire son assembly de tests pour retirer ses références externes ; conserver uniquement les captures PNG sous artifacts/acorn-atom.
