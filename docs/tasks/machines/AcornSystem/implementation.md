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
  - [x] Corriger la fréquence nominale System 1 établie par le manuel constructeur.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Common/Machines/AcornSystem/Constants/ModelConstants.cs` et `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornSystem/Common/Constants/SystemConstants.cs` : remplacer 1 008 000 par 1 000 000 Hz, quartz indiqué dans la nomenclature du manuel System 1 archivé ; conserver les résultats historiques datés et documenter cette correction.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuSystemRamIoTests.cs` : vérifier les fréquences nominales CPU System 1/standard/rapide et leur conservation dans le CPU ; exécuter ce groupe existant après ajout de ces trois cas.
  - [x] Corriger la phase télétexte du raccordement System 2 partagé avec les System 3/4.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornSystem/Common/Services/SystemVideo.Teletext.cs` : supprimer la copie de niveau TR6 détenue par le module et utiliser l'avancement de douze demi-points existant du SAA5050, qui conserve sa phase et restitue chaque RGB intermédiaire ; garder seulement les coordonnées de présentation dans le module.
    - [x] Modifier les résultats générés des tests existants System après la modification du raccordement et consigner le résultat, sans ajouter de nouveau test reproduisant les tests de pipeline déjà présents.
  - [ ] Modifier ce suivi : consigner la revue des composants et raccordements System 2, puis System 3 avec chacun de ses CPU, System 4 et System 5 ; ajouter avant réalisation les corrections établies avec leurs fichiers.


System 1 : composants et bus relus (`Cpu6502.Clock.cs`, `.Pins.cs`, `.Interrupts.cs`, `RamIo8154.cs`, `.Ports.cs`, `SystemTapeInterface.cs`, `AcornSystemMachine.cs`, `.Bus.cs`, `SystemHexDisplay.cs`). Le CPU possède cycles, phases et entrées ; SystemTape possède temps, bascules, compteurs et échéance ; l’INS8154 est statique et commandé par les strobes, sans horloge interne fictive. La carte conserve les raccordements et les prochaines échéances notifiées, sans refaire les calculs internes. L’afficheur intègre les durées des sorties sans cadencer le matériel. Correction : fréquence déjà prévue par le profil transmise au constructeur 6502 (valeur déclarée auparavant nulle), et contrat technique SystemTape renommé en Tape. Aucun changement de fréquence réelle, de cycle, de bypass ni d’ordonnanceur commun. Résultats : 10 tests SystemTape Debug, 87 ms ; 72 tests System/RAM-E/S, 1 s ; zéro échec/ignoré. Fiches Mos6502, Ins8154, SystemTape et validation System 1 actualisées. Les performances historiques System 1 restent celles de leur passage, aucune nouvelle mesure globale revendiquée.


### Suite ordonnée de la revue et des mesures

- [ ] Terminer la revue System 2, puis System 3 avec ses deux CPU, System 4 et System 5.
  - [x] Modifier `docs/project/emulations/Acorn/Acorn System/System 2/validation.md` : consigner le bilan du câblage 6502/8154/VDU/Tape après la revue de leurs fichiers et les corrections établies.
  - [x] Modifier `docs/project/emulations/Acorn/Acorn System/System 3/validation.md` : consigner successivement la revue 6502/8271 puis 6809/VIA, avec les corrections établies ajoutées au suivi avant leur réalisation.
  - [ ] Modifier `docs/project/emulations/Acorn/Acorn System/System 4/validation.md` : consigner les raccordements vérifiés et leurs différences réelles par rapport au System 3.
  - [ ] Modifier `docs/project/emulations/Acorn/Acorn System/System 5/validation.md` : consigner ensuite la revue de la carte rapide et du générateur physique, après réalisation des corrections de propriété temporelle définies avant modification.

Correction télétexte du 9 novembre : le niveau TR6 dupliqué dans SystemVideo est supprimé. Le SAA5050 conserve sa phase et restitue les douze RGB successifs par ClockHalfDots ; les coordonnées de présentation restent dans le module. Groupe existant AcornEmuSystemTests/AcornEmuSystemRamIoTests : **75 réussites Debug, zéro échec ni ignoré, environ 1 s**. Ce temps est celui des tests fonctionnels, pas une mesure de débit. Aucun nouveau chronométrage de machine ou gain chiffré revendiqué.


- [x] Supprimer le calcul de phase vidéo sans division sur les profils System actuels.
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornSystem/Common/Services/AcornSystemMachine.cs` : retirer `_videoPhase` et appeler directement `TickCharacter` à chaque période CPU ; VDU40 reçoit PH2 à 1 MHz et VDU80 reçoit 2 MHz comme la carte rapide, sans rapport fractionnaire dans les profils existants.
  - [x] Modifier les sorties générées du groupe de tests System existant : vérifier les champs, parités et raccordements après cette suppression ; consigner les résultats sans les assimiler à une mesure de débit.


