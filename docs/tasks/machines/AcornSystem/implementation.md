# AcornEmu — Acorn System

- [x] Réaliser le bloc Acorn System avant Atom et Electron.
  - [x] Composants des cartes Eurocard.
    - [x] Créer `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornSystem/Common/Constants/RamIoConstants.cs` : registres, adresses de bits et taille RAM de l’INS8154, une constante par ligne.
    - [x] Créer `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornSystem/Common/Services/RamIo8154.cs` : RAM 128 octets, deux ports, directions et accès par bit, sans dépendance BBC ni publication prématurée d’une machine utilisable.
    - [x] Créer `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornSystem/Common/Constants/SystemVideoConstants.cs` : adresses des cartes VDU 40/80, taille de RAM, caractères et pixels, registres CRTC et couleurs invariantes.
    - [x] Créer `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornSystem/Common/Services/SystemVideo.cs` : utiliser les CRTC6845 et SAA5050 existants, décoder les RAM et registres des cartes, produire les pixels et le curseur sans créer de fenêtre ou de composant graphique natif.
    - [x] Créer les fichiers `SystemConstants.cs`, `SystemKeyboardConstants.cs` et `SystemTapeConstants.cs` dans le dossier Constants ; créer `Dictionaries/SystemKeyboardMap.cs`, `Services/SystemKeyboard.cs` et `Services/SystemTape.cs` : clavier et strobe, clavier hexadécimal et afficheurs System 1, tape câblée au bus et transport commun de lecture/enregistrement.
    - [x] Créer `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Constants/Cpu6809Constants.cs`, `Services/Cpu6809.cs` et ses partiels Instructions/Addressing/Interrupts : CPU 6809 complet, indicateurs, cycles et interruptions nécessaires à la carte alternative du System 3.
  - [x] Assemblage et intégration.
    - [x] Modifier `Constants/SystemConstants.cs` et créer `Services/SystemHexDisplay.cs` dans le dossier AcornSystem/Common : définir les cartes RAM, registres FDC, horloges et rendu des huit afficheurs System 1, sans changer les composants BBC.
    - [x] Modifier `Services/SystemTape.cs` : exposer le signal sous la tête pour l’écoute réelle dans le mixage du cœur, sans confondre le signal tape et les bruits mécaniques.
    - [x] Créer `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornSystem/Common/Enums/SystemCpuCard.cs`, `Services/AcornSystemMachine.cs` et ses partiels Bus/Media/Input : assembler CPU, cartes RAM/ROM/VDU, tape et contrôleur disque selon System 1 à 5 ; System 3 unique avec choix CPU 6502 ou 6809.
    - [x] Créer `Common/Machines/AcornSystem/Constants/ModelConstants.cs`, `Dictionaries/ModelCatalog.cs` et `Dictionaries/AcornSystemModel.cs` : caractéristiques matérielles et compatibilité firmware propres à chaque carte et processeur.
    - [x] Modifier `Common/Machines/Common/Enums/MachineEnums.cs`, `Enums/HardwareModelEnums.cs`, `Dictionaries/HardwareModelCatalog.cs`, `Dictionaries/MachineCatalog.cs` et les fonctions de configuration concernées : ajouter les System sans modifier l’identité des BBC existants.
    - [x] Créer `Emulators/AcornEmu/Machines/AcornSystem/Common/Dictionaries/AcornEmuSystemModelCatalog.cs`, `Functions/SystemFunctions.cs` et ses partiels Firmware/Storage ; modifier le catalogue AcornEmu, sa fabrique et `Services/EmulatorCore.cs` : réglages propres aux System, choix CPU System 3, firmware, stockage, entrées et dimensions de rendu sans dépendance aux timings BBC.
    - [x] Modifier `Common/Machines/Common/Dictionaries/AcornFirmware.OperatingSystem.cs` et `AcornFirmware.CharacterGenerator.cs` : ajouter les identités System dans les catégories existantes, sans nouveau catalogue ; ajouter les fichiers nécessaires dans `%APPDATA%/Retro Media Lab/Emulation/Machines/acorn/Firmware/` sans écrasement (moniteur System 1, COS, DOS 40/80, moniteur 6809, système 5 et caractères VDU80), contrôler taille et SHA-1.
    - [x] Modifier `Emulators/AcornEmu/Services/Via6522.cs`, `Machines/AcornSystem/Common/Constants/SystemConstants.cs`, `Services/AcornSystemMachine.cs` et `.Bus.cs` : ajouter l’entrée CA1 générique, raccorder BLANK, tape sur PB7/CB2 pour la carte 6809 et conserver les callbacks BBC.
    - [x] Modifier les ressources `Resources/00-Base` et toutes les langues Acorn : invariants dans la base, textes traduits par Argos, noms de modèles lisibles.
  - [x] Validation du bloc complet.
    - [x] Créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuSystemTests.cs` : scénarios autonomes CPU, bus, INS8154, VDU, clavier et disques ; regrouper la vérification après raccordement des composants.
    - [x] Créer puis supprimer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornSystemBootDiagnostic.cs` et ses fichiers temporaires : démarrer les ROM réelles et vérifier le rendu, en libérant toute surface et tout cœur dans finally.
    - [x] Modifier ce suivi avec les résultats ; construire application et tous les modules par `scripts/local-building.cmd --building=debug --modules=A`, vérifier exécutable et DLL, puis committer le bloc System terminé.

