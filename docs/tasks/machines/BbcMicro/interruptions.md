# Suivi — BbcMicro

- [ ] Sorties NMI émises par les contrôleurs et reçues sans consultation répétée.
  - [x] Modifier `Intel8271.cs`, `Intel8271.Commands.cs`, `Intel8271.Bus.cs`, `Intel8271.Registers.cs`, `Intel8271.SerialRead.cs`, `IIntel8271.cs` sous `src/RetroMediaLab.Chips/Controllers/Intel/FDC/8271/` : notifier les changements du bit de sortie au moment des mutations de statut.
  - [x] Modifier `Wd1770.cs`, `Wd1770.Registers.cs`, `Wd1770.Bus.cs`, `IWd1770.cs` sous `src/RetroMediaLab.Chips/Controllers/WesternDigital/WD177x/WD1770/` : notifier l'état IRQ/DRQ combiné après les opérations matérielles, sans ajouter de front intermédiaire lors des mutations atomiques.
  - [x] Modifier également `Wd1770.Commands.cs` et `Wd1770.Transfers.cs` : produire la notification directement depuis les mutations IRQ/DRQ, combiner leurs mutations atomiques et retirer le rappel de vérification ajouté à chaque Tick ; conserver les fronts sans surveillance périodique de la sortie.
  - [x] Modifier `Mc6854.cs`, `Mc6854.Status.cs`, `Mc6854.Receive.cs`, `Mc6854.Transmit.cs`, `IMc6854.cs` sous `src/RetroMediaLab.Chips/Network/Motorola/M6800/6854/` : publier les changements IRQ après mutations internes, sans consultation du statut par le module à chaque cycle CPU.
  - [x] Modifier `Disks.cs`, `Econet.cs`, `BbcMicroMachine.Interrupts.cs`, `BbcMicroMachine.cs` et créer `Enums/BbcNmiSources.cs` dans `.../BbcMicro/Common/` : recevoir les niveaux, appliquer la validation réseau et agréger les contributions dans la carte, sans lecture répétée des contrôleurs.
  - [x] Modifier les fiches `docs/chips/Controllers/Intel8271.md`, `Wd1770.md` et `docs/chips/Network/Mc6854.md` après vérification de leurs chemins réels pour décrire les notifications écrites et la validation différée.
