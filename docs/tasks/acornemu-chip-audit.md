# Audit matériel AcornEmu — composants et câblages

- [ ] Confronter tous les composants utilisés aux documents constructeur et aux sources d'émulateurs, puis corriger les écarts.
  - [ ] Compléter le VIA 6522 en cours.
    - [ ] Créer `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Components/Mos6522/Constants/Via6522Constants.cs` : constantes distinctes des registres, masques, modes et broches, une déclaration par ligne.
    - [ ] Créer `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Components/Mos6522/Enums/ViaControlMode.cs` : huit modes PCR nommés, distincts des bits encodés dans chaque moitié du registre.
    - [x] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Components/Mos6522/Services/Via6522.cs` et créer `Components/Mos6522/Services/Via6522.Ports.cs` : séparer registres et broches, entrées CA2/CB2, modes indépendants d'interruption, impulsions/handshake, latches d'entrée et PB6 de T2 ; conserver les callbacks des cartes existantes et leur propriété des périphériques.
    - [ ] Créer `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuVia6522Tests.cs` : cas autonomes des modes PCR/ACR, fronts, acquittements, broches et comptage PB6. Contrôler ce groupe après les modifications regroupées ; aucun fichier externe.
  - [ ] CPU et bus des machines.
    - [ ] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Services/Cpu6502*.cs` : compléter les accès transitoires, instructions NMOS, IRQ/NMI/RESET et particularités CMOS selon les variantes réellement utilisées ; compléter ce fichier de suivi avec les références et les contrôles utiles avant les changements.
    - [ ] Modifier `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Services/Cpu6809*.cs`, `Cpu80186*.cs` et `CpuArm26*.cs` selon les écarts établis : jeu d'instructions, exceptions, accès mémoire, cycles, banque de registres, cache et coprocesseurs propres aux variantes installées ; consigner les limites restantes ici.
    - [ ] Modifier les services des cartes sous `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines` pour corriger les décodages d'adresses, arbitrages, horloges, attentes, interruptions et liaisons réellement câblées, suivant leurs manuels. Ne pas déduire la fidélité d'une carte de tests isolés de ses puces.
  - [ ] Affichage, mémoire et audio.
    - [ ] Modifier `Services/Crtc6845.cs`, `TeletextSaa5050.cs`, `Sn76489.cs` et les services vidéo/audio des cartes sous `Machines` selon les écarts confirmés. Distinguer le MC6845 original (VSYNC fixe) des autres variantes ; vérifier les ULA Atom/Electron/BBC et leurs accès partagés, couleurs, cadences et son.
    - [ ] Modifier les services MEMC/IOC/VIDC, mémoire shadow/sideways, ULA Tube, RTC/EEPROM, clavier et contrôleurs des cartes sous `Machines` selon leurs registres, masques, interruptions, resets et câblages documentés ; inscrire ici chaque composant effectivement examiné et les corrections nécessaires.
  - [ ] Contrôleurs et interfaces.
    - [ ] Modifier `Services/Intel8271*.cs`, `Wd1770*.cs`, `Upd765*.cs` et `Upd765DriveControl.cs` selon les écarts confirmés : variantes, commandes, recherche d'identifiants, flux, index, DMA/PIO, reset, erreurs, protections et cadences. Reprendre les limites NEC encore ouvertes dans `docs/tasks/acornemu-archimedes.md`, sans considérer les seuls secteurs réguliers comme preuve de fidélité complète.
    - [ ] Modifier `Services/Hd63463*.cs`, `AtaPio*.cs`, `AcornScsi*.cs` et leurs adaptateurs de cartes pour corriger les écarts de commandes, statuts, buffers, DMA, géométries et durées suivant les fiches constructeur ; conserver les médias sous la responsabilité de leur propriétaire.
    - [ ] Modifier `Services/Acia6850.cs`, `Mos6551*.cs`, `Ns16450*.cs`, `Mc6854*.cs`, `Upc82C711Configuration.cs` et les autres composants d'E/S effectivement présents sous `Machines` pour corriger les écarts documentés de broches, registres, débits, interruptions et resets.
  - [ ] Validation et livraison par groupe terminé.
    - [ ] Modifier ce suivi avec les références précises, les écarts corrigés et les limites de chaque composant ; ajouter avant exécution les chemins de tout nouveau test nécessaire et la suppression de tout diagnostic externe temporaire.
    - [ ] Modifier les sorties `build/Debug` avec `scripts/local-building.cmd --building=debug --modules=A` après validation du groupe à livrer, puis modifier ce suivi avec le résultat. Ne pas déclarer l'audit global terminé sur la base de la compilation ou d'un sous-ensemble de tests.

Les chemins `Services/...` ci-dessus sont relatifs à `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/`. Les groupes de machines Archimedes, Risc PC et A7000 restent dans l'objectif initial ; leurs composants ajoutés rejoindront cet audit.

Références de départ : fiche MOS 6522 (https://www.princeton.edu/~mae412/HANDOUTS/Datasheets/6522.pdf), sources jsbeeb, b-em, b2, Atomulator et MAME. Les sources d'émulateurs permettent de comparer des détails d'implémentation ; les variantes et valeurs du matériel restent à établir avec les documents correspondants.

Groupe VIA ports/T2 : code déplacé dans Components/Mos6522 ; interruptions indépendantes, fronts CA2/CB2, modes manuel/handshake/impulsion, latch CA1/CB1 et comptage PB6 couverts par les tests autonomes. Les modes de décalage série, PB7 de T1 et les détails complets de retard des minuteries restent à vérifier/corriger dans cet audit ; le groupe ne constitue pas une affirmation de VIA entièrement fidèle.

� poursuivre pour le VIA complet : modifier Components/Mos6522/Services/Via6522*.cs et ses constantes pour le registre � d�calage, PB7/T1, les d�lais des compteurs et les diff�rences de lecture/latch entre ports A et B, apr�s confrontation d�taill�e aux fiches constructeur. Les autres chemins Services/<puce> de ce document se trouvent d�sormais sous Components/<puce>/Services apr�s le rangement demand�.