Avancement VDU du 9 novembre : suppression de `_videoPhase` et de sa boucle dans `AcornSystemMachine.Tick`. Pour les profils configurés, `VideoClock` est toujours égal à `_clock` : PH2/1 MHz sur VDU40, 2 MHz sur VDU80/carte rapide. Un appel direct conserve donc exactement un caractère par période de bus, sans accumulateur ni division artificiels. Les compteurs CRTC et la phase TR6 restent dans leurs composants. Les **75 tests System existants réussissent en Debug, zéro échec ni ignoré, durée 1 s** après cette modification. Aucun gain de débit n'est déduit de cette durée. Compilation des dépendances de tests seulement, sans livraison dans build/Debug ni ouverture de RML.


- [x] Mesurer le System 2 actuel hors RML, rendu et drainage audio inclus.
  - [x] Créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/System2TimingProbe.cs` : charger les firmwares existants COS40/SAA5050, chauffer quarante champs puis chronométrer trois fenêtres de 160 champs ; relever cycles et empreintes hors chronométrage, libérer les entrées, transport et callbacks dans finally.
  - [x] Modifier `docs/project/emulations/Acorn/Acorn System/System 2/validation.md` : consigner les trois relevés, scénario, fréquences et limites de comparaison, après exécution isolée du diagnostic.
  - [x] Supprimer `tests/RetroMediaLab.LocalEmulationTests/Emulation/System2TimingProbe.cs` après utilisation et régénérer les sorties du projet sans ce diagnostic externe ; aucun fichier temporaire créé par le diagnostic.

Débit System 2 actuel : trois fenêtres de 160 champs, 10,184747 / 10,293092 / 10,201109 ms par champ, soit 194,305–196,372 % du profil 1 MHz. Rendu et drainage audio inclus, COS40 au moniteur ; tableau complet et limites dans la fiche System 2. Ce passage ne clôture pas la revue des autres composants ni celle des autres machines.

Nettoyage du diagnostic terminé : source supprimée, projet de tests régénéré sans dépendances, code 0 (quatre avertissements existants, aucune erreur). Aucune livraison de l’application effectuée.


- [x] Terminer le raccordement du circuit de répétition clavier System 2 avant la suite.
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornSystem/Common/Services/SystemKeyboard.cs` selon le suivi `docs/tasks/chips/SystemKeyboardRepeat/implementation.md` : retirer la gestion temporelle matérielle du module et raccorder AKD/REPEAT même après relâchement du dernier caractère ; vérifier le code retenu et le strobe avec le groupe System existant.

Circuit de répétition déplacé dans Chips, REPEAT sans AKD raccordé : 5 tests propres et 76 tests System réussis. Fiche `docs/chips/Controllers/SystemKeyboardRepeat.md`, valeurs nominales RC et portée explicites. Revue globale System 2 et mesures isolées des autres puces restent ouvertes.


