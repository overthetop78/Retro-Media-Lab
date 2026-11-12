# SN74LS257B — multiplexeur des commandes

- [x] Réaliser les quatre multiplexeurs à sorties trois états.
  - [x] Réaliser le boîtier indépendant de la console.
    - [x] Créer `src/RetroMediaLab.Chips/Logic/TexasInstruments/SN74xx/SN74LS257/Interfaces/ISn74Ls257.cs`, `Models/Sn74Ls257Timing.cs`, `Services/Sn74Ls257.cs` : deux groupes de quatre données, sélection commune, G haut désactive les pilotes, connaissance et haute impédance distinctes, délais TI SDLS148 page4 séparés data/select/activation/désactivation, réutilisation du stockage local, événements et snapshots avec propagations en cours.
  - [x] Vérifier le boîtier avant câblage des manettes.
    - [x] Créer `tests/RetroMediaLab.Chips.Tests/Logic/TexasInstruments/SN74xx/SN74LS257/Sn74Ls257Tests.cs` : toutes les sélections des256 couples de groupes, entrées inconnues avec résolution lorsque les données concordent, G connu/inconnu, délais de données/sélection/pilotage, sorties simultanément résolues, reprise JSON au milieu des délais ; exécuter le groupe avec les décodeurs et le son.
