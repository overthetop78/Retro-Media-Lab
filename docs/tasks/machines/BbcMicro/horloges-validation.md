# Suivi — BbcMicro

- [ ] Validation groupée du bloc horloges et signaux, après les modifications cohérentes.
  - [x] Modifier ce fichier avec le résultat réel de la compilation et des tests Chips Debug, corriger dans les fichiers du bloc les erreurs révélées avant de poursuivre.
    - [x] Modifier `tests/RetroMediaLab.Chips.Tests/Shared/ArchitectureTests.cs` pour afficher les noms exacts des composants sans contrat dans l'échec, sans supprimer ni assouplir l'exigence.
    - [x] Modifier `tests/RetroMediaLab.Chips.Tests/Controllers/Intel/FDC/8271/ClockTests.cs` pour conserver attendu/résultat dans l'ordre recommandé et supprimer l'avertissement introduit par le nouveau contrôle.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuSerialWiringTests.cs` : compléter le relais ObservedAcia pour les notifications TXD/RTS/diviseur et les phases TxC/RxC ajoutées au contrat, puis consigner le résultat du groupe d'intégration.
    - [x] Modifier `tests/RetroMediaLab.LocalEmulationTests/Emulation/AcornEmuSerialWiringTests.cs` : comparer les champs matériels hors callbacks de câblage et vérifier explicitement la présentation initiale des broches à la construction puis au PowerOn.
  - [x] Modifier ce fichier avec le résultat réel des tests Chips Release et des contrôles d'intégration Acorn concernés, sans assimiler ces suites à une mesure du BBC complet.
  - [x] Modifier les fiches des composants du bloc avec les résultats correspondant réellement à leurs cas, et conserver les performances globales dans `docs/chips/CPU/Mos6502.md` uniquement après mesure du BBC complet.

Premier passage Debug du bloc : compilation Chips et tests réussie ; 3 401 tests réussis, 3 échecs, aucun ignoré, 51 s. Échecs : EveryPublicComponentHasItsOwnHardwareContract, HardwareContractsExposePublicInstanceSignals et ScsiHostAdapterCostTests.DataStrobesAreAllocationFreeAndPreserveTheHardwareLatches(false), 8 136 octets observés au lieu de zéro. Aucun résultat Release ou d'intégration Acorn n'est encore disponible pour ce bloc.

Suite ciblée des sept composants modifiés en Release : 723 réussites, zéro échec ni ignoré, 710 ms. Les deux contrôles de coût SCSI réussissent lors du passage isolé ; le premier échec d'allocation n'est donc pas attribué à une cause établie. Les contrôles de contrats publics signalent notamment Acb4000Clocks et d'autres classes de services ACB-4000 ; l'absence de contrat sur Acb4000Clocks est également visible dans HEAD, et aucun fichier Adaptec n'est modifié par ce bloc. Ces échecs ne sont pas supprimés ni considérés résolus.

Compilation de l'application et du module Acorn Debug réussie pendant la construction des tests d'intégration. Premier groupe d'intégration exécuté : 164 réussites, deux échecs sur 166 cas, 37 s. Les échecs du câblage série venaient des nouvelles références de callbacks comparées par réflexion et du compte initial des broches à la construction. Après adaptation de ces seules assertions, les six cas AcornEmuSerialWiringTests réussissent en Debug, 848 ms ; les états, compteurs, phases, sorties et interruptions restent comparés. Les 160 cas extérieurs à cette classe ne sont pas répétés sans changement de code concerné.

Groupe d'intégration Release final : 166 réussites, zéro échec ni ignoré, 10 s. Les fiches des sept composants du bloc contiennent le bilan groupé, ses limites et les échecs globaux encore ouverts. Aucune mesure du BBC complet, capture nouvelle ou livraison par local-building n'est revendiquée à cette étape.