- [x] Vérifier le débit séparé des composants System 2 sans modifier leur réalisation.
  - [x] Créer `tests/RetroMediaLab.Chips.Tests/Controllers/Acorn/SystemComponentTimingProbe.cs` : trois fenêtres isolées pour le 6502 avec bus mémoire, le CRTC programmé comme COS40, le SAA5050 avec tous ses demi-points, l'INS8154 en transactions RAM/ports et SystemTape en réception active ; effectuer les initialisations hors chronométrage et détacher les callbacks possédés dans finally. Présenter transactions/s pour le composant statique, cycles/s et rapport nominal pour les composants cadencés.
  - [x] Modifier les fiches existantes `docs/chips/CPU/Mos6502.md`, `docs/chips/Video/Crtc6845.md`, `docs/chips/Video/Saa5050.md`, `docs/chips/IO/Ins8154.md`, `docs/chips/Controllers/SystemTape.md` et la fiche System 2 : consigner les relevés propres à chaque scénario, leurs unités et limites, après exécution du diagnostic seul.
  - [x] Supprimer `tests/RetroMediaLab.Chips.Tests/Controllers/Acorn/SystemComponentTimingProbe.cs` et régénérer le projet de tests sans ce diagnostic ; il ne crée ni fichier, ni fenêtre, ni thread.

Débits séparés obtenus et consignés dans les cinq fiches : 6502 18,2–18,6 Mcycles/s ; HD6845S 20,3–21,4 Mcaractères/s ; SAA5050 5,81–5,83 Mcaractères/s ; INS8154 69,0–72,4 Mtransactions/s ; SystemTape 2647–2774 Mcycles PH2 de référence/s. Trois fenêtres par composant, scénarios isolés, pas de gain avant/après revendiqué.

Diagnostic de débit supprimé ; projet Chips.Tests régénéré sans dépendances, code 0, un avertissement xUnit préexistant. Revue du câblage System 2 consignée : aucune nouvelle réécriture des puces déjà validées ; corrections temporelles ciblées et vérifiées. Suite de la revue : System 3 en 6502, puis 6809, puis System 4 ; générateur System 5 toujours non raccordé.


- [x] Revoir System 3 avec le 6502 avant la carte 6809.
  - [x] Modifier les sorties générées des groupes existants 8271 Chips et Intel8271 module : exécuter les contrôles hors sauvegardes d'état après revue de Clock/Bus/DriveSignals, des fréquences 1 MHz propriétaire/2 MHz contrôleur et du raccordement Intel8271Drive, puis consigner le résultat et les limites dans les fiches 8271/System 3.
  - [x] Créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/System3NativeProbe.cs` : charger la ROM DOS System 3 et la disquette BASIC existante en lecture seule par le SDK ; saisir CAT/LOAD BASIC/BASIC/PRINT 12+34 par le clavier ; vérifier les données transférées, le résultat écran et trois relevés de cadence avec vidéo/audio ; libérer l'image possédée et détacher les entrées dans finally.
  - [x] Modifier `docs/project/emulations/Acorn/Acorn System/System 3/validation.md` et `docs/chips/Controllers/Intel8271.md` : consigner les résultats réellement obtenus par le diagnostic et les conditions de mesure.
  - [x] Supprimer `tests/RetroMediaLab.LocalEmulationTests/Emulation/System3NativeProbe.cs` et régénérer l'assembly de tests sans ce diagnostic externe ; conserver seulement les preuves finales prévues dans les dossiers machine.

System 3/6502 actuel : chargement BASIC par 8271 vérifié, empreinte exacte, PRINT 12+34=46, trois fenêtres Debug 182,947–189,285 % au prompt. Deux attentes/assertions du diagnostic corrigées après observation, aucun défaut de production établi. Résultats détaillés dans la fiche machine.

Source temporaire System3NativeProbe supprimée ; projet régénéré sans dépendances, code 0, quatre avertissements préexistants et aucune erreur. Le bloc de revue 6502 est consigné, sans annoncer la clôture de la carte 6809.


- [x] Revoir la carte System 3 en 6809.
  - [x] Modifier les tests autonomes MC6809 et leur fiche selon `docs/tasks/chips/Mc6809/implementation.md` : compléter les masques de pile et transferts, puis vérifier les broches et instructions existantes avant la carte.
  - [x] Modifier `docs/project/emulations/Acorn/Acorn System/System 3/validation.md` : consigner le câblage IRQ du VIA, NMI du 8271 et les données/strobes clavier/bande après la revue des fichiers ; ajouter avant réalisation le diagnostic de moniteur et les corrections effectivement nécessaires.


- [x] Ne créer que le circuit RAM/E-S réellement monté sur la carte CPU System.
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornSystem/Common/Services/AcornSystemMachine.cs` et `.Bus.cs` : rendre le 8154 optionnel, le créer/raccorder/réinitialiser uniquement pour Keypad6502 et Standard6502 ; conserver le VIA des cartes 6809/rapide et les décodages matériels existants.
  - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuSystemRamIoTests.cs` : compléter le contrôle de présence des circuits sur les quatre cartes avec l'absence/présence effective du 8154, puis vérifier le groupe System après correction.


- [x] Mesurer la carte System 3/6809 après les modifications actuelles.
  - [x] Créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/System6809NativeProbe.cs` : charger le moniteur et les caractères existants, utiliser les cartes RAM du profil 6809 ; saisir R, M2000/programme/G2000 puis M2010 par le clavier, vérifier l'écriture invitée 5A ; chronométrer trois fenêtres de 160 champs avec rendu/drainage audio et vérifier quatre périodes EXTAL par cycle E ; relâcher les entrées et détacher les médias/callbacks dans finally.
  - [x] Modifier `docs/project/emulations/Acorn/Acorn System/System 3/validation.md` et `docs/chips/CPU/Mc6809.md` : consigner uniquement les résultats actuels de ce passage avec leurs unités, conditions et limites.
  - [x] Supprimer `tests/RetroMediaLab.LocalEmulationTests/Emulation/System6809NativeProbe.cs` après utilisation et régénérer l'assembly sans ce diagnostic externe ; aucun autre fichier temporaire n'est créé par le test.

