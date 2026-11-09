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
  - [x] Modifier `docs/project/emulations/Acorn/Acorn System/System 4/validation.md` : consigner les raccordements vérifiés et leurs différences réelles par rapport au System 3.
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


- [x] Revoir et vérifier le System 4 actuel après le System 3.
  - [x] Créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/System4NativeProbe.cs` : construire le profil System 4 par SystemFunctions.Create avec ses ROM existantes, ouvrir l'image BASIC existante en lecture seule par le SDK, vérifier catalogue/chargement exact/exécution/calcul sur le port 0, déplacer le même média vers le port 1 à chaud et vérifier CAT1 et ses octets lus ; relever trois fenêtres de 160 champs avec vidéo/audio et détacher/libérer toutes les ressources possédées dans finally.
  - [x] Créer les preuves finales `artifacts/TestsMachines/Acorn/Acorn System 4/20261109-system4-current.bmp` et sa copie `artifacts/TestsChips/Intel8271/Acorn System 4/20261109-current.bmp` depuis le framebuffer natif, sans retouche.
  - [x] Modifier `docs/project/emulations/Acorn/Acorn System/System 4/validation.md` et `docs/chips/Controllers/Intel8271.md` : consigner les différences réelles de profil avec System 3, les fichiers de câblage communs relus, les résultats actuels et leurs unités ; ne pas réécrire les puces communes sans défaut établi.
  - [x] Supprimer `tests/RetroMediaLab.LocalEmulationTests/Emulation/System4NativeProbe.cs` et régénérer l'assembly sans le diagnostic externe ; conserver uniquement les captures finales prévues hors Git.

System 4 actuel : chargement exact BASIC, calcul 46, CAT1 après déplacement à chaud du média, 512 octets lus sur le second port. Trois fenêtres Debug 10,562988 / 10,471156 / 11,107172 ms par champ (180,064–191,001 %). Capture native actuelle inspectée. Aucun nouveau changement de production nécessaire.

Diagnostic System 4 supprimé, assembly régénérée sans dépendances : code 0, aucune erreur, quatre avertissements préexistants. Suite : réalisation du générateur physique System 5, avant ses nouveaux essais de carte.


- [x] Reprendre le générateur réel de la carte System 5.
  - [x] Créer le circuit `src/RetroMediaLab.Chips/Controllers/Acorn/SystemClock/` selon `docs/tasks/chips/SystemClock/implementation.md`, avec les trois compteurs physiques et leurs horloges partagées ; vérifier ses sorties avant de modifier le raccordement de la machine.

Générateur 24 MHz réalisé dans Chips, avec trois 74S163 et ses sept sorties matérielles ; 21 contrôles Debug passent (12 compteur, neuf générateur), 58 ms. Le raccordement System 5 et la suppression du calcul de phase VIA restent à réaliser après WAIT/READY ; aucun résultat de cadence machine actuel n’est encore annoncé pour ce réseau.

  - [x] Créer `artifacts/.tmp-archives/machines/Acorn/Acorn System 5/System5-cpu-via-clock-62.png` depuis le schéma PDF 62 déjà archivé : vérifier directement le fil PH2 du VIA 6522A et les sorties CPU/bus ; consigner la cadence et la liaison réellement observées dans la fiche System 5 avant le câblage.

- [x] Raccorder les circuits System 5 terminés à leurs vrais destinataires.
  - [x] Créer `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornSystem/Common/Services/SystemFastCpuBus.cs` : adaptateur des buffers CPU/Acorn Bus, R/W transmis au changement par ICpuBusCyclePins, niveaux initiaux explicites, absence de transaction externe pendant concession ; Tick CPU ne cadence plus les périphériques à la place du générateur.
  - [x] Créer `Services/AcornSystemMachine.Clock.cs` dans le même dossier : raccorder le générateur 24 MHz au circuit WAIT/READY, PH0 au CPU, PH1/PH2 CPU aux deux fronts VIA 6522A ; cadencer vidéo/disque et référence audio/clavier depuis la sortie libre 2 MHz ; conserver l'ordre temporel avant les transferts CPU et le générateur libre pendant reset/attente.
  - [x] Modifier `Services/AcornSystemMachine.cs` et `Constants/SystemConstants.cs` : instancier ces circuits uniquement sur Fast6502A, avancer le montage par ses clocks source, appliquer RESET via la broche CPU et ses périodes réelles ; retirer `_viaPhase` et le faux 1 MHz fixe du VIA, conserver le tick direct 1 MHz des autres cartes.
  - [x] Créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuSystemClockTests.cs` : vérifier ratio source/CPU, phases et délai du VIA, maintien des clocks vidéo/disque pendant WAIT, concession/reprise sans accès externe et reset sans remise à zéro du générateur ; exécuter les régressions System utiles.