## Références déjà identifiées

- Manuel Acorn System 5 : https://chrisacorns.computinghistory.org.uk/docs/Acorn/Manuals/Acorn_System5Handbook.pdf
- Cartes et cartes mémoire 6502/6502A/6809 : https://github.com/mamedev/mame/blob/master/src/mame/acorn/acrnsys.cpp
- INS8154, référence à confronter au manuel (modes strobed non implémentés dans cette source, décodage set-bit à vérifier) : https://github.com/mamedev/mame/blob/master/src/devices/machine/ins8154.cpp

## Blocs suivants conservés

- [ ] Atom puis Electron : créer les suivis `docs/tasks/machines/AcornAtom/implementation.md` et `docs/tasks/machines/AcornElectron/implementation.md` avec leurs fichiers concrets avant développement.
- [ ] Archimedes : créer `docs/tasks/machines/AcornArchimedes/implementation.md`, comparer les cartes et options réelles, notamment A440/A440/1 et A5000/Alpha, avant de définir les modèles.
- [ ] Risc PC et A7000 : créer `docs/tasks/acornemu-riscpc-a7000.md`, comparer A7000/A7000+ avant de définir des modèles séparés.

ABC, Communicator, stations Unix et Pocket Book exclus de la demande actuelle. Le document Google lié exige une authentification actuellement indisponible ; la liste explicite de l’objectif reste conservée ici et sera confrontée au document dès que son accès sera possible.


INS8154 : l'ancien constat limité aux ports statiques est périmé. Le composant partagé `Chips/IO/NationalSemiconductor/INS815x/8154` couvre également les modes strobed, les handshakes, les masques trois états et INTR ; voir [la fiche complète et ses preuves](../../../chips/IO/Ins8154.md). Les cartes System utilisent les modes programmés par leur logiciel et le câblage réel, sans raccorder arbitrairement INTR aux CPU. Les reprises du 8 novembre sont suivies par modèle dans `system1-validation.md` à `system5-validation.md`.


VDU : mémoires à 0400 (40 colonnes) ou 1000/F000 (80 colonnes), CRTC à 0800 ou 1840/E840 avec miroirs de la carte 80 colonnes. Tampon 480 × 250, caractères 12/6 pixels, curseur fourni par le CRTC partagé. Sources exactes : `src/devices/bus/acorn/system/vdu40.cpp` et `vdu80.cpp` de MAME ; port 8154 contrôlé contre la fiche National Semiconductor : https://www.theoddys.com/acorn/semiconductor_datasheets/INS8154%20National%20Semiconductor.pdf . Raccordement terminé ; entrelacement vérifié par ROM synthétique puis captures des ROM réelles.