Mesure actuelle System 3/6809, après retrait de l’instance 8154 absente : 14,572009 / 14,172370 / 13,689057 ms par champ, 137,249–146,102 % à 1 MHz E. Programme invité 2010=5A, ratio EXTAL/E=4 vérifié. Bootstrap actuel encore à vérifier séparément.

Nettoyage du diagnostic 6809 terminé, assembly régénérée sans dépendances : code 0, aucune erreur, quatre avertissements préexistants. Aucun build de livraison de RML.


- [x] Vérifier le bootstrap disque actuel System 3/6809.
  - [x] Créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/System6809BootstrapProbe.cs` : réutiliser MemoryTrackDisk et son encodeur MediaEngine avec deux secteurs de 256 octets, descripteur FF42 du moniteur et programme 6809 ; demander D par clavier, vérifier les 512 octets lus, les octets du programme chargé, son écriture 2010=5A et son texte BOOT OK en RAM vidéo ; libérer/détacher le média, les entrées et les callbacks dans finally.
  - [x] Créer les preuves finales `artifacts/TestsMachines/Acorn/Acorn System 3/20261109-system3-6809-bootstrap-current.bmp` et `artifacts/TestsChips/MC6809/Acorn System 3/20261109-bootstrap-current.bmp` depuis le framebuffer natif actuel, sans retouche ; ces captures sont les preuves conservées, pas des fichiers temporaires du diagnostic.
  - [x] Modifier les fiches MC6809, 8271 et System 3 : consigner les résultats actuels et la portée exacte du média de test, puis mettre à jour les suivis correspondants.
  - [x] Supprimer `tests/RetroMediaLab.LocalEmulationTests/Emulation/System6809BootstrapProbe.cs` et régénérer l'assembly sans le diagnostic dépendant des ROM externes ; aucun autre fichier temporaire créé.

Bootstrap actuel réussi et capture inspectée : deux secteurs FM, 512 octets, programme 6809 exécuté, 2010=5A et BOOT OK. Premier diagnostic corrigé pour transmettre D majuscule au moniteur ; aucun changement de production nécessaire.

Diagnostic de bootstrap supprimé et assembly régénérée sans dépendances, code 0 (quatre avertissements préexistants, aucune erreur). Les deux preuves BMP finales restent conservées hors Git. Revue System 3 des deux cartes effectuée ; suite dans l’ordre : System 4, puis générateur physique System 5.