Câblage courant : 32 contrôles Clock/RAM/E/S System passent en Debug (678 ms). Première compilation du test corrigée pour lever la collision de nom Intel8271, sans changement du matériel. Source libre, WAIT/READY, PH0 CPU, phases VIA, vidéo/disque et adaptation du bus sont raccordés ; la validation avec les vraies images reste le bloc suivant.

- [x] Vérifier le System 5 assemblé sur ses images existantes, après les dernières modifications.
  - [x] Créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/System5CurrentClockProbe.cs` : construire le profil par SystemFunctions.Create avec NOS80/VDU80 existants, monter `F:/Retro/Acorn System/System BASIC.ssd` en lecture seule via le SDK, saisir CAT/BASIC/PRINT 12+34 par clavier, vérifier lecture physique, empreinte BASIC chargée et résultat écran, puis trois fenêtres isolées de 40 champs avec vidéo/audio et compteurs source/CPU ; détacher le média, les entrées et libérer le disque possédé dans finally.
  - [x] Créer `artifacts/TestsMachines/Acorn/Acorn System 5/20261109-system5-clock-wiring-current.bmp` et sa copie `artifacts/TestsChips/Mos6502/Acorn System 5/20261109-clock-wiring-current.bmp` depuis le framebuffer réel du calcul invité, sans retouche ; consigner le montage et la portée dans les fiches CPU/VIA/générateur/machine.
  - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/System5CurrentClockProbe.cs` : distinguer les captures des passages d'optimisation du compteur puis du générateur par un suffixe de scénario borné, conserver la capture de référence et réutiliser exactement les mêmes fenêtres et assertions.
  - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/System5CurrentClockProbe.cs` : ajouter un passage write sans répétition des mesures, créer exclusivement une copie temporaire `F:/Retro/Acorn System/RML-System5-20261109-write-probe.ssd` si elle n'existe pas, la monter par le SDK en écriture, saisir SAVE/LOAD PROBE par le DOS invité et vérifier huit octets après effacement puis après flush/fermeture/réouverture ; vérifier l'image source inchangée et supprimer uniquement la copie créée, après détachement et fermeture dans finally.
  - [x] Créer les captures natives `artifacts/TestsMachines/Acorn/Acorn System 5/20261109-system5-clock-wiring-write.bmp` et sa copie TestsChips/Mos6502/Acorn System 5/20261109-clock-wiring-write.bmp depuis le catalogue invité après cette sauvegarde et relecture ; consigner la persistance et le résultat réellement observé.
  - [x] Supprimer `tests/RetroMediaLab.LocalEmulationTests/Emulation/System5CurrentClockProbe.cs` après consignation et régénérer l'assembly sans le diagnostic utilisant les images externes ; ne conserver que les captures finales prévues.

Validation native du montage courant effectuée : CAT, BASIC exact, calcul 46, SAVE et deux LOAD exacts dont réouverture réelle de fichier. 177 contrôles permanents passent. Diagnostic externe supprimé, assembly régénérée sans dépendances : zéro erreur, quatre avertissements préexistants. Copie temporaire et compagnons absents. Dernier débit Debug 23,213584–23,585582 % ; la machine n'est pas clôturée, l'examen des coûts logiciels source reste ouvert avant la décision de performance et la livraison.

- [x] Mesurer les cartes actuelles après les modifications communes, avant toute nouvelle optimisation.
  - [x] Créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/SystemCurrentPerformanceProbe.cs` : charger les ROM existantes des System 1, 2, 3/6502, 3/6809, 4 et 5 par SystemFunctions, chauffer puis chronométrer trois fenêtres identiques au moniteur avec vidéo/audio ; relever les cycles réels et les débits des circuits d'horloge System 5 isolés, sans fenêtre RML. Libérer les entrées et détacher les médias dans finally.
  - [x] Modifier les fiches de validation System 1 à 5 et des circuits concernés : consigner les mesures actuelles, configuration de compilation, scénarios, unités et limites, sans présenter les anciennes mesures comme actuelles.
  - [x] Supprimer `tests/RetroMediaLab.LocalEmulationTests/Emulation/SystemCurrentPerformanceProbe.cs` après utilisation et régénérer l'assembly sans ce diagnostic externe et ses éventuels artefacts temporaires.

