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
# Reprise du 8 novembre — carte complète après les System

- [ ] Terminer les raccordements Atom avec les composants partagés déjà réalisés.
  - [x] Terminer la sortie FS notifiée du MC6847 dans `docs/tasks/chips/Mc6847/suivi.md`, avant le câblage de carte. 51 contrôles Debug réussis ; commit `f7db897b0`.
  - [x] Créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AtomBoardProbe.cs` temporairement pour relever avant modification la carte Atom complète Debug : firmware réel, vidéo et drainage audio, mêmes cycles/scénarios et empreintes de sorties, journaux sous `artifacts/TestsMachines/Acorn/Acorn Atom/` ; réutiliser ce diagnostic après raccordement et le supprimer ensuite.
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornAtom/Common/Services/AtomMachine.cs` : câbler IRQ du VIA et NMI du 8271 au CPU par notifications ; retenir les entrées PPI et transmettre leurs changements depuis l'horloge bande, le comparateur bande, REPT et FS, au lieu de relire ces composants dans chaque Tick. Préserver chaque front et les lectures de bus.
  - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuAtomTests.cs` : couvrir lignes CPU, entrées PPI en modes strobed, FS, bande, REPT et reset ; comparer au niveau physique attendu et vérifier l'absence d'interrogation périodique.
  - [x] Supprimer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AtomBoardProbe.cs` après les mesures avant/après et consigner les résultats effectifs ; conserver les preuves sous `artifacts/TestsMachines/Acorn/Acorn Atom/`.
  - [x] Créer `artifacts/.tmp-archives/machines/Acorn/Acorn Atom/colour-board-issue1-1982.html` depuis la notice Acorn déjà consultée ; préserver les références réutilisables sans confondre l'extension 50 Hz et le MC6847 de base.
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornAtom/Common/Dictionaries/AtomKeyboardMap.cs` et `Services/AtomKeyboard.cs` : traduire les caractères hôtes et le pavé numérique vers les contacts et modificateurs réels, conserver commandes physiques et relâchements, mémoriser la matrice entre changements d'entrée/sélection. L'observation par ROM suit ci-dessous.
  - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuAtomTests.cs` : ponctuation AZERTY, touches du pavé, modificateurs, commandes, conflits du SHIFT physique unique et relâchements. 47 contrôles Atom Debug réussis.
  - [x] Créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AtomInputProbe.cs` temporairement : démarrer les ROM réelles, saisir via HostCharacters et pavé numérique des calculs BASIC qui écrivent leurs résultats en RAM, vérifier guillemets et correction par Retour arrière ; enregistrer le vrai tampon vidéo et le résultat sous `artifacts/TestsMachines/Acorn/Acorn Atom/`, fermer la carte dans finally.
  - [x] Supprimer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AtomInputProbe.cs` après consignation et régénérer l'assembly de tests sans dépendance aux ROM externes.
  - [x] Créer `artifacts/.tmp-archives/machines/Acorn/Acorn Atom/mame-atom-20261108.cpp` depuis la source de comparaison déjà consultée et en conserver l'empreinte ; réutiliser sa table de contacts et les résultats du BASIC réel.
  - [x] Créer un diagnostic invité temporaire `tests/RetroMediaLab.LocalEmulationTests/Emulation/AtomMediaProbe.cs` : réutiliser les ROM et le transport bande/Disc Pack existants pour SAVE/NEW/LOAD/RUN, captures natives et contrôle de persistance, avec destruction/détachement dans finally. Créer les médias de validation sous `F:/Retro/Acorn Atom/` via le service SDK existant, sans écraser un fichier existant ; conserver les captures sous `artifacts/TestsMachines/Acorn/Acorn Atom/`. Bande et disquette réussies ; correction de l'attente du diagnostic après LIST, sans changement matériel.
  - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AtomMediaProbe.cs` : PRINT $7 (bip), programme graphique BASIC en boucle, captures du vrai rendu et mesures de carte complète Debug avec et sans Disc Pack ; vidéo/drainage audio, cycles et empreintes consignés. Deux cas réussis ; Release non exécutée avant la priorité donnée à la revue statique.
  - [x] Supprimer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AtomMediaProbe.cs` après consignation des résultats ; conserver les preuves et les médias sous leurs emplacements autorisés.
  - [ ] Modifier les fichiers générés de l'assembly de tests pour en retirer le diagnostic externe, puis les fichiers sous `build/Debug/Retro Media Lab/` avec tous les modules à la clôture du bloc Atom pour son essai live. Attendre la fin de la revue statique demandée sans build.

Résultats du raccordement : 18 contrôles Atom Debug réussis. Comparaison de carte complète à ROM réelle : 24,301 → 24,016 ms/champ au repos ; 25,753 → 25,934 avec bande. Cycles et empreintes vidéo/audio identiques dans chaque passage avant/après ; aucun gain global significatif annoncé. Diagnostic externe supprimé et assembly de tests régénéré avec zéro erreur. Voir `docs/project/emulations/Acorn/Acorn Atom/validation.md`. La machine reste ouverte, notamment pour la saisie hôte et les essais invités/live.