6809 : instructions officielles, adressage indexé, deux piles et interruptions intégrés. Moniteur réel démarré, contrôles autonomes pile, adressage et FIRQ réussis. Cela ne constitue pas une certification cycle par cycle.


Assemblage terminé : CPU, bus, RAM, INS8154/VIA, VDU, clavier, tape et 8271 raccordés aux profils. Aucun PSG BBC ajouté aux System. Les cartes réseau et les périphériques non émulés ne sont pas annoncés comme disponibles.



Firmware installés sans écrasement : moniteur System 1 B987354C (512 octets), COS C587DA5D (2048), DOS System 3 4CB322DA (4096), moniteur 6809 B14A884B (2048), système 5F/NOS80 E5882982 (8192) et caractères VDU80 04C326A7 (2048). Empreintes complètes confrontées aux références MAME. Variantes DOS40A 3BECC10F, DOS40B 8D492106 et DOS5/80 3F787AB8 classées compatibles distinctement des ROM officielles. Les variantes DOS80A du System 3 ne sont pas annoncées compatibles avec la carte CPU System 5 à VIA.


## Actions de validation concrètes supplémentaires

- [x] Créer les captures `artifacts/acorn-system/AcornSystem1.png` à `AcornSystem5.png` et `AcornSystem3-6809.png` depuis les tampons vidéo réels pendant le diagnostic, puis les inspecter visuellement ; ces captures de revue restent conservées, séparément des fichiers de diagnostic temporaires.
- [x] Modifier les attentes des tests existants uniquement lorsque leur hypothèse BBC est devenue incorrecte avec les System (modèles supplémentaires et images facultatives) ; garder les vérifications BBC en place et ajouter les vérifications System correspondantes.

- [x] Corriger les raccordements révélés par les démarrages réels.
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Services/EmulatorCore.HardDisks.cs` : initialiser seulement les unités réellement raccordées, afin que les System sans interface HDD n’appellent pas une méthode matérielle non prise en charge.
  - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornSystemBootDiagnostic.cs` : garantir le nettoyage du dossier isolé même si Dispose échoue ; terminer les démarrages, captures et leur inspection avant suppression du diagnostic.

## Build Debug demandé

- [x] Produire le build Debug avec tous les modules.
  - [x] Modifier les fichiers générés sous build/Debug par scripts/local-building.cmd --building=debug --modules=A, puis vérifier l’exécutable et les DLL des modules.
  - [x] Modifier ce suivi avec le résultat du build et son chemin.


Build demandé réussi (code 0). Exécutable : build/Debug/Retro Media Lab/retromedialab.exe. Les 24 modules et leurs DLL sont présents ; SHA-256 Acorn identique au fichier compilé. Avertissements CA1416 sans erreur bloquante. Validation fonctionnelle System encore distincte du build.


## Correction du rendu constaté

- [x] Corriger les lignes manquantes du VDU 40 colonnes.
  - [x] Modifier SystemVideo.cs dans Emulators/AcornEmu/Machines/AcornSystem/Common/Services : distinguer les rasters entrelacés des rasters non entrelacés pour la position et le générateur télétexte.
  - [x] Modifier SystemVideoConstants.cs dans le même composant : nommer le facteur des deux champs entrelacés.
  - [x] Modifier AcornEmuSystemTests.cs : vérifier le rendu entrelacé avec une ROM de caractères synthétique, sans fichier externe.


- [x] Vérifier les commandes disque depuis les ROM réelles.
  - [x] Modifier tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornSystemBootDiagnostic.cs : envoyer CAT via le clavier émulé aux System 3, 4 et 5, raccorder une image mémoire empruntée et constater les octets effectivement lus ; libérer le média après détachement dans finally.