- [x] Poursuivre la réduction des validations C# redondantes du générateur, sans changer le circuit.
  - [x] Modifier successivement `src/RetroMediaLab.Chips/Logic/TexasInstruments/SN74xx/SN74S163/Services/Sn74S163.cs`, puis `src/RetroMediaLab.Chips/Controllers/Acorn/SystemClock/Services/SystemClockGenerator.cs`, selon leurs suivis : factoriser les opérations canoniques des vrais fronts lorsque le contrat de période complète garantit déjà les niveaux de départ et de fin ; conserver le chemin public par niveaux, ses contrôles, les captures partagées et tous les états locaux. Vérifier et mesurer chaque bloc avant de retenir le changement.

Mesures effectuées sur le code de production 2480c530d : six configurations au moniteur, trois fenêtres de 80 présentations après chauffe. Débits Debug actuels consignés dans chaque validation : System 1 979,7–1 052,5 %, System 2 187,4–200,9 %, System 3/6502 183,1–184,1 %, System 3/6809 186,8–195,0 %, System 4 175,3–182,9 %, System 5 22,2–22,8 %. Générateur 24 MHz isolé : 7,878–8,278 millions de périodes/s, coût limitant établi avant ses récepteurs. Pas de mesure Release dans ce passage, aucun gain comparatif déduit des anciens scénarios BASIC. Diagnostic temporaire supprimé ; assembly régénérée sans lui, zéro erreur, quatre avertissements préexistants. Première invocation dotnet test corrigée (no-dependencies appartient à dotnet build), import Contracts du banc corrigé avant son exécution. Premier passage réussi mais journal normal sans chiffres ; second passage avec journal détaillé utilisé pour les valeurs consignées. Aucune modification de production dans ce bloc ; optimisation System 5 toujours ouverte.

Opérations de fronts factorisées et vérifiées, aucune modification du fonctionnement matériel. System 5 au même moniteur après modifications : 76,259704 / 75,102638 / 76,012375 ms par présentation, 26,184209–26,587615 % réels en Debug, environ 14,6 % de durée hôte en moins. BASIC exact, calcul 46 et même capture native, 177 contrôles de carte/disque/audio réussis. Fiches compteur, générateur, CPU et System 5 actualisées ; diagnostics supprimés et assemblies régénérées sans eux. La revue restante des coûts du réseau et de ses destinataires demeure ouverte avant décision de performance et livraison ; la clôture historique du bloc initial ne couvre pas cette reprise.

