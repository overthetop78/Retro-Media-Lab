# MC6809 — entrées matérielles conservées

- [x] Terminer les liaisons d'interruption sans sondage.
  - [x] Modifier les entrées et leurs propriétaires existants.
    - [x] Modifier `src/RetroMediaLab.Chips/CPU/Motorola/M6800/6809/Services/Cpu6809.cs` : ajouter SetIrq/SetNmi/SetFirq et les niveaux conservés ; Tick échantillonne NMI localement, Reset lit ce niveau mémorisé, supprimer le callback Firq.
    - [x] Modifier `Cpu6809.Interrupts.cs` dans le même dossier : arbitrer sur les niveaux locaux, conserver masques, vecteurs, piles, SYNC/CWAI et cycles.
    - [x] Modifier `src/RetroMediaLab.Chips/CPU/Motorola/M6800/6809/Interfaces/ICpu6809.cs` : exposer SetIrq/SetNmi/SetFirq à la place du fournisseur Firq, en conservant les autres membres du contrat.
    - [x] Modifier le propriétaire `src/RetroMediaLab.Emulation.Acorn/Emulators/AcornEmu/Machines/AcornSystem/Common/Services/AcornSystemMachine.cs` pour raccorder les sorties réelles VIA/8271 et initialiser IRQ/NMI ; aucun nouveau routage par le SDK.
  - [x] Vérifier le CPU et sa carte réelle.
    - [x] Modifier `tests/RetroMediaLab.Chips.Tests/CPU/Motorola/M6800/6809/6809Tests.cs` et `SystemInstructionTests.cs` : notifications des bus de tests, FIRQ par entrée, contrôle d'absence de lecture des fournisseurs, attentes matérielles de cycles et piles conservées.
    - [x] Créer `artifacts/.tmp-system3/Program.cs` avec le vrai moniteur 6809 et ses commandes de mémoire/exécution ; conserver captures, données et durée de la carte complète ; nettoyage suivi dans le bloc System 3.
  - [x] Corriger le décompte CWAI établi par le démarrage disque.
    - [x] Modifier `src/RetroMediaLab.Chips/CPU/Motorola/M6800/6809/Services/Cpu6809.Instructions.cs` et `Cpu6809.Interrupts.cs` : séparer les 16 cycles précédant l'attente des quatre cycles de vectorisation ; les 20 cycles minimum comprennent déjà la sortie d'attente, sans ajouter sept cycles à vingt.
    - [x] Modifier `tests/RetroMediaLab.Chips.Tests/CPU/Motorola/M6800/6809/6809Tests.cs` : vérifier séparément empilement, vectorisation et total minimum de vingt cycles, puis relancer le bootstrap disque réel du banc existant.