- [x] Corriger la polarité clavier révélée par CAT.
  - [x] Modifier Emulators/AcornEmu/Machines/AcornSystem/Common/Services/SystemKeyboard.cs : fournir ASCII après le circuit de remise en forme, sans inverser une seconde fois les caractères déjà décodés.
  - [x] Modifier tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuSystemTests.cs : contrôler les caractères réellement fournis au bus et le changement de modificateur.


- [x] Vérifier les touches hexadécimales du System 1 avant nettoyage du diagnostic.
  - [x] Modifier AcornSystemBootDiagnostic.cs : inspecter les octets d’affichage du moniteur après M puis 0000, sans modifier le cœur pour le diagnostic, puis retirer cette instrumentation avec le fichier temporaire.


- [x] Corriger la persistance des LED multiplexées du System 1.
  - [x] Modifier Services/SystemKeyboard.cs : accumuler les segments allumés pendant une image, au lieu de conserver uniquement le dernier état instantané après extinction.
  - [x] Modifier Services/AcornSystemMachine.cs : recommencer l’accumulation à chaque image de présentation du System 1.
  - [x] Modifier AcornEmuSystemTests.cs : vérifier l’extinction entre deux balayages sans disparition prématurée des segments déjà allumés.


## Livraison du bloc System

- [x] Préparer la livraison après validation.
  - [x] Supprimer tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornSystemBootDiagnostic.cs après ses neuf scénarios réels réussis ; conserver uniquement les PNG de revue dans artifacts/acorn-system.
  - [x] Modifier les fichiers générés des tests après suppression du diagnostic, puis inscrire dans ce suivi le résultat des contrôles autonomes Acorn.
  - [x] Modifier les fichiers générés build/Debug par scripts/local-building.cmd --building=debug --modules=A après les corrections, vérifier les 24 modules et la DLL Acorn livrée.
  - [x] Modifier ce suivi avec les résultats définitifs et les limites matérielles du bloc, puis créer le commit Acorn System.


- [x] Nettoyer les fins de fichiers signalées avant livraison.
  - [x] Modifier Emulators/AcornEmu/Factories/AcornEmuMachineFactory.cs et Resources/fr-FR/Emulation.resx : supprimer la ligne vide finale supplémentaire.


Validation finale du code : 391 contrôles Acorn autonomes réussis après suppression du diagnostic. Auparavant, neuf scénarios avec ROM externes ont réussi : six démarrages (System 3 avec chacun des deux CPU) et trois CAT via clavier et 8271, avec lecture du catalogue synthétique RML TEST. Captures réelles inspectées dans artifacts/acorn-system ; aucun dossier rml-system-boot restant. Correctifs : VDU entrelacé, polarité ASCII et repos clavier, persistance LED, absence de configuration HDD quand aucun HDD raccordé. Build Debug All terminé avec code 0 : 24 modules et exécutable présents, empreinte Acorn identique. Chemin : build/Debug/Retro Media Lab/retromedialab.exe.

## Revue statique des horloges et liaisons — 8 novembre 2026

Les chiffres de validation ci-dessus sont historiques et n'incluent pas les corrections de cette reprise. La demande actuelle porte sur le code et les fiches existantes, sans nouveaux tests/builds. Strobe PB7 du clavier Standard6502 raccordé à STB du 8154 ; HSYNC/RA du CRTC raccordés par notifications au SAA5050 ; complétion des broches et phases MC6809 en cours de validation différée. Aucune nouvelle clôture fonctionnelle ou performance déduite de ces seules lectures.