- [ ] Corriger la capture et la sérialisation vidéo réelles de la VDU80 avant de reprendre la clôture System 5.
  - [x] Créer le composant `RetroMediaLab.Chips/Logic/TexasInstruments/SN74xx/SN74LS374/` selon son suivi, couvrir toutes ses fonctions et mesurer son débit, puis committer ce bloc.
  - [x] Créer le composant `RetroMediaLab.Chips/Logic/TexasInstruments/SN74xx/SN74LS165/` après avoir documenté sa variante et ses entrées réelles dans son suivi ; couvrir toutes ses fonctions et mesurer son débit, puis committer ce bloc.
  - [ ] Terminer le câblage vidéo selon les schémas de la carte 400,019.
    - [x] Créer les extraits PNG des pages utiles 84, 85 et 87 du manuel sous `artifacts/.tmp-archives/machines/Acorn/Acorn System 5/`, puis ceux du schéma complet après identification de sa page : consigner les fronts 2/12 MHz, NL, DE, CU et données ROM établis dans `docs/project/emulations/Acorn/Acorn System/System 5/vdu80.md` avant de modifier le chemin vidéo.
    - [ ] Modifier `SystemVideo.cs` et les fichiers de câblage `AcornSystemMachine.Clock.cs`/`.cs` dans `Emulators/AcornEmu/Machines/AcornSystem/Common/Services/` : raccorder le latch de caractère, DE retardé et le sérialiseur aux vraies horloges 2/12 MHz du générateur suivant la carte 400,019 ; définir les autres fichiers concrets après vérification des chronogrammes, conserver le signal natif et la frontière des champs, puis vérifier logiciel, médias et performances actuelles.

Les deux circuits manquants de capture/sérialisation VDU80 sont réalisés : SN74LS374 (10 contrôles), SN74LS165 historique (12 contrôles) ; ensemble logique/générateur/WAIT-READY 67 réussites. Chacun dépasse sa cadence nécessaire dans les scénarios isolés documentés. Sources TI archivées, variante 165 sans suffixe vérifiée dans le livre historique, aucune substitution d'émulateur prise comme preuve. Les diagnostics sont retirés et l'assembly régénérée ; preuves de raccordement à produire au bloc de carte suivant. Les performances System 5 précédentes ne sont pas déclarées comme celles d'une carte utilisant déjà ces deux nouveaux composants.

Phases de la carte 400,019 consignées dans System 5/vdu80.md après inspection des figures 10/14/15 et du schéma PDF 93. Prérequis supplémentaire corrigé dans le CRTC : MA physique, DE et CU disposent désormais de notifications au changement, sans sondage de VDU à ajouter au rythme des dots ; 88 contrôles CRTC réussissent, et la trace de référence ne change pas. Mesures isolées documentées dans sa fiche, diagnostic supprimé. Le raccordement de la capture et de la sérialisation reste l'action suivante ; aucun nouveau débit System 5 ni signal vidéo corrigé annoncé dans ce passage.

Prérequis ROM du trajet VDU80 : composant `src/RetroMediaLab.Chips/Memory/NationalSemiconductor/MM52116/Services/Mm52116.cs` et contrat/constantes réalisés suivant le suivi Mm52116, avec délais explicites et sans valeur par défaut de carte. Neuf contrôles dédiés et groupe final de 164 contrôles réussis ; banc autonome supprimé. Les références acquises donnent des maxima, pas la mesure d'un exemplaire : question sur le profil temporel en attente. Poursuivre le raccordement matériel après cette décision, sans inventer le délai réel à partir d'une borne constructeur.

Pendant l'attente du choix de délais VDU80, correction indépendante du raccordement de données : SN74LS165 reçoit des bits parallèles partiellement inconnus sans en faire des zéros connus. Broches A/B/H constantes préservées, C–G résolues à la réponse MM52116. Fixture autonome de raccordement vérifiée, 28 contrôles ROM/registre réussis ; aucune durée imposée à la carte réelle, aucune nouvelle cadence machine revendiquée. Les détails et fichiers modifiés sont dans le suivi Sn74LS165. Le choix entre plafonds documentés et mesures électriques reste demandé avant le câblage temporel de la carte.

### VDU80, reprise après clarification utilisateur — 9 novembre 2026

La question des délais nominaux non publiés ne bloque plus le trajet : profil numérique explicite NL 20 ns typiques, ROM 450/120/100 ns maximums, sans les attribuer à une mesure de carte. `RetroMediaLab.Chips/Controllers/Acorn/Vdu80CharacterGenerator/` assemble les trois composants vérifiés, conserve les échéances locales et reçoit ses clocks par notifications. Sept contrôles propres et groupe final de 56 contrôles réussis en Debug (80 ms), banc temporaire retiré et assembly régénérée. Le suivi du bloc et les mesures sont dans `docs/tasks/chips/Vdu80CharacterGenerator/implementation.md` et `docs/chips/Controllers/Vdu80CharacterGenerator.md`.

