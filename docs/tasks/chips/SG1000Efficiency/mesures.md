# Efficacité individuelle des composants SG-1000
- [x] Mesurer les composants, sans modifier leur implémentation.
  - [x] Créer `tests/RetroMediaLab.Chips.Tests/Performance/Sg1000ChipEfficiency.cs` : banc autonome commun, parcours physiques, échauffement, échantillons chronométrés, volumes et allocations par composant/profil.
  - [x] Créer `tests/RetroMediaLab.Chips.Tests/Profile-Sg1000Chips.ps1` : entrée xUnit temporaire, exécutions Debug puis Release isolées, rapports et suppression de l'entrée temporaire dans finally.
- [x] Modifier `tests/RetroMediaLab.Chips.Tests/RetroMediaLab.Chips.Tests.csproj` : option RmlChipEfficiencyOnly pour compiler le banc seul, indépendamment des anciens tests dont les dépendances ont été supprimées.


Résultat : 34 cas réussis Debug et 34 Release; aucun écart d'état entre parcours. Rapports : artifacts/TestsChips/Sega/SG-1000/Efficiency/20261112T212037308Z. Chiffres et limites consignés dans docs/project/emulations/Sega/segaemu-performance-architecture.md. Aucun changement des puces.

- [x] Attribuer les coûts du profil machine aux composants.
  - [x] Créer `tests/RetroMediaLab.LocalEmulationTests/Summarize-Sg1000ComponentCosts.ps1` : exploiter les trois profils complets existants, séparer les coûts propres CPU/VDP/DRAM/autres mémoires/logique/câblage/PSG, produire ms échantillonnées par image et estimations natives explicitement distinguées, extraire les méthodes dominantes et volumes par image dans un rapport JSON/Markdown.


Réanalyse terminée sur les trois profils : partitions réconciliées, méthodes internes et volumes par image consignés. Aucun modèle de puce ni parcours machine modifié.

- [x] Réduire les redondances du transport, de la logique et des réponses DRAM.
  - [x] Modifier `src/RetroMediaLab.Chips/CPU/Zilog/Z80/Z80/Contracts/Z80BusPins.cs` et `src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Machines/SG1000/Services/Sg1000CpuBus.cs` : transmettre les niveaux/pilotages primitifs déjà encodés; remplacer les reconstructions et comparaisons Nullable par des masques, sans modifier les broches ni leur sérialisation.
  - [x] Modifier `src/RetroMediaLab.Chips/Logic/Common/Services/CombinationalOutputs.cs`, `src/RetroMediaLab.Chips/Logic/TexasInstruments/SN74xx/SN74LS139/Services/Sn74Ls139.cs` et `src/RetroMediaLab.Chips/Logic/TexasInstruments/SN74xx/SN74LS32/Services/Sn74Ls32.cs` : résoudre la table logique complète par masque/table canonique; calculer les délais seulement pour les cibles changées, avec prévalidation et ordre inchangés.
  - [x] Modifier `src/RetroMediaLab.Chips/Memory/Fujitsu/MB81xx/MB8118/Services/Mb8118.cs`, `src/RetroMediaLab.Chips/Memory/Fujitsu/MB81xx/MB8118/Services/Mb8118.ReadCycle.cs` et `src/RetroMediaLab.Emulation.Sega/Emulators/SegaEmu/Machines/SG1000/Services/Sg1000Board.Bus.cs` : éviter les sous-appels sans sortie échue et les publications sans observateur; ne livrer au VDP que les entrées effectivement changées, avec huit états DRAM distincts.
  - [x] Vérifier le lot complet.
    - [x] Créer `tests/RetroMediaLab.Chips.Tests/Performance/CombinationalTruthTableTests.cs` et modifier `tests/RetroMediaLab.Chips.Tests/RetroMediaLab.Chips.Tests.csproj` : contrôler exhaustivement les entrées ternaires 139/32, puis comparer les parcours du banc d'efficacité Debug/Release.
    - [x] Utiliser `tests/RetroMediaLab.LocalEmulationTests/Profile-Sg1000.ps1` pour produire les rapports et traces des trois ROM avec comparaison aux états/images/PCM de référence; modifier le code du lot seulement si une divergence impose une correction.


Lot vérifié. La suppression des entrées VRAM inchangées a été rejetée et retirée (date interne différente); les autres changements sont conservés. États, vidéo, PCM et compteurs matériels comparés identiques sur les trois ROM. Gains mesurés 6,57/7,03/9,63 %, cible machine non atteinte. Rapports et limites dans l'étude commune. Le lanceur du banc inclut également les trois contrôles autonomes de table/projection.