- [x] Retirer l'appel périodique sans effet au codeur ASCII System.
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornSystem/Common/Services/SystemKeyboard.cs` : conserver sa référence temporelle, son échéance de répétition et son strobe ; publier l'échéance lors des changements d'entrée et avancer directement à chaque front dû. Conserver 500 ms puis 50 ms et l'action de REPT au prochain tick, sans lire les touches à chaque période.
  - [x] Modifier `Services/AcornSystemMachine.cs` dans ce dossier : conserver l'échéance notifiée dans la carte, avancer le codeur uniquement à cette échéance ou avant une entrée/reset ; sélectionner explicitement matrice hexadécimale ou codeur ASCII suivant la carte réellement montée.
  - [x] Modifier les quatre constructions de matrice dans `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuSystemRamIoTests.cs` : sélectionner explicitement le clavier hexadécimal, sans ajouter ni exécuter de test pendant la revue.

Le clavier ASCII ne reçoit plus d'appel par période quand aucune échéance ne tombe. Il conserve son temps absolu et publie chaque front de strobe dû ; la carte retient seulement la date notifiée. REPT raccourcit le délai au prochain tick comme auparavant. La matrice du System 1 ne prépare plus d'état ASCII/repeat, et le codeur ASCII ne reconstruit plus cette matrice absente. Ces changements sont écrits et relus, sans nouvelle compilation ni exécution de test pendant cette revue ; aucun gain chiffré ou nouveau verdict de machine complète annoncé.




## Continuité de la revue — 9 novembre 2026

- [ ] Revoir System 1 puis les cartes System suivantes, sans réécrire les composants déjà vérifiés sans défaut établi.
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornSystem/Common/Services/AcornSystemMachine.cs` : transmettre au constructeur 6502 la fréquence déjà définie par la carte, au lieu de laisser sa fréquence déclarée à zéro ; conserver le même avancement par les cycles de bus.
  - [x] Modifier `src/RetroMediaLab.Chips/Controllers/Acorn/SystemTape/Services/SystemTapeInterface.cs`, `Interfaces/ISystemTapeInterface.cs`, le raccordement `AcornSystemMachine.cs` et `tests/RetroMediaLab.Chips.Tests/Controllers/Acorn/SystemTapeTests.cs` : renommer les sorties/entrées techniques `Cassette…` en `Tape…`, sans alias ni migration ; conserver compteurs, échéances et signaux.
  - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuSystemRamIoTests.cs` : remplacer les quatre lectures CassetteOutput restantes par TapeOutput.
  - [x] Modifier ce suivi et les fiches concernées : consigner la revue 6502/8154/SystemTape, le raccordement System 1 et les résultats des contrôles existants après ces corrections.
  - [ ] Modifier ce suivi : consigner la revue des composants et raccordements System 2, puis System 3 avec chacun de ses CPU, System 4 et System 5 ; ajouter avant réalisation les corrections établies avec leurs fichiers.


System 1 : composants et bus relus (`Cpu6502.Clock.cs`, `.Pins.cs`, `.Interrupts.cs`, `RamIo8154.cs`, `.Ports.cs`, `SystemTapeInterface.cs`, `AcornSystemMachine.cs`, `.Bus.cs`, `SystemHexDisplay.cs`). Le CPU possède cycles, phases et entrées ; SystemTape possède temps, bascules, compteurs et échéance ; l’INS8154 est statique et commandé par les strobes, sans horloge interne fictive. La carte conserve les raccordements et les prochaines échéances notifiées, sans refaire les calculs internes. L’afficheur intègre les durées des sorties sans cadencer le matériel. Correction : fréquence déjà prévue par le profil transmise au constructeur 6502 (valeur déclarée auparavant nulle), et contrat technique SystemTape renommé en Tape. Aucun changement de fréquence réelle, de cycle, de bypass ni d’ordonnanceur commun. Résultats : 10 tests SystemTape Debug, 87 ms ; 72 tests System/RAM-E/S, 1 s ; zéro échec/ignoré. Fiches Mos6502, Ins8154, SystemTape et validation System 1 actualisées. Les performances historiques System 1 restent celles de leur passage, aucune nouvelle mesure globale revendiquée.