Le montage source + trajet vidéo isolé atteint 18,88–19,10 % des 24 MHz nécessaires en Debug, sans CPU/CRTC. Ce n'est pas une nouvelle mesure de la machine complète. La conservation de la prochaine échéance n'a pas établi de gain significatif. Reste à raccorder ce circuit au rendu de la machine, puis réduire les coûts logiciels du montage sans supprimer les fronts matériels et vérifier l'ensemble. System 5 reste en cours ; les anciennes annonces de cadence ne couvrent pas ce nouveau raccordement.

Complément VDU80 : source seule environ 268–272 ms et circuit seul 204–209 ms pour 100 ms internes. Conservation directe des niveaux vidéo et de leur connaissance : circuit seul 171–182 ms (réduction médiane environ 12,3 % dans ce scénario). Une garde d'appel supplémentaire sans gain établi a été retirée. Groupe final 56 contrôles réussis, 78 ms ; banc supprimé. Ces résultats n'actualisent pas la cadence machine complète. Raccordement au rendu et coût du générateur toujours à traiter.

- [x] Raccorder le trajet VDU80 vérifié à System 5.
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornSystem/Common/Services/SystemVideo.cs`, créer `SystemVideo.Monochrome.cs` et modifier `AcornSystemMachine.Clock.cs` : câbler MA/RA/DE/CU aux entrées du circuit, données RAM à leur changement, niveaux 2/12 MHz du générateur ; remplacer DrawMonochromeCell par la capture des dots effectivement émis. Garder les horloges libres pendant WAIT/reset CPU, la vidéo 40 colonnes, les champs et leur parité ; garder le framebuffer dans SystemVideo.
  - [x] Créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuSystemMonochromeTests.cs` : vérifier les 80 caractères aux bords, les rasters de ROM, les changements RAM, le XOR curseur et l'inversion, les champs entrelacés/progressifs et la conservation des clocks pendant reset CPU ; exécuter les contrôles du groupe concerné, sans image externe ni fenêtre RML.
  - [x] Créer puis supprimer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuSystem5VduProbe.cs` : vérifier le moniteur et BASIC réellement chargé depuis la disquette déjà acquise, calcul 46, rendu natif et performances actuelles (trois fenêtres), conserver les captures sous `artifacts/TestsMachines/Acorn/Acorn System 5/` et `artifacts/TestsChips/Vdu80CharacterGenerator/Acorn System 5/`, supprimer le banc et régénérer l'assembly. Consigner les résultats dans les fiches de machine et de composants.

Priorité utilisateur confirmée : réduire les coûts des puces avant de poursuivre le câblage VDU80. Profil du réseau 24 MHz relevé ; tentatives de pré-décodage, forme arithmétique et propagation conditionnelle sans gain significatif, retirées. Le contrôle supplémentaire des 256 combinaisons compte/commande du compteur reste ; groupe final 26 réussites, 58 ms, banc et traces nettoyés. Aucune résolution de la régression ni nouvelle vitesse complète revendiquée. Étape suivante : chemin de réception des fronts du SN74LS165, où SetClock repasse par la capture/résolution générale même sur le front descendant.

Gain établi sur le SN74LS165 : chemin complet SetClock simplifié en conservant le contrat de captures partagées. Scénario isolé 2,4 millions de périodes : 184–196 ms avant, 156–161 ms après (environ −16,3 % en durée moyenne). 37 contrôles registre/ROM/trajet VDU80 réussis, 73 ms ; banc supprimé et assembly régénérée. Cette mesure ne résout ni ne remesure encore le coût global de System 5 ; priorité maintenue aux puces.

Générateur 24 MHz optimisé : placement direct des bits et parcours des seuls destinataires changés. Réseau isolé avec 12/2 MHz : 2749–2795 ms avant, 2449–2485 ms après pour 24 millions de périodes (environ −10,9 % en durée moyenne). Sept sorties observées : gain global environ 3,6 %, moins favorable que la seule transformation des bits ; détail conservé dans SystemClock.md. Groupe permanent compteur/générateur/VDU80 : 34 réussites, 59 ms ; banc retiré et assembly régénérée. Aucun front, compte ou état perdu, aucune nouvelle cadence machine complète revendiquée.

Contrôle initial du raccordement VDU80 : compilation module Debug réussie. Groupe de dix contrôles carte/horloges : huit réussis, deux échecs curseur/inversion à mi-caractère. Les bords 80 colonnes, RA, RAM, champs progressifs/entrelacés et les quatre contrôles d'horloge passent. Le manuel identifie un réseau RC CU omis du profil initial ; clarification utilisateur demandée pour un profil nominal calculé/documenté. Référence constructeur acquise : HD46505SP-2 renommé HD68B45SP ; les sorties suivent le descendant CLK avec plafonds séparés. Ne pas contourner cet écart dans les assertions ni le framebuffer ; le bloc reste ouvert.

Contrôle supplémentaire autonome du reset CPU : l'instance VDU80 conserve et avance ses propres comptes pendant Reset(false), soit +9 captures caractère, +54 clocks dot et +4500 ns, source +108 périodes. Groupe ciblé hors les deux cas de curseur déjà échoués : neuf réussites, 204 ms ; ne pas présenter ce groupe comme validation du curseur ou de la machine entière. Les deux assertions de curseur restent présentes et inchangées. Aucun RML ouvert, aucune livraison Debug utilisateur lancée.

RC du curseur réalisé selon l'accord utilisateur, faute de mesure réelle disponible : R12/R13/C6 exacts du schéma, seuil relatif de référence 50 % documenté comme convention calculée, charge conservée, franchissements locaux à 1 ns. Sept contrôles RC plus trajet/registre/ROM : 44 réussites, 79 ms ; groupe System/8271/audio/volumes hors sauvegardes différées : 185 réussites, 6 s. Les deux anciens cas de curseur sont vérifiés suivant le profil électrique calculé et un contrôle indépendant des notifications CU à 167/135 ns ; la supposition initiale d'un curseur aligné exactement sur six dots était non établie. Aucun déplacement correcteur dans le framebuffer, aucun changement du skew. La demande de profil en attente est résolue. Reste l'essai natif réel et la mesure complète du System 5 raccordé.

- [x] Nettoyer le bloc VDU80 vérifié après son essai natif.
  - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornSystem/Common/Constants/SystemVideoConstants.cs` : supprimer les seules constantes du dessin direct désormais sans référence, le trajet matériel restant dans Chips.
  - [x] Supprimer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuSystem5VduProbe.cs`, régénérer l'assembly sans banc temporaire et consigner preuves/conditions/mesures actuelles dans `docs/tasks/machines/AcornSystem/system5-validation.md`, `implementation.md` et la fiche VDU80 ; conserver les captures réelles ignorées, puis commit du raccordement vérifié. La performance reste un point ouvert, pas une clôture de la machine.

Raccordement VDU80 et preuve native réalisés : démarrage NOS80, CAT/chargement BASIC depuis le SSD SDK en lecture seule, empreinte exacte C000, PRINT 12+34 = 46. Captures actuelles inspectées et conservées séparément sous TestsMachines et TestsChips/Vdu80CharacterGenerator, passage 20261109-094546. Tableau complet dans la validation System 5 : 115,806415–116,049232 ms/champ, 17,206490–17,242568 % en Debug, ratio source/CPU exactement 12. La première assertion de banc confondait le résultat 46 avec le prompt > sur la même ligne ; seul le banc est corrigé puis l'essai logiciel repassé, sans répéter les mesures déjà valides. Nettoyage des constantes inutilisées, suppression du banc, disque détaché/libéré dans finally, assembly régénérée. Groupe final 185 contrôles réussis (7 s). Raccordement terminé ; machine toujours en cours pour performance insuffisante.
