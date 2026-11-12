# SN74LS139A — deux décodeurs indépendants

- [x] Réaliser le décodeur complet.
  - [x] Réaliser les signaux et états.
    - [x] Créer `src/RetroMediaLab.Chips/Logic/TexasInstruments/SN74xx/SN74LS139/Interfaces/ISn74Ls139.cs`, `Models/Sn74Ls139Timing.cs`, `Services/Sn74Ls139.cs` : deux décodeurs 2→4, validation basse, inconnus résolus par table, trajets A/B à deux ou trois étages et G à deux étages selon TI SDLS013A pages2–3, délais distincts montée/descente ; réutiliser CombinationalOutputs privé à l’instance, snapshots indépendants et refus atomiques.
  - [x] Vérifier le décodeur.
    - [x] Créer `tests/RetroMediaLab.Chips.Tests/Logic/TexasInstruments/SN74xx/SN74LS139/Sn74Ls139Tests.cs` : table complète à trois états, les deux moitiés et huit sorties, parcours G/A/B à deux/trois niveaux, feedback via le stockage local déjà vérifié dans SN74LS32, reprise pendant propagation et validation des profils ; exécuter le groupe avec SN74LS32.